using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Scripting;

namespace Causeless3t.Network
{
    [AttributeUsage(AttributeTargets.Class)]
    public class APIAttribute : PreserveAttribute
    {
        public string API { get; }

        /// <summary>HTTP Method of the handler. POST when not set (2.0.0 behavior).</summary>
        public RequestMethod Method { get; set; } = RequestMethod.Post;

        public APIAttribute(string api) => API = api;
        public static string GetAPI(Type type) => type.GetCustomAttribute<APIAttribute>()?.API;

        public static RequestMethod GetMethod(Type type) =>
            type.GetCustomAttribute<APIAttribute>()?.Method ?? RequestMethod.Post;
    }

    public interface IRequest
    {
    }

    public interface ICollapsableRequest : IRequest, IEquatable<ICollapsableRequest>
    {
        void Collapse(ICollapsableRequest req);
    }

    [Serializable]
    public class BaseResponse
    {
        public int ErrCode;
    }

    public interface IHttpManagerAware
    {
        void Bind(HttpManager manager);
    }

    public interface IRequestHandler
    {
        string API { get; }

        RequestInfo CreateRequest(
            int packetNum,
            IRequest request,
            Action<RequestInfo, string> callback = null);

        void Parse(RequestInfo requestInfo, string responseString);

        void ErrorProcess(RequestInfo requestInfo, BaseResponse response, int error);

        void ReleasePacket(RequestInfo info);
    }

    /// <summary>
    /// Optional per-handler transport policy. <see cref="HttpManager"/> uses the defaults
    /// (POST, retry connection errors, log bodies, always ready, no extra headers) for handlers
    /// that do not implement it. <see cref="RequestHandler{REQ,RES}"/> implements it with
    /// overridable members.
    /// </summary>
    public interface IRequestPolicy
    {
        /// <summary>HTTP Method. Also part of the registration key, so GET and PUT handlers can share a path.</summary>
        RequestMethod Method { get; }

        /// <summary>
        /// Whether connection errors (including timeouts) are retried automatically. Return false
        /// for requests that are not safe to send twice; the handler then receives
        /// <see cref="HttpErrorCodes.ConnectionFailed"/> after the first failure.
        /// </summary>
        bool RetryOnConnectionError { get; }

        /// <summary>Whether the request body may be written to the log. Return false for credentials and large payloads.</summary>
        bool LogRequestBody { get; }

        /// <summary>
        /// Called each frame before a waiting request starts (and before every retry). Returning false keeps
        /// the request waiting without consuming a retry, for example while an access token is being refreshed.
        /// </summary>
        bool IsReadyToSend(RequestInfo info);

        /// <summary>
        /// Adds headers right before each send (also before every retry), after the default and per-request
        /// headers, so values such as Authorization always use the current credentials.
        /// </summary>
        void ApplyHeaders(RequestInfo info, IDictionary<string, string> headers);

        /// <summary>
        /// Whether an empty 2xx body other than 204/205 is a valid response. 204 and 205 are always
        /// treated as success without a body.
        /// </summary>
        bool AllowEmptyResponse { get; }
    }

