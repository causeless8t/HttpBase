using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Pool;

namespace Causeless3t.Network
{
    [AttributeUsage(AttributeTargets.Class)]
    public class APIAttribute : Attribute
    {
        public string API { get; }

        public APIAttribute(string api) => API = api;
        public static string GetAPI(Type type) => type.GetCustomAttribute<APIAttribute>()?.API;
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

    public abstract class RequestHandler<REQ, RES> : IRequestHandler
        where REQ : IRequest
        where RES : BaseResponse
    {
        public string API => APIAttribute.GetAPI(GetType());

        private static readonly IObjectPool<RequestInfo> RequestInfoPool =
            new ObjectPool<RequestInfo>(() => new RequestInfo());

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
                HttpManager.Instance.EnqueueBundlePacket(this, collapsableRequest, callback);
                return;
            }

            HttpManager.Instance.EnqueuePacket(this, request, callback);
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

            var info = RequestInfoPool.Get();
            var body = JsonUtility.ToJson(typedRequest);

            Debug.Log($"<color=yellow>Req {API} >> {body}</color>");

            info.SetInfo(this, API, packetNum, body, callback);
            return info;
        }

        public void Parse(RequestInfo requestInfo, string responseString)
        {
            var response = JsonUtility.FromJson<RES>(responseString);
            Process(requestInfo, response);
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
    }
}
