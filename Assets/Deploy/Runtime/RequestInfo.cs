
namespace Causeless3t.Network
{
    public sealed class RequestInfo
    {
        public cRequestBase sending;
        public HttpResponseDelegate requestDelegate;

        public RequestInfo(eProtocolType type, cRequestBase info, HttpResponseDelegate callback)
        {
            protocolType = type;
            sending = info;
            requestDelegate = callback;
            if( WebRequest.CurrPacketNum == int.MaxValue )
                WebRequest.CurrPacketNum = 0;
            sending.packet_number = ++WebRequest.CurrPacketNum;
        }
    }
}