    public abstract class RequestHandler<REQ, RES> :
        IRequestHandler,
        IHttpManagerAware,
        IRequestPolicy
        where REQ : IRequest
        where RES : BaseResponse
    {
        public virtual string API => APIAttribute.GetAPI(GetType());

        /// <summary>HTTP Method. Defaults to <see cref="APIAttribute.Method"/>, which defaults to POST.</summary>
        public virtual RequestMethod Method => APIAttribute.GetMethod(GetType());

        /// <summary>Whether connection errors are retried automatically. True by default (2.0.0 behavior).</summary>
        public virtual bool RetryOnConnectionError => true;

        /// <summary>Whether the request body may be logged. True by default.</summary>
        public virtual bool LogRequestBody => true;

        /// <summary>Whether an empty 2xx body other than 204/205 is accepted. False by default (2.0.0 behavior).</summary>
        public virtual bool AllowEmptyResponse => false;

        private HttpManager _httpManager;

        private static readonly IObjectPool<RequestInfo> RequestInfoPool =
            new ObjectPool<RequestInfo>(() => new RequestInfo());

        void IHttpManagerAware.Bind(HttpManager manager)
        {
            if (manager == null)
                throw new ArgumentNullException(nameof(manager));

            if (_httpManager != null &&
                !ReferenceEquals(_httpManager, manager))
            {
                throw new InvalidOperationException(
                    $"{GetType().Name} is already bound to another HttpManager.");
            }

            _httpManager = manager;
        }

        bool IRequestPolicy.IsReadyToSend(RequestInfo info) => IsReadyToSend(info);

        void IRequestPolicy.ApplyHeaders(RequestInfo info, IDictionary<string, string> headers) =>
            ApplyHeaders(info, headers);

        public void EnqueueRequest(Action<RequestInfo, string> callback = null)
        {
            Enqueue(MakeReqMessage(), callback);
        }

        protected abstract REQ MakeReqMessage();

        protected void Enqueue(REQ request, Action<RequestInfo, string> callback)
        {
            if (request == null)
                throw new InvalidOperationException($"{GetType().Name} returned a null request.");

            if (request is ICollapsableRequest collapsableRequest)
            {
                GetManager().EnqueueBundlePacket(this, collapsableRequest, callback);
                return;
            }

            GetManager().EnqueuePacket(this, request, callback);
        }

        /// <summary>
        /// Enqueues a request with caller state stored in <see cref="RequestInfo.UserState"/>, so
        /// <see cref="Process"/> and <see cref="ErrorProcess"/> can deliver the result to the caller
        /// without keeping per-request fields on the handler. Collapsible requests are not supported.
        /// </summary>
        protected void Enqueue(REQ request, object userState, Action<RequestInfo, string> callback)
        {
            if (request == null)
                throw new InvalidOperationException($"{GetType().Name} returned a null request.");

            if (request is ICollapsableRequest)
            {
                throw new InvalidOperationException(
                    $"{GetType().Name}: collapsible requests cannot carry per-request state.");
            }

            GetManager().EnqueuePacket(this, request, callback, userState);
        }

        private HttpManager GetManager()
        {
            if (_httpManager == null)
            {
                throw new InvalidOperationException(
                    $"{GetType().Name} is not registered to an HttpManager.");
            }

            return _httpManager;
        }

        public RequestInfo CreateRequest(
            int packetNum,
            IRequest request,
            Action<RequestInfo, string> callback = null)
        {
            if (request is not REQ typedRequest)
            {
                throw new ArgumentException(
                    $"Request must be of type {typeof(REQ).Name}.",
                    nameof(request));
            }

            var method = Method;
            var body = method.HasRequestBody()
                ? SerializeRequest(typedRequest)
                : null;

            Dictionary<string, string> headers = null;
            AddRequestHeaders(typedRequest, ref headers);

            var info = RequestInfoPool.Get();

            try
            {
                info.SetInfo(this, API, packetNum, body, callback);
                info.SetRequestOptions(method, headers);
            }
            catch
            {
                info.Reset();
                RequestInfoPool.Release(info);
                throw;
            }

            return info;
        }

        /// <summary>Serializes the request body (POST, PUT, PATCH). JsonUtility by default.</summary>
        protected virtual string SerializeRequest(REQ request) => JsonUtility.ToJson(request);

        /// <summary>
        /// Adds per-request headers that are fixed when the request is created. Create the dictionary
        /// only when adding headers. Use <see cref="ApplyHeaders"/> for values that can change before sending.
        /// </summary>
        protected virtual void AddRequestHeaders(REQ request, ref Dictionary<string, string> headers)
        {
        }

        /// <summary>See <see cref="IRequestPolicy.ApplyHeaders"/>.</summary>
        protected virtual void ApplyHeaders(RequestInfo info, IDictionary<string, string> headers)
        {
        }

        /// <summary>See <see cref="IRequestPolicy.IsReadyToSend"/>.</summary>
        protected virtual bool IsReadyToSend(RequestInfo info) => true;

        public void Parse(RequestInfo requestInfo, string responseString)
        {
            var response = DeserializeResponse(requestInfo, responseString);
            Process(requestInfo, response);
        }

        /// <summary>
        /// Converts the response body. An empty body (204, 205 or <see cref="AllowEmptyResponse"/>) becomes a
        /// response with default field values. JsonUtility by default.
        /// </summary>
        protected virtual RES DeserializeResponse(RequestInfo requestInfo, string responseString)
        {
            return JsonUtility.FromJson<RES>(
                string.IsNullOrEmpty(responseString) ? "{}" : responseString);
        }

        protected abstract void Process(RequestInfo requestInfo, RES response);

        public virtual void ErrorProcess(RequestInfo requestInfo, BaseResponse response, int error)
        {
            Debug.LogError($"Has occurred common error({error})");
        }

        public virtual void ReleasePacket(RequestInfo info)
        {
            info.Reset();
            RequestInfoPool.Release(info);
        }
    }

    public abstract class RequestHandler<REQ, RES, PARAM> : RequestHandler<REQ, RES>
        where REQ : IRequest
        where RES : BaseResponse
    {
        protected override REQ MakeReqMessage() => default;
        protected abstract REQ MakeReqMessage(PARAM @params);

        public void EnqueueRequest(
            PARAM param,
            Action<RequestInfo, string> callback = null)
        {
            Enqueue(MakeReqMessage(param), callback);
        }

        /// <summary>Enqueues a request with caller state (<see cref="RequestInfo.UserState"/>).</summary>
        public void EnqueueRequestWithState(
            PARAM param,
            object userState,
            Action<RequestInfo, string> callback = null)
        {
            Enqueue(MakeReqMessage(param), userState, callback);
        }
    }
}
