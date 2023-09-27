
using Causeless3t.Core;

namespace Causeless3t.Network
{
    public class HttpRequest : Singleton<HttpRequest>
    {
        public void TestAPI(int param)
        {
            if (HttpManager.Instance.GetHandler("sample/test") is not SampleRequest handler) return;
            handler.SamepleParam = param;
            HttpManager.Instance.EnqueuePacket(handler);
        }
    }
}

