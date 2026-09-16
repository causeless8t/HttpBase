using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

namespace Causeless3t.Network
{
    public sealed class HttpManager : MonoBehaviour, IDisposable
    {
        private const int InvalidResponseError = -1;

        private sealed class PendingCollapsibleRequest
        {
            public IRequestHandler Handler { get; }
            public ICollapsableRequest Request { get; }
            public Action<RequestInfo, string> Callback { get; private set; }
            public Coroutine DelayCoroutine { get; set; }

            public PendingCollapsibleRequest(
                IRequestHandler handler,
                ICollapsableRequest request,
                Action<RequestInfo, string> callback)
            {
                Handler = handler;
                Request = request;
                Callback = callback;
            }

            public void Collapse(
                ICollapsableRequest request,
                Action<RequestInfo, string> callback)
            {
                Request.Collapse(request);
                Callback += callback;
            }
        }

        private readonly Dictionary<string, IRequestHandler> _requestHandlers = new();
        private readonly Queue<RequestInfo> _requestWaitingQueue = new();

        private readonly Dictionary<string, PendingCollapsibleRequest>
            _pendingCollapsibleRequests = new();

        private readonly HashSet<RequestInfo> _inProgressRequests = new();
        private readonly HashSet<RequestInfo> _retryingRequests = new();

        private readonly Dictionary<RequestInfo, UnityWebRequest>
            _activeWebRequests = new();

        private HttpManagerOptions _options;
        private bool _isInitialized;

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
            _isInitialized = true;

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

            if (_pendingCollapsibleRequests.TryGetValue(
                    handler.API,
                    out var pendingRequest))
            {
                if (pendingRequest.Request.Equals(request))
                {
                    pendingRequest.Collapse(request, callback);
                    return;
                }

                FlushPendingBundle(handler.API, pendingRequest);
            }

            var newPendingRequest = new PendingCollapsibleRequest(
                handler,
                request,
                callback);

            _pendingCollapsibleRequests.Add(
                handler.API,
                newPendingRequest);

            newPendingRequest.DelayCoroutine = StartCoroutine(
                DelayedSendBundlePacket(
                    handler.API,
                    newPendingRequest));
        }

        private IEnumerator DelayedSendBundlePacket(
            string api,
            PendingCollapsibleRequest pendingRequest)
        {
            yield return new WaitForSecondsRealtime(
                _options.BundleDelaySeconds);

            if (!_isInitialized ||
                !_pendingCollapsibleRequests.TryGetValue(
                    api,
                    out var currentRequest) ||
                !ReferenceEquals(currentRequest, pendingRequest))
            {
                yield break;
            }

            _pendingCollapsibleRequests.Remove(api);
            pendingRequest.DelayCoroutine = null;
            QueueBundleRequest(pendingRequest);
        }

        private void FlushPendingBundle(
            string api,
            PendingCollapsibleRequest pendingRequest)
        {
            if (!_pendingCollapsibleRequests.TryGetValue(
                    api,
                    out var currentRequest) ||
                !ReferenceEquals(currentRequest, pendingRequest))
            {
                return;
            }

            _pendingCollapsibleRequests.Remove(api);

            if (pendingRequest.DelayCoroutine != null)
            {
                StopCoroutine(pendingRequest.DelayCoroutine);
                pendingRequest.DelayCoroutine = null;
            }

            QueueBundleRequest(pendingRequest);
        }

        private void QueueBundleRequest(
            PendingCollapsibleRequest pendingRequest)
        {
            _requestWaitingQueue.Enqueue(
                pendingRequest.Handler.CreateRequest(
                    _packetNumber++,
                    pendingRequest.Request,
                    pendingRequest.Callback));
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

            if (_requestHandlers.TryGetValue(
                    handler.API,
                    out var registeredHandler))
            {
                throw new InvalidOperationException(
                    $"The API path '{handler.API}' is already registered by " +
                    $"{registeredHandler.GetType().FullName}.");
            }

            if (handler is IHttpManagerAware managerAware)
                managerAware.Bind(this);

            _requestHandlers.Add(handler.API, handler);

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
            return _requestHandlers.Values.OfType<T>().FirstOrDefault();
        }

        private void Update()
        {
            if (!_isInitialized)
                return;

            while (_requestWaitingQueue.Count > 0 &&
                   _inProgressRequests.Count <
                   _options.MaxConcurrentRequests)
            {
                var request = _requestWaitingQueue.Dequeue();
                StartCoroutine(SendPacket(request));
            }
        }

