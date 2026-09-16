
using System;
using UnityEngine;

namespace Causeless3t.Network
{
    public class HttpRequest : MonoBehaviour
    {
        [SerializeField]
        private HttpManager _httpManager;

        [SerializeField]
        private string _baseUrl = "https://api.example.com";

        public void Initialize()
        {
            if (_httpManager == null)
                throw new InvalidOperationException(
                    "HttpManager is not assigned.");

            var options = new HttpManagerOptions(_baseUrl);

            _httpManager.Initialize(
                options,
                typeof(SampleHandler).Assembly);
        }

        public void TestAPI(int param)
        {
            _httpManager.GetHandler<SampleHandler>().EnqueueRequest(param, (req, recv) =>
            {
                Debug.Log($"{req.Protocol}\nrecv => {recv}");
            });
        }
    }
}

