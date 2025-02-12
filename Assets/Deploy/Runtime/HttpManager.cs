using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using Causeless3t.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Causeless3t.Network
{
    public sealed class HttpManager : Singleton<HttpManager>, IDisposable
    {
        private static readonly int DefaultTimeout = 15;
        private static readonly string DefaultUrl = "https://google.com"; 
        private static readonly int MaxConcurrentPacketCount = 5;
        private static readonly float MaxBundlePacketDuration = 5f;
        private static readonly float RetryTerm = 2f;
        
        private readonly Dictionary<string, IRequestHandler> _requestHandlers = new();
        private readonly Queue<RequestInfo> _requestWaitingQueue = new();
        private readonly Dictionary<string, (IRequestHandler, ICollapsableRequest)> _collapsableRequestDic = new();
        private CancellationTokenSource _delayedPacketCTS;
        private readonly Queue<RequestInfo> _inProgressQueue = new();
        
        private int _packetNumber;
        public int PacketNumber => _packetNumber;

        public void Initialize()
        { 
            var types = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsDefined(typeof(APIAttribute)));
            RegisterRequestHandler(types);
            _packetNumber = 0;
        }

        public void EnqueuePacket(IRequestHandler handler) => _requestWaitingQueue.Enqueue(handler.CreateRequest(_packetNumber++));
        
        public void EnqueueBundlePacket(IRequestHandler handler, ICollapsableRequest request)
        {
            if (!_collapsableRequestDic.TryGetValue(handler.API, out var requestTuple))
            {
                requestTuple = new ValueTuple<IRequestHandler, ICollapsableRequest>
                {
                    Item1 = handler,
                    Item2 = request
                };
                _collapsableRequestDic.Add(handler.API, requestTuple);
                DelayedSendBundlePacket().Forget();
            }
            else if (!requestTuple.Item2.Equals(request))
            {
                _delayedPacketCTS?.Cancel();
                _delayedPacketCTS = null;
                _requestWaitingQueue.Enqueue(requestTuple.Item1.CreateRequest(_packetNumber++, requestTuple.Item2));
                _collapsableRequestDic[handler.API] = new ValueTuple<IRequestHandler, ICollapsableRequest>
                {
                    Item1 = handler,
                    Item2 = request
                };
                DelayedSendBundlePacket().Forget();
            }
            else
                requestTuple.Item2.Collapse(request);
        }
        
        private async UniTask DelayedSendBundlePacket()
        {
            _delayedPacketCTS ??= new();
            await UniTask.WaitForSeconds(MaxBundlePacketDuration, cancellationToken: _delayedPacketCTS.Token);
            Queue<(IRequestHandler, ICollapsableRequest)> collapsedSendingQueue = new();
            foreach (var tuple in _collapsableRequestDic.Values)
                collapsedSendingQueue.Enqueue(tuple);
            _collapsableRequestDic.Clear();
            while (collapsedSendingQueue.Count > 0)
            {
                var requestTuple = collapsedSendingQueue.Dequeue();
                _requestWaitingQueue.Enqueue(requestTuple.Item1.CreateRequest(_packetNumber++, requestTuple.Item2));
                await UniTask.Yield();
            }
            _delayedPacketCTS = null;
        }
        
        private void RegisterRequestHandler(IEnumerable<Type> types)
        {
            _requestHandlers.Clear();
            Debug.Log("Register Request Handler");
            foreach (var type in types)
            {
                Debug.Log($"-> {type.Name}");
                IRequestHandler requestHandler = Activator.CreateInstance(type) as IRequestHandler;
                _requestHandlers.TryAdd(type.GetCustomAttribute<APIAttribute>().API, requestHandler);
                Debug.Log($"-> {type.Name}...DONE");
            }
        }

        public IRequestHandler GetHandler(string api)
        {
            if (!_requestHandlers.TryGetValue(api, out var handler))
            {
                Debug.LogErrorFormat ("{0} doesn't exist recv handler !!", api);
                return null;
            }
            return handler;
        }
        
        public T GetHandler<T>() where T : class, IRequestHandler
        {
            var api = APIAttribute.GetAPI(typeof(T));
            return GetHandler(api) as T;
        }


        public override void OnUpdate()
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
            WWWForm formData = new WWWForm();
            byte[] bytes = new System.Text.UTF8Encoding().GetBytes(info.Body);
            using var www = UnityWebRequest.Post($"{DefaultUrl}{info.Protocol}", formData);
            www.uploadHandler = new UploadHandlerRaw(bytes);
            www.downloadHandler = new DownloadHandlerBuffer();
            // www.SetRequestHeader("Content-Type", "application/json");
            // www.SetRequestHeader("Authorization", $"Bearer {SessionKey}");
            www.useHttpContinue = false;
            www.timeout = DefaultTimeout;
            
            info.State = RequestInfo.eRequestState.InProgress;
            _inProgressQueue.Enqueue(info);
            
            float elapsedTime = Time.realtimeSinceStartup;

            await www.SendWebRequest();

            if ((www.result != UnityWebRequest.Result.Success && www.result != UnityWebRequest.Result.InProgress) || www.error != null)
            {
                string errString = www.error;

                if (Application.internetReachability == NetworkReachability.NotReachable)
                {
                    Debug.LogErrorFormat("network is not reachable !! {0}", errString);
                }
                else
                {
                    Debug.LogErrorFormat("recv error in {0}sec !! {1}", Time.realtimeSinceStartup - elapsedTime, errString);
                }
                RetryProcess(errString, info).Forget();
                return;
            }
            
            while (!www.downloadHandler.isDone)
                await UniTask.Yield();            

            string recv = www.downloadHandler.text;

            if (string.IsNullOrEmpty(recv))
            {
                Debug.LogErrorFormat ("recv is empty in {0}sec !!", Time.realtimeSinceStartup - elapsedTime);
                RetryProcess("recv is empty!!", info).Forget();
                return;
            }

            var handler = GetHandler(info.Protocol);
            if (handler == null && info.CustomCallback == null)
            {
                _inProgressQueue.Dequeue();
                Debug.LogError($"{handler.GetType().Name}핸들러와 커스텀콜백이 존재하지 않아 {handler.API} 를 처리하는데 실패했습니다.");
                return;
            }
            
            var httpStatusCode = (HttpStatusCode)www.responseCode;
            
            try
            {
                ParsePacketProcess(handler, info, httpStatusCode, recv);
            }
            catch(Exception e)
            {
                Debug.LogError($"{handler.GetType().Name}핸들러에서 {handler.API} 를 처리하는데 실패했습니다.");
                Debug.LogError($"{e}");
            }
            finally
            { 
                handler?.ReleasePacket(info); 
                _inProgressQueue.Dequeue();
            } 
        }
        
        private void ParsePacketProcess(IRequestHandler handler, RequestInfo info, HttpStatusCode responseCode, string recvString)
        {
            var baseRes = JsonUtility.FromJson<BaseResponse>(recvString);
            if (handler == null)
            {
                info.CustomCallback?.Invoke(info, recvString);
            }
            else
            {
                if (responseCode != HttpStatusCode.OK)
                    handler.ErrorProcess(info, baseRes, (int)responseCode);
                else if (baseRes.ErrCode != (int)HttpStatusCode.OK && baseRes.ErrCode != 0)
                    handler.ErrorProcess(info, baseRes, baseRes.ErrCode);
                else
                {
                    info.CustomCallback?.Invoke(info, recvString);
                    handler.Parse(info, recvString);
                }
            }
        }
        
        private async UniTask RetryProcess(string error, RequestInfo info)
        {
            _inProgressQueue.Dequeue();

            await UniTask.WaitForSeconds(RetryTerm); // 재시도 간격
            
            if (!info.Retry(_packetNumber++))
            {
                Debug.LogError(error);
                return;
            }
            _requestWaitingQueue.Enqueue(info);
        }

        public void Dispose()
        {
            _delayedPacketCTS?.Cancel();
            _delayedPacketCTS = null;
            
            _requestHandlers.Clear();
            _requestWaitingQueue.Clear();
            _inProgressQueue.Clear();
            _packetNumber = 0;
        }
    }
}
