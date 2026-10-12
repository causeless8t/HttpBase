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
        private const int InvalidResponseError = HttpErrorCodes.InvalidResponse;
        private const string JsonContentType = "application/json";

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

        /// <summary>Policy used for handlers that do not implement <see cref="IRequestPolicy"/> (2.0.0 behavior).</summary>
        private sealed class DefaultRequestPolicy : IRequestPolicy
        {
            public static readonly DefaultRequestPolicy Instance = new();

            public RequestMethod Method => RequestMethod.Post;
            public bool RetryOnConnectionError => true;
            public bool LogRequestBody => true;
            public bool AllowEmptyResponse => false;
            public bool IsReadyToSend(RequestInfo info) => true;

            public void ApplyHeaders(RequestInfo info, IDictionary<string, string> headers)
            {
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

        private readonly Dictionary<string, string> _sendHeaders =
            new(StringComparer.OrdinalIgnoreCase);

        private HttpManagerOptions _options;
        private bool _isInitialized;

        private int _packetNumber;
        public int PacketNumber => _packetNumber;

        /// <summary>True between <see cref="Initialize"/> and <see cref="Dispose"/>.</summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>Options passed to <see cref="Initialize"/>. Null when not initialized.</summary>
        public HttpManagerOptions Options => _options;

        /// <summary>Number of requests waiting, in progress or waiting for a retry.</summary>
        public int PendingRequestCount =>
            _requestWaitingQueue.Count + _inProgressRequests.Count + _retryingRequests.Count;

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
            EnqueuePacket(handler, request, callback, null);
        }

        /// <summary>
        /// Enqueues a request with caller state stored in <see cref="RequestInfo.UserState"/>.
        /// </summary>
        public void EnqueuePacket(
            IRequestHandler handler,
            IRequest request,
            Action<RequestInfo, string> callback,
            object userState)
        {
            ThrowIfNotInitialized();

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var info = handler.CreateRequest(_packetNumber++, request, callback);
            info.SetUserState(userState);
            _requestWaitingQueue.Enqueue(info);
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

            var key = GetHandlerKey(handler);

            if (_pendingCollapsibleRequests.TryGetValue(
                    key,
                    out var pendingRequest))
            {
                if (pendingRequest.Request.Equals(request))
                {
                    pendingRequest.Collapse(request, callback);
                    return;
                }

                FlushPendingBundle(key, pendingRequest);
            }

            var newPendingRequest = new PendingCollapsibleRequest(
                handler,
                request,
                callback);

            _pendingCollapsibleRequests.Add(
                key,
                newPendingRequest);

            newPendingRequest.DelayCoroutine = StartCoroutine(
                DelayedSendBundlePacket(
                    key,
                    newPendingRequest));
        }

        private IEnumerator DelayedSendBundlePacket(
            string key,
            PendingCollapsibleRequest pendingRequest)
        {
            yield return new WaitForSecondsRealtime(
                _options.BundleDelaySeconds);

            if (!_isInitialized ||
                !_pendingCollapsibleRequests.TryGetValue(
                    key,
                    out var currentRequest) ||
                !ReferenceEquals(currentRequest, pendingRequest))
            {
                yield break;
            }

            _pendingCollapsibleRequests.Remove(key);
            pendingRequest.DelayCoroutine = null;
            QueueBundleRequest(pendingRequest);
        }

        private void FlushPendingBundle(
            string key,
            PendingCollapsibleRequest pendingRequest)
        {
            if (!_pendingCollapsibleRequests.TryGetValue(
                    key,
                    out var currentRequest) ||
                !ReferenceEquals(currentRequest, pendingRequest))
            {
                return;
            }

            _pendingCollapsibleRequests.Remove(key);

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

        /// <summary>
        /// Registers a handler instance. The registration key is the HTTP Method and API path, so
        /// handlers with the same path and different methods can be registered together.
        /// </summary>
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

            var key = GetHandlerKey(handler);

            if (_requestHandlers.TryGetValue(
                    key,
                    out var registeredHandler))
            {
                throw new InvalidOperationException(
                    $"The API '{key}' is already registered by " +
                    $"{registeredHandler.GetType().FullName}.");
            }

            if (handler is IHttpManagerAware managerAware)
                managerAware.Bind(this);

            _requestHandlers.Add(key, handler);

            Debug.Log(
                $"Registered HTTP Handler: {key} -> " +
                $"{handler.GetType().FullName}");
        }

        /// <summary>
        /// Returns the handler for a path. Prefers the POST handler (2.0.0 behavior), otherwise the only
        /// handler registered for the path.
        /// </summary>
        public IRequestHandler GetHandler(string api)
        {
            if (!string.IsNullOrEmpty(api) &&
                _requestHandlers.TryGetValue(
                    MakeHandlerKey(RequestMethod.Post, api),
                    out var postHandler))
            {
                return postHandler;
            }

            IRequestHandler found = null;
            foreach (var handler in _requestHandlers.Values)
            {
                if (handler.API != api)
                    continue;

                if (found != null)
                {
                    Debug.LogErrorFormat(
                        "{0} has handlers for several methods. Use GetHandler(api, method).",
                        api);
                    return null;
                }

                found = handler;
            }

            if (found == null)
                Debug.LogErrorFormat("{0} doesn't exist recv handler !!", api);

            return found;
        }

        public IRequestHandler GetHandler(string api, RequestMethod method)
        {
            if (!_requestHandlers.TryGetValue(
                    MakeHandlerKey(method, api),
                    out var handler))
            {
                Debug.LogErrorFormat(
                    "{0} doesn't exist recv handler !!",
                    MakeHandlerKey(method, api));
                return null;
            }

            return handler;
        }

        public T GetHandler<T>() where T : class, IRequestHandler
        {
            return _requestHandlers.Values.OfType<T>().FirstOrDefault();
        }

        private static IRequestPolicy GetPolicy(IRequestHandler handler)
        {
            return handler as IRequestPolicy ?? DefaultRequestPolicy.Instance;
        }

        private static string GetHandlerKey(IRequestHandler handler)
        {
            return MakeHandlerKey(GetPolicy(handler).Method, handler.API);
        }

        private static string MakeHandlerKey(RequestMethod method, string api)
        {
            // POST keeps the bare path so 2.0.0 keys and log lines stay the same.
            return method == RequestMethod.Post
                ? api
                : $"{method.ToHttpMethod()} {api}";
        }

        private void Update()
        {
            if (!_isInitialized)
                return;

            // Each waiting request is looked at once per frame. Requests whose handler is not ready
            // (for example while a token is refreshed) go back to the end of the queue.
            var count = _requestWaitingQueue.Count;

            while (count-- > 0 &&
                   _isInitialized &&
                   _requestWaitingQueue.Count > 0 &&
                   _inProgressRequests.Count <
                   _options.MaxConcurrentRequests)
            {
                var request = _requestWaitingQueue.Dequeue();

                if (!IsReadyToSend(request))
                {
                    _requestWaitingQueue.Enqueue(request);
                    continue;
                }

                // Registered before the coroutine starts so the concurrency limit sees it this frame.
                request.State = RequestInfo.eRequestState.InProgress;
                _inProgressRequests.Add(request);
                StartCoroutine(SendPacket(request));
            }
        }

        private bool IsReadyToSend(RequestInfo info)
        {
            var handler = info.Handler;
            if (handler == null)
                return true;

            try
            {
                return GetPolicy(handler).IsReadyToSend(info);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return true;
            }
        }

        private UnityWebRequest CreateWebRequest(
            RequestInfo info,
            IRequestPolicy policy)
        {
            var method = info.Method;
            var www = new UnityWebRequest(
                _options.BuildUrl(info.Protocol),
                method.ToHttpMethod());

            try
            {
                var hasBody = method.HasRequestBody();

                if (hasBody)
                {
                    var bytes = new System.Text.UTF8Encoding()
                        .GetBytes(info.Body ?? string.Empty);

                    www.uploadHandler = new UploadHandlerRaw(bytes)
                    {
                        contentType = JsonContentType
                    };
                }

                www.downloadHandler = new DownloadHandlerBuffer();
                www.useHttpContinue = false;
                www.timeout = _options.TimeoutSeconds;

                // Order: defaults, then per-request headers, then send-time headers (each may override).
                _sendHeaders.Clear();
                _sendHeaders["Accept"] = JsonContentType;

                if (hasBody)
                    _sendHeaders["Content-Type"] = JsonContentType;

                foreach (var header in _options.DefaultHeaders)
                    _sendHeaders[header.Key] = header.Value;

                foreach (var header in info.Headers)
                    _sendHeaders[header.Key] = header.Value;

                policy.ApplyHeaders(info, _sendHeaders);

                foreach (var header in _sendHeaders)
                {
                    RequestInfo.ValidateHeader(header.Key, header.Value);
                    www.SetRequestHeader(header.Key, header.Value);
                }

                _sendHeaders.Clear();
                return www;
            }
            catch
            {
                _sendHeaders.Clear();
                www.Dispose();
                throw;
            }
        }

        private IEnumerator SendPacket(RequestInfo info)
        {
            var policy = GetPolicy(info.Handler);
            UnityWebRequest www;

            try
            {
                www = CreateWebRequest(info, policy);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                FailRequest(
                    info,
                    $"{info.Protocol} request could not be created.",
                    InvalidResponseError);
                yield break;
            }

            if (_options.LogRequestBodies && policy.LogRequestBody)
            {
                Debug.Log(
                    $"<color=yellow>Req {info.Method.ToHttpMethod()} {info.Protocol} >> {info.Body}</color>");
            }

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
                    info.SetResponse(0, null, null, error);

                    if (Application.internetReachability ==
                        NetworkReachability.NotReachable)
                    {
                        Debug.LogErrorFormat(
                            "network is not reachable !! {0} {1}",
                            info.Protocol,
                            error);
                    }
                    else
                    {
                        Debug.LogErrorFormat(
                            "connection error in {0}sec !! {1} {2}",
                            Time.realtimeSinceStartup - elapsedTime,
                            info.Protocol,
                            error);
                    }

                    if (!policy.RetryOnConnectionError)
                    {
                        FailRequest(
                            info,
                            $"{info.Protocol} failed with a connection error and is not retried.",
                            HttpErrorCodes.ConnectionFailed);
                        yield break;
                    }

                    StartCoroutine(RetryProcess(error, info));
                    yield break;
                }

                var recv = www.downloadHandler?.text ?? string.Empty;
                info.SetResponse(
                    www.responseCode,
                    recv,
                    www.GetResponseHeaders(),
                    www.error);

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
                    string.IsNullOrEmpty(recv) &&
                    !IsNoContentStatusCode(httpStatusCode) &&
                    !policy.AllowEmptyResponse)
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

                    // The caller still gets exactly one result for the request.
                    NotifyError(info, null, InvalidResponseError);
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
            var baseResponse = TryParseBaseResponse(recvString);

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

        /// <summary>
        /// Reads <see cref="BaseResponse.ErrCode"/> when the body is a JSON object. Bodies that are not JSON
        /// (for example an HTML error page from a proxy) give null instead of an exception.
        /// </summary>
        private static BaseResponse TryParseBaseResponse(string recvString)
        {
            if (string.IsNullOrEmpty(recvString))
                return null;

            var trimmed = recvString.TrimStart();
            if (trimmed.Length == 0 || trimmed[0] != '{')
                return null;

            try
            {
                return JsonUtility.FromJson<BaseResponse>(recvString);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        private static bool IsSuccessStatusCode(HttpStatusCode responseCode)
        {
            var statusCode = (int)responseCode;
            return statusCode >= 200 && statusCode <= 299;
        }

        private static bool IsNoContentStatusCode(HttpStatusCode responseCode)
        {
            return responseCode == HttpStatusCode.NoContent ||
                   responseCode == HttpStatusCode.ResetContent;
        }

        private void FailRequest(
            RequestInfo info,
            string error,
            int errorCode)
        {
            Debug.LogError(error);

            try
            {
                NotifyError(info, null, errorCode);
            }
            finally
            {
                CompleteRequest(info);
            }
        }

        private static void NotifyError(
            RequestInfo info,
            BaseResponse response,
            int errorCode)
        {
            try
            {
                info.Handler?.ErrorProcess(info, response, errorCode);
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"{info.Protocol} 오류 처리 중 예외가 발생했습니다.");

                Debug.LogError($"{exception}");
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
                FailRequest(
                    info,
                    $"{info.Protocol} failed after {info.RetryCount} retries: {error}",
                    HttpErrorCodes.ConnectionFailed);
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

        /// <summary>
        /// Aborts active requests and completes every waiting, in-progress and retrying request. Each of
        /// them receives <see cref="HttpErrorCodes.Canceled"/> through ErrorProcess before it is released,
        /// so callers waiting for a result are not left waiting.
        /// </summary>
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

            var remaining = new List<RequestInfo>(
                _requestWaitingQueue.Count +
                _inProgressRequests.Count +
                _retryingRequests.Count);

            while (_requestWaitingQueue.TryDequeue(
                       out var waitingRequest))
            {
                remaining.Add(waitingRequest);
            }

            remaining.AddRange(_inProgressRequests);
            remaining.AddRange(_retryingRequests);
            _inProgressRequests.Clear();
            _retryingRequests.Clear();

            foreach (var request in remaining)
            {
                request.SetResponse(0, null, null, "The HttpManager was disposed.");
                NotifyError(request, null, HttpErrorCodes.Canceled);
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
