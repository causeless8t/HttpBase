using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using Causeless3t.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Causeless3t.Network
{
    public sealed class HttpManager : Singleton<HttpManager>, IDisposable
    {
        private const int InvalidResponseError = -1;

        private readonly Dictionary<string, IRequestHandler> _requestHandlers = new();
        private readonly Queue<RequestInfo> _requestWaitingQueue = new();

        private readonly Dictionary<
            string,
            (
                IRequestHandler Handler,
                ICollapsableRequest Request,
                Action<RequestInfo, string> Callback
            )> _collapsableRequestDic = new();

        private HttpManagerOptions _options;
        private CancellationTokenSource _lifetimeCTS;
        private CancellationTokenSource _delayedPacketCTS;
        private readonly HashSet<RequestInfo> _inProgressRequests = new();

        private int _packetNumber;
        public int PacketNumber => _packetNumber;

        public void Initialize(
            HttpManagerOptions options,
            params Assembly[] handlerAssemblies)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            if (handlerAssemblies == null)
                throw new ArgumentNullException(nameof(handlerAssemblies));

            if (handlerAssemblies.Any(assembly => assembly == null))
            {
                throw new ArgumentException(
                    "Handler assemblies cannot contain null.",
                    nameof(handlerAssemblies));
            }

            Dispose();
            _options = options;
            _lifetimeCTS = new CancellationTokenSource();

            try
            {
                foreach (var assembly in handlerAssemblies.Distinct())
                    RegisterHandlersFrom(assembly);
            }
            catch
            {
                Dispose();
                throw;
            }

            _packetNumber = 0;
        }

        public void EnqueuePacket(
            IRequestHandler handler,
            IRequest request,
            Action<RequestInfo, string> callback = null)
        {
            ThrowIfNotInitialized();

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            if (request == null)
                throw new ArgumentNullException(nameof(request));

            _requestWaitingQueue.Enqueue(
                handler.CreateRequest(_packetNumber++, request, callback));
        }

        public void EnqueueBundlePacket(
            IRequestHandler handler,
            ICollapsableRequest request,
            Action<RequestInfo, string> callback = null)
        {
            ThrowIfNotInitialized();

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!_collapsableRequestDic.TryGetValue(handler.API, out var requestTuple))
            {
                _collapsableRequestDic.Add(
                    handler.API,
                    (handler, request, callback));

                DelayedSendBundlePacket().Forget();
            }
            else if (!requestTuple.Request.Equals(request))
            {
                _delayedPacketCTS?.Cancel();
                _delayedPacketCTS = null;

                _requestWaitingQueue.Enqueue(
                    requestTuple.Handler.CreateRequest(
                        _packetNumber++,
                        requestTuple.Request,
                        requestTuple.Callback));

                _collapsableRequestDic[handler.API] =
                    (handler, request, callback);

                DelayedSendBundlePacket().Forget();
            }
            else
            {
                requestTuple.Request.Collapse(request);
                requestTuple.Callback += callback;
                _collapsableRequestDic[handler.API] = requestTuple;
            }
        }

        private async UniTask DelayedSendBundlePacket()
        {
            var lifetimeToken = _lifetimeCTS?.Token ?? CancellationToken.None;
            var cancellationSource = _delayedPacketCTS ??=
                CancellationTokenSource.CreateLinkedTokenSource(lifetimeToken);

            try
            {
                await UniTask.WaitForSeconds(
                    _options.BundleDelaySeconds,
                    cancellationToken: cancellationSource.Token);

                Queue<(
                    IRequestHandler Handler,
                    ICollapsableRequest Request,
                    Action<RequestInfo, string> Callback
                )> collapsedSendingQueue = new();

                foreach (var tuple in _collapsableRequestDic.Values)
                    collapsedSendingQueue.Enqueue(tuple);

                _collapsableRequestDic.Clear();

                while (collapsedSendingQueue.Count > 0)
                {
                    cancellationSource.Token.ThrowIfCancellationRequested();

                    var requestTuple = collapsedSendingQueue.Dequeue();

                    _requestWaitingQueue.Enqueue(
                        requestTuple.Handler.CreateRequest(
                            _packetNumber++,
                            requestTuple.Request,
                            requestTuple.Callback));

                    await UniTask.Yield();
                }
            }
            catch (OperationCanceledException)
            {
                // Dispose 또는 새로운 병합 그룹에 의해 취소되었습니다.
            }
            finally
            {
                if (ReferenceEquals(_delayedPacketCTS, cancellationSource))
                    _delayedPacketCTS = null;

                cancellationSource.Dispose();
            }
        }

        public int RegisterHandlersFrom(Assembly assembly)
        {
            ThrowIfNotInitialized();

            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            var handlerTypes = assembly
                .GetTypes()
                .Where(type =>
                    type.IsClass &&
                    !type.IsAbstract &&
                    typeof(IRequestHandler).IsAssignableFrom(type) &&
                    type.IsDefined(typeof(APIAttribute)))
                .ToArray();

            foreach (var handlerType in handlerTypes)
            {
                if (handlerType.GetConstructor(Type.EmptyTypes) == null)
                {
                    throw new InvalidOperationException(
                        $"{handlerType.FullName} must have a public parameterless constructor.");
                }

                IRequestHandler handler;

                try
                {
                    handler = (IRequestHandler)Activator.CreateInstance(handlerType);
                }
                catch (Exception exception)
                {
                    throw new InvalidOperationException(
                        $"Failed to create {handlerType.FullName}.",
                        exception);
                }

                RegisterHandler(handler);
            }

            return handlerTypes.Length;
        }

        public void RegisterHandler(IRequestHandler handler)
        {
            ThrowIfNotInitialized();

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            if (string.IsNullOrWhiteSpace(handler.API))
            {
                throw new InvalidOperationException(
                    $"{handler.GetType().FullName} does not define a valid API path.");
            }

            if (!_requestHandlers.TryAdd(handler.API, handler))
            {
                var registeredType = _requestHandlers[handler.API].GetType();

                throw new InvalidOperationException(
                    $"The API path '{handler.API}' is already registered by " +
                    $"{registeredType.FullName}.");
            }

            Debug.Log(
                $"Registered HTTP Handler: {handler.API} -> " +
                $"{handler.GetType().FullName}");
        }

        public IRequestHandler GetHandler(string api)
        {
            if (!_requestHandlers.TryGetValue(api, out var handler))
            {
                Debug.LogErrorFormat("{0} doesn't exist recv handler !!", api);
                return null;
            }

            return handler;
        }

        public T GetHandler<T>() where T : class, IRequestHandler
        {
            var api = APIAttribute.GetAPI(typeof(T));
            return GetHandler(api) as T;
        }

        public override void OnUpdate()
        {
            if (_lifetimeCTS == null || _lifetimeCTS.IsCancellationRequested)
                return;

            for (int i = 0; i < _requestWaitingQueue.Count; ++i)
            {
                if (_inProgressRequests.Count >= _options.MaxConcurrentRequests)
                    break;

                var request = _requestWaitingQueue.Dequeue();
                SendPacket(request).Forget();
            }
        }

        private async UniTask SendPacket(RequestInfo info)
        {
            WWWForm formData = new WWWForm();
            byte[] bytes = new System.Text.UTF8Encoding().GetBytes(info.Body);
            using var www = UnityWebRequest.Post(_options.BuildUrl(info.Protocol), formData);
            www.uploadHandler = new UploadHandlerRaw(bytes);
            www.downloadHandler = new DownloadHandlerBuffer();
            // www.SetRequestHeader("Content-Type", "application/json");
            // www.SetRequestHeader("Authorization", $"Bearer {SessionKey}");
            www.useHttpContinue = false;
            www.timeout = _options.TimeoutSeconds;

            info.State = RequestInfo.eRequestState.InProgress;
            _inProgressRequests.Add(info);

            float elapsedTime = Time.realtimeSinceStartup;

            var cancellationToken =
                _lifetimeCTS?.Token ?? CancellationToken.None;

            try
            {
                await www.SendWebRequest().WithCancellation(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                CompleteRequest(info);
                return;
            }

            if (www.result == UnityWebRequest.Result.ConnectionError)
            {
                var error = www.error;

                if (Application.internetReachability == NetworkReachability.NotReachable)
                {
                    Debug.LogErrorFormat(
                        "network is not reachable !! {0}",
                        error);
                }
                else
                {
                    Debug.LogErrorFormat(
                        "connection error in {0}sec !! {1}",
                        Time.realtimeSinceStartup - elapsedTime,
                        error);
                }

                RetryProcess(error, info).Forget();
                return;
            }

            if (www.result == UnityWebRequest.Result.DataProcessingError)
            {
                FailRequest(
                    info,
                    www.error ?? "Failed to process the response data.",
                    InvalidResponseError);

                return;
            }

            if (www.result == UnityWebRequest.Result.InProgress)
            {
                FailRequest(
                    info,
                    "The request did not complete.",
                    InvalidResponseError);

                return;
            }

            var recv = www.downloadHandler.text;
            var handler = info.Handler;

            if (handler == null && info.CustomCallback == null)
            {
                Debug.LogError(
                    $"{info.Protocol} 요청을 처리할 Handler와 커스텀 콜백이 없습니다.");

                CompleteRequest(info);
                return;
            }

            var httpStatusCode = (HttpStatusCode)www.responseCode;

            if (www.result == UnityWebRequest.Result.Success &&
                string.IsNullOrEmpty(recv))
            {
                FailRequest(
                    info,
                    $"{info.Protocol} returned an empty response.",
                    InvalidResponseError);

                return;
            }

            try
            {
                ParsePacketProcess(handler, info, httpStatusCode, recv);
            }
            catch (Exception e)
            {
                var handlerName = handler?.GetType().Name ?? "CustomCallback";
                Debug.LogError(
                    $"{handlerName}에서 {info.Protocol} 응답을 처리하는데 실패했습니다.");

                Debug.LogError($"{e}");
            }
            finally
            {
                CompleteRequest(info);
            }
        }

        private void ParsePacketProcess(
            IRequestHandler handler,
            RequestInfo info,
            HttpStatusCode responseCode,
            string recvString)
        {
            BaseResponse baseResponse = null;

            if (!string.IsNullOrEmpty(recvString))
                baseResponse = JsonUtility.FromJson<BaseResponse>(recvString);

            if (handler == null)
            {
                info.CustomCallback?.Invoke(info, recvString);
                return;
            }

            if (!IsSuccessStatusCode(responseCode))
            {
                handler.ErrorProcess(info, baseResponse, (int)responseCode);
                return;
            }

            if (baseResponse != null &&
                baseResponse.ErrCode != (int)HttpStatusCode.OK &&
                baseResponse.ErrCode != 0)
            {
                handler.ErrorProcess(info, baseResponse, baseResponse.ErrCode);
                return;
            }

            info.CustomCallback?.Invoke(info, recvString);
            handler.Parse(info, recvString);
        }

        private static bool IsSuccessStatusCode(HttpStatusCode responseCode)
        {
            var statusCode = (int)responseCode;
            return statusCode >= 200 && statusCode <= 299;
        }

        private void FailRequest(
            RequestInfo info,
            string error,
            int errorCode)
        {
            Debug.LogError(error);

            try
            {
                info.Handler?.ErrorProcess(info, null, errorCode);
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"{info.Protocol} 오류 처리 중 예외가 발생했습니다.");

                Debug.LogError($"{exception}");
            }
            finally
            {
                CompleteRequest(info);
            }
        }

        private async UniTask RetryProcess(string error, RequestInfo info)
        {
            _inProgressRequests.Remove(info);

            if (!info.TryPrepareRetry(
                    _packetNumber,
                    _options.MaxRetryAttempts))
            {
                Debug.LogError(error);
                CompleteRequest(info);
                return;
            }

            _packetNumber++;

            var cancellationToken =
                _lifetimeCTS?.Token ?? CancellationToken.None;

            try
            {
                await UniTask.WaitForSeconds(
                    _options.RetryDelaySeconds,
                    cancellationToken: cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();
                _requestWaitingQueue.Enqueue(info);
            }
            catch (OperationCanceledException)
            {
                CompleteRequest(info);
            }
        }

        private void CompleteRequest(RequestInfo info)
        {
            if (info == null)
                return;

            _inProgressRequests.Remove(info);

            var handler = info.Handler;
            if (handler == null)
            {
                info.Reset();
                return;
            }

            handler.ReleasePacket(info);
        }

        private void ThrowIfNotInitialized()
        {
            if (_options == null ||
                _lifetimeCTS == null ||
                _lifetimeCTS.IsCancellationRequested)
            {
                throw new InvalidOperationException(
                    "HttpManager.Initialize() must be called before enqueueing requests.");
            }
        }

        public void Dispose()
        {
            var lifetimeCTS = _lifetimeCTS;
            _lifetimeCTS = null;
            lifetimeCTS?.Cancel();

            var delayedPacketCTS = _delayedPacketCTS;
            _delayedPacketCTS = null;
            delayedPacketCTS?.Cancel();

            while (_requestWaitingQueue.TryDequeue(out var requestInfo))
                CompleteRequest(requestInfo);

            _collapsableRequestDic.Clear();
            _requestHandlers.Clear();
            _options = null;
            _packetNumber = 0;

            lifetimeCTS?.Dispose();
        }
    }
}
