using System;

namespace Causeless3t.Network.Protocol
{
    [Serializable]
    public class BaseRequest : IRequest
    {
        public int packetNumber;

        public BaseRequest()
        {
            packetNumber = HttpManager.Instance.PacketNumber;
        }
    }
}