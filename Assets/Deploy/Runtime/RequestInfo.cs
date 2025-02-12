
using System;

namespace Causeless3t.Network
{
    public sealed class RequestInfo
    {
        private static readonly int MaxRetryCount = 3;
        
        public enum eRequestState
        {
            Ready = 0,
            InProgress,
            Retrying,
        }

        public eRequestState State { get; set; }
        public string Protocol { get; private set; }
        public int PacketNumber { get; private set; }
        public string Body { get; private set; }
        public int RetryCount { get; private set; }
        
        public Action<RequestInfo, string> CustomCallback { get; private set; }

        public void SetInfo(string protocol, int packetNum, string body, Action<RequestInfo, string> callback = null)
        {
            State = eRequestState.Ready;
            Protocol = protocol;
            PacketNumber = packetNum;
            Body = body;
            RetryCount = 0;
            CustomCallback = callback;
        }

        public void Reset()
        {
            State = eRequestState.Ready;
            Protocol = null;
            PacketNumber = 0;
            Body = null;
            RetryCount = 0;
            CustomCallback = null;
        }

        public bool Retry(int packetNum)
        {
            PacketNumber = packetNum;
            State = eRequestState.Retrying;
            return ++RetryCount < MaxRetryCount;
        }
    }
}
