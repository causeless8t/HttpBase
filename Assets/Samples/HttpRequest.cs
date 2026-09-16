
using Causeless3t.Core;
using UnityEngine;

namespace Causeless3t.Network
{
    public class HttpRequest : Singleton<HttpRequest>
    {
        public void Initialize()
        {
            HttpManager.Instance.Initialize(typeof(SampleHandler).Assembly);
        }

        public void TestAPI(int param)
        {
            HttpManager.Instance.GetHandler<SampleHandler>().EnqueueRequest(param, (req, recv) =>
            {
                Debug.Log($"{req.Protocol}\nrecv => {recv}");
            });
        }
    }
}

