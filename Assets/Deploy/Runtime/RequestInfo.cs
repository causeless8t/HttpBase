
namespace Causeless3t.Network
{
    public sealed class RequestInfo
    {
        public enum eRequestState
        {
            Ready = 0,
            InProgress,
            Retrying,
            Completed
        }

        public eRequestState State { get; set; }
        public string Protocol { get; private set; }
        public int PacketNumber { get; private set; }
        public byte[] Body { get; private set; }
        public int RetryCount { get; private set; }

        public void SetInfo(string protocol, int packetNum, byte[] body)
        {
            State = eRequestState.Ready;
            Protocol = protocol;
            PacketNumber = packetNum;
            Body = body;
            RetryCount = 0;
        }

        public void Reset()
        {
            State = eRequestState.Ready;
            Protocol = null;
            PacketNumber = 0;
            Body = null;
            RetryCount = 0;
        }

        public void Retry(int packetNum)
        {
            PacketNumber = packetNum;
            State = eRequestState.Retrying;
            RetryCount++;
        }
    }
}