        private IEnumerator SendPacket(RequestInfo info)
        {
            var formData = new WWWForm();
            var bytes =
                new System.Text.UTF8Encoding().GetBytes(info.Body);

            var www = UnityWebRequest.Post(
                _options.BuildUrl(info.Protocol),
                formData);

            www.uploadHandler = new UploadHandlerRaw(bytes);
            www.downloadHandler = new DownloadHandlerBuffer();
            // www.SetRequestHeader("Content-Type", "application/json");
            // www.SetRequestHeader("Authorization", $"Bearer {SessionKey}");
            www.useHttpContinue = false;
            www.timeout = _options.TimeoutSeconds;

            info.State = RequestInfo.eRequestState.InProgress;
            _inProgressRequests.Add(info);
            _activeWebRequests.Add(info, www);

            var elapsedTime = Time.realtimeSinceStartup;

            try
            {
                yield return www.SendWebRequest();

                if (!_isInitialized)
                    yield break;

                if (www.result ==
                    UnityWebRequest.Result.ConnectionError)
                {
                    var error = www.error;

                    if (Application.internetReachability ==
                        NetworkReachability.NotReachable)
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

                    StartCoroutine(RetryProcess(error, info));
                    yield break;
                }

                if (www.result ==
                    UnityWebRequest.Result.DataProcessingError)
                {
                    FailRequest(
                        info,
                        www.error ??
                        "Failed to process the response data.",
                        InvalidResponseError);

                    yield break;
                }

                if (www.result ==
                    UnityWebRequest.Result.InProgress)
                {
                    FailRequest(
                        info,
                        "The request did not complete.",
                        InvalidResponseError);

                    yield break;
                }

                var recv = www.downloadHandler.text;
                var handler = info.Handler;

                if (handler == null &&
                    info.CustomCallback == null)
                {
                    Debug.LogError(
                        $"{info.Protocol} 요청을 처리할 Handler와 커스텀 콜백이 없습니다.");

                    CompleteRequest(info);
                    yield break;
                }

                var httpStatusCode =
                    (HttpStatusCode)www.responseCode;

                if (www.result ==
                        UnityWebRequest.Result.Success &&
                    string.IsNullOrEmpty(recv))
                {
                    FailRequest(
                        info,
                        $"{info.Protocol} returned an empty response.",
                        InvalidResponseError);

                    yield break;
                }

                try
                {
                    ParsePacketProcess(
                        handler,
                        info,
                        httpStatusCode,
                        recv);
                }
                catch (Exception exception)
                {
                    var handlerName =
                        handler?.GetType().Name ??
                        "CustomCallback";

                    Debug.LogError(
                        $"{handlerName}에서 {info.Protocol} 응답을 처리하는데 실패했습니다.");

                    Debug.LogError($"{exception}");
                }
                finally
                {
                    CompleteRequest(info);
                }
            }
            finally
            {
                _activeWebRequests.Remove(info);
                www.Dispose();
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

        private IEnumerator RetryProcess(
            string error,
            RequestInfo info)
        {
            _inProgressRequests.Remove(info);

            if (!info.TryPrepareRetry(
                    _packetNumber,
                    _options.MaxRetryAttempts))
            {
                Debug.LogError(error);
                CompleteRequest(info);
                yield break;
            }

            _packetNumber++;
            _retryingRequests.Add(info);

            yield return new WaitForSecondsRealtime(
                _options.RetryDelaySeconds);

            if (!_isInitialized)
                yield break;

            _retryingRequests.Remove(info);
            _requestWaitingQueue.Enqueue(info);
        }

        private void CompleteRequest(RequestInfo info)
        {
            if (info == null)
                return;

            _inProgressRequests.Remove(info);
            _retryingRequests.Remove(info);

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
            if (!_isInitialized ||
                _options == null)
            {
                throw new InvalidOperationException(
                    "HttpManager.Initialize() must be called before enqueueing requests.");
            }
        }

        public void Dispose()
        {
            _isInitialized = false;

            foreach (var webRequest in
                     _activeWebRequests.Values.ToArray())
            {
                webRequest.Abort();
            }

            StopAllCoroutines();

            foreach (var webRequest in
                     _activeWebRequests.Values.ToArray())
            {
                webRequest.Dispose();
            }

            _activeWebRequests.Clear();
            _pendingCollapsibleRequests.Clear();

            while (_requestWaitingQueue.TryDequeue(
                       out var waitingRequest))
            {
                CompleteRequest(waitingRequest);
            }

            foreach (var request in
                     _inProgressRequests.ToArray())
            {
                CompleteRequest(request);
            }

            foreach (var request in
                     _retryingRequests.ToArray())
            {
                CompleteRequest(request);
            }

            _requestHandlers.Clear();
            _options = null;
            _packetNumber = 0;
        }

        private void OnDestroy()
        {
            Dispose();
        }
    }
}
