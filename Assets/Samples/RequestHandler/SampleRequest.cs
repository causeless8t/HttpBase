using System;
using System.Reflection;
using UnityEngine;

namespace Causeless3t.Network
{
    [API("sample/test")]
    public class SampleRequest : BaseRequestHandler
    {
        protected override string API => GetType().GetCustomAttribute<APIAttribute>().API;
        
        public int SamepleParam { get; set; }

        public override RequestInfo CreateRequest(int packetNum)
        {
            var info = base.CreateRequest(packetNum);
            info.SetInfo(API, packetNum, BitConverter.GetBytes(SamepleParam));
            return info;
        }

        public override void Parse(RequestInfo requestInfo, string response)
        {
            Debug.Log($"sample response {response}");
        }
    }
}

