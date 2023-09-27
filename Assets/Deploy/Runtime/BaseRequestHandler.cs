using System;
using UnityEngine.Pool;

namespace Causeless3t.Network
{
    [AttributeUsage(AttributeTargets.Class)]
    public class APIAttribute : Attribute
    {
        public string API { get; }

        public APIAttribute(string api) => API = api;
    }

    public abstract class BaseRequestHandler
    {
        protected abstract string API { get; }
        
        protected static readonly IObjectPool<RequestInfo> RequestInfoPool = new ObjectPool<RequestInfo>(() => new RequestInfo());

        public virtual RequestInfo CreateRequest(int packetNum) => RequestInfoPool.Get();

        public abstract void Parse(RequestInfo requestInfo, string response);

        public virtual void ErrorProcess(RequestInfo requestInfo, string response, int error) { }

        public virtual void ReleasePacket(RequestInfo info)
        {
            info.Reset();
            RequestInfoPool.Release(info);
        }
    }
}