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

    public interface ICollapsableRequest : IEquatable<ICollapsableRequest>
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

        RequestInfo CreateRequest(int packetNum);

        RequestInfo CreateRequest(int packetNum, ICollapsableRequest request);
        
        void Parse(RequestInfo requestInfo, string responseString);

        void ErrorProcess(RequestInfo requestInfo, BaseResponse response, int error);

        void ReleasePacket(RequestInfo info);
    }

    public abstract class RequestHandler<REQ, RES> : IRequestHandler where REQ : IRequest where RES : BaseResponse
    {
        public string API => APIAttribute.GetAPI(GetType());

        protected REQ Message { get; set; }

        protected Action<RequestInfo, string> CallbackAction;
        
        private static readonly IObjectPool<RequestInfo> RequestInfoPool = new ObjectPool<RequestInfo>(() => new RequestInfo());

        public void EnqueueRequest(Action<RequestInfo, string> callback = null)
        {
            CallbackAction = callback;
            
            if (typeof(ICollapsableRequest).IsAssignableFrom(typeof(REQ)))
                HttpManager.Instance.EnqueueBundlePacket(this, MakeReqMessage() as ICollapsableRequest);
            else
            {
                Message = MakeReqMessage();
                HttpManager.Instance.EnqueuePacket(this);
            }
        }
        
        protected abstract REQ MakeReqMessage();

        public RequestInfo CreateRequest(int packetNum)
        {
            var info = RequestInfoPool.Get();
            var req = JsonUtility.ToJson(Message);
            Debug.Log($"<color=yellow>Req {API} >> {req}</color>");
            info.SetInfo(API, packetNum, req, CallbackAction);
            return info;
        }

        public RequestInfo CreateRequest(int packetNum, ICollapsableRequest request)
        {
            var info = RequestInfoPool.Get();
            var req = JsonUtility.ToJson(request);
            Debug.Log($"<color=yellow>Req {API} >> {req}</color>");
            info.SetInfo(API, packetNum, req, CallbackAction);
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
    
    public abstract class RequestHandler<REQ, RES, PARAM> : RequestHandler<REQ, RES> where REQ : IRequest where RES : BaseResponse
    {
        protected override REQ MakeReqMessage() => default;
        protected abstract REQ MakeReqMessage(PARAM @params);
        
        public void EnqueueRequest(PARAM param, Action<RequestInfo, string> callback = null)
        {
            if (typeof(ICollapsableRequest).IsAssignableFrom(typeof(REQ)))
                HttpManager.Instance.EnqueueBundlePacket(this, MakeReqMessage(param) as ICollapsableRequest);
            else
            {
                Message = MakeReqMessage(param);
                HttpManager.Instance.EnqueuePacket(this);
            }

            CallbackAction = callback;
        }
    }
}