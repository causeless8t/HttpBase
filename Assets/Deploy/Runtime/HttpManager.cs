using System;
using System.Collections.Generic;
using System.Reflection;
using Causeless3t.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Causeless3t.Network
{
    public sealed class HttpManager : MonoSingleton<HttpManager>
    {
        private static readonly string DefaultUrl = "https://google.com"; 
        private static readonly int MaxConcurrentPacketCount = 5;
        
        private readonly Dictionary<string, BaseRequestHandler> _requestHandlers = new();
        private readonly Queue<RequestInfo> _requestWaitingQueue = new();
        private readonly Queue<RequestInfo> _inProgressQueue = new();

        private int _packetNumber;

        public void Initialize(IEnumerable<Type> types)
        { 
            RegisterRequestHandler(types);
            _packetNumber = 0;
        }

        public void EnqueuePacket(BaseRequestHandler handler) => _requestWaitingQueue.Enqueue(handler.CreateRequest(_packetNumber++));
        
        private void RegisterRequestHandler(IEnumerable<Type> types)
        {
            _requestHandlers.Clear();
            Debug.Log("Register Request Handler");
            foreach (var type in types)
            {
                Debug.Log($"-> {type.Name}");
                BaseRequestHandler requestHandler = Activator.CreateInstance(type) as BaseRequestHandler;
                _requestHandlers.TryAdd(type.GetCustomAttribute<APIAttribute>().API, requestHandler);
                Debug.Log($"-> {type.Name}...DONE");
            }
        }

        public BaseRequestHandler GetHandler(string api)
        {
            if (!_requestHandlers.TryGetValue(api, out var handler))
            {
                Debug.LogErrorFormat ("{0} doesn't exist recv handler !!", api);
                return null;
            }
            return handler;
        }
        
        private void Update()
        {
            for (int i=0; i<_requestWaitingQueue.Count; ++i)
            {
                if (_inProgressQueue.Count >= MaxConcurrentPacketCount)
                    break;
                var request = _requestWaitingQueue.Dequeue();
                SendPacket(request).Forget();
            }
        }
        
        private async UniTask SendPacket(RequestInfo info)
        { 
            var www = UnityWebRequest.Put($"{DefaultUrl}/{info.Protocol}", info.Body);
            www.method = "POST";
            www.SetRequestHeader("Content-Type", "application/json");
            www.useHttpContinue = false;
            
            info.State = RequestInfo.eRequestState.InProgress;
            _inProgressQueue.Enqueue(info);
            
            float elapsedTime = Time.realtimeSinceStartup;

            await www.SendWebRequest();

            if ((www.result != UnityWebRequest.Result.Success && www.result != UnityWebRequest.Result.InProgress) || www.error != null)
            {
                string errString = www.error;
                www.Dispose();

                if (Application.internetReachability == NetworkReachability.NotReachable)
                {
                    Debug.LogErrorFormat("network is not reachable !! {0}", errString);
                }
                else
                {
                    Debug.LogErrorFormat("recv error in {0}sec !! {1}", Time.realtimeSinceStartup - elapsedTime, errString);
                }
                return;
            }
            
            while (!www.downloadHandler.isDone)
                await UniTask.Yield();            

            string recv = www.downloadHandler.text;

            if (string.IsNullOrEmpty(recv))
            {
                www.Dispose();

                Debug.LogErrorFormat ("recv is empty in {0}sec !!", Time.realtimeSinceStartup - elapsedTime);
                return;
            }

            var handler = GetHandler(info.Protocol);
            if (handler != null)
            {
                handler.Parse(info, recv);
                handler.ReleasePacket(info);
            }

            www.Dispose();
            _inProgressQueue.Dequeue();
        }
    }
}
