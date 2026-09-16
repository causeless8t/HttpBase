
using System;

namespace Causeless3t.Network
{
    public sealed class RequestInfo
    {
        public enum eRequestState
        {
            Ready = 0,
            InProgress,
            Retrying,
        }

        public eRequestState State { get; set; }
        internal IRequestHandler Handler { get; private set; }
        public string Protocol { get; private set; }
        public int PacketNumber { get; private set; }
        public string Body { get; private set; }
        public int RetryCount { get; private set; }
        
        public Action<RequestInfo, string> CustomCallback { get; private set; }

        public void SetInfo(
            IRequestHandler handler,
            string protocol,
            int packetNum,
            string body,
            Action<RequestInfo, string> callback = null)
        {
            Handler = handler ?? throw new ArgumentNullException(nameof(handler));
            State = eRequestState.Ready;
            Protocol = protocol;
            PacketNumber = packetNum;
            Body = body;
            RetryCount = 0;
            CustomCallback = callback;
        }

        public void Reset()
        {
            Handler = null;
            State = eRequestState.Ready;
            Protocol = null;
            PacketNumber = 0;
            Body = null;
            RetryCount = 0;
            CustomCallback = null;
        }

        public bool TryPrepareRetry(
            int packetNum,
            int maxRetryAttempts)
        {
            if (maxRetryAttempts < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxRetryAttempts));
            }

            if (RetryCount >= maxRetryAttempts)
                return false;

            RetryCount++;
            PacketNumber = packetNum;
            State = eRequestState.Retrying;
            return true;
        }
    }
}
