
using Causeless3t.Core;
using UnityEngine;

namespace Causeless3t.Network
{
    public class HttpRequest : Singleton<HttpRequest>
    {
        [SerializeField]
        private string _baseUrl = "https://api.example.com";

        public void Initialize()
        {
            var options = new HttpManagerOptions(_baseUrl);

            HttpManager.Instance.Initialize(
                options,
                typeof(SampleHandler).Assembly);
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

