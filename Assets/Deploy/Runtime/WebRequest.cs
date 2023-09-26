using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using WebProtocol;
using System.Net;
using Common;
using System.Security.Cryptography.X509Certificates;
using System;
using UnityEngine.Networking;

public partial class WebRequest:Singleton<WebRequest>
{
    public static int CurrPacketNum = 0;

    class CertPublicKey : CertificateHandler
    {
        public string PUB_KEY;

        // Encoded RSAPublicKey
        protected override bool ValidateCertificate (byte[] certificateData)
        {
            X509Certificate2 certificate = new X509Certificate2 (certificateData);
            string pk = certificate.GetPublicKeyString ();
            // 인증정보를 무시한다
            return true;
        }
    }

    public delegate void HttpResponseDelegate(string resText);
    public delegate void ResponseDelegate(bool isSuccess);

    public delegate void ResDelegate<T>(bool isSuccess, T recvData) where T : cResponseBase;
    public delegate void HttpResDelegate<T>(T recvData) where T : cResponseBase;

    private UnityWebRequest www = null;
    private string jsonInfo = string.Empty;
    private X509Certificate2 certFile = null;
    private string publicKey = string.Empty;

	UIConfirmPopup currDelayPopup = null;
    const float notifyNetworkDelayTime = 5F;    // timeout
    const float notifyNetworkDelayDeadLine = 18f;// timeout
    private bool isNewVersion = false;

    [HideInInspector]
    public bool isEncryptPacket = true;
    private WebRequestInfo currWebRequestInfo = null;
    private bool isLastPacket = false;
    private int lastPacketAutoRetryCount = 0;
    private int lastPacketAutoRetryMax
    {
        get
        {
            if (TableManager.IsState() && TableManager.Instance.VariableInfoTable != null)
            {
                return TableManager.Instance.VariableInfoTable.NetworkErrorAutoRetryCount;
            }
            else
            {
                return 1; // default
            }
        }
    }

    private IEnumerator requestCoroutine;
    private IEnumerator networkDelayCoroutine;
    //private IEnumerator appguardDelayCoroutine;

    //
    class WebRequestInfo
    {
        public eProtocolType protocolType;
        public cRequestBase sending;
        public HttpResponseDelegate requestDelegate;

        public WebRequestInfo(eProtocolType type, cRequestBase info, HttpResponseDelegate callback)
        {
            protocolType = type;
            sending = info;
            requestDelegate = callback;
            if( WebRequest.CurrPacketNum == int.MaxValue )
                WebRequest.CurrPacketNum = 0;
            sending.packet_number = ++WebRequest.CurrPacketNum;
        }
    }

    Queue<WebRequestInfo> requestQueue = new Queue<WebRequestInfo>();

    void Awake()
    {
        isEncryptPacket = true;
        if (_instance == null)
            _instance = this;        

#if UNITY_IPHONE
        Handheld.SetActivityIndicatorStyle(UnityEngine.iOS.ActivityIndicatorStyle.WhiteLarge);
#elif UNITY_ANDROID
        Handheld.SetActivityIndicatorStyle(AndroidActivityIndicatorStyle.Large);
#endif
        DontDestroyOnLoad(gameObject);

        TextAsset certText = Resources.Load<TextAsset>("Crt/qpyou.cn_crt");
        certFile = new X509Certificate2(certText.bytes);
        publicKey = certFile.GetPublicKeyString ();
    }

    public override void OnDestroy()
    {
        InitWebRequest ();
        base.OnDestroy();
    }

    public void InitWebRequest()
    {
        currDelayPopup = null;
        requestQueue.Clear();
        UniqueClientID = string.Empty;
        UserGuid = 0;
        m_Token = string.Empty;
        TableUrl = string.Empty;

        currWebRequestInfo = null;
        isLastPacket = false;
        lastPacketAutoRetryCount = 0;

        if (requestCoroutine != null)
        {
            StopCoroutine(requestCoroutine);
            requestCoroutine = null;
        }

        if (www != null)
        {
            www.Abort();
            www.Dispose();
            www = null;
        }
    }

    public void HttpRequest<Req, Res>(eProtocolType type, Req reqData, HttpResDelegate<Res> responseDelegate) where Req : class,new() where Res : cResponseBase,new()
    {
        WebRequestInfo reqInfo = new WebRequestInfo(type, reqData as cRequestBase, (recvString) =>
        {
            Res resData = Util.JsonToClass<Res>(recvString);
            responseDelegate(resData);
        });
        requestQueue.Enqueue(reqInfo);
    }

    public bool IsRequestQueueEmpty()
    {
        return (requestQueue.Count == 0);
    }

    void RequestQueueWebRequest()
    {
        if (requestQueue.Count > 0)
        {
            StartWebRequest(requestQueue.Peek());
            SendJsonData (currWebRequestInfo);
        }
    }

    void StartWebRequest(WebRequestInfo info)
    {
        info.sending.token = m_Token;
        info.sending.tableUrl = TableUrl;
#if DEVELOP
        if( isEncryptPacket )
        {
            string[] encryptList = new string[] {"ARAM","YUBIN","DEV2","SEONGYEON","GON"};
            for(int i=0; i<encryptList.Length; ++i )
            {
                if( encryptList[i].Equals(TableManager.Instance.CurrSeverUrlInfo.SeverIndex) )
                {
                    isEncryptPacket = false;
                    break;
                }
            }
        }
#endif
        currWebRequestInfo = info;

#if DEVELOP
        Debug.LogFormat("send >> eProtocolType.{0}, body={1}", info.protocolType, Util.ClassToJson<cRequestBase>(info.sending));
#endif
    }

    void Update()
    {
        if (currWebRequestInfo == null)
        {
            RequestQueueWebRequest ();
        }
    }

    void SendJsonData(WebRequestInfo info)
    {
        if (requestCoroutine != null)
        {
            StopCoroutine(requestCoroutine);
            requestCoroutine = null;
        }

        if (www != null)
        {
            www.Abort();
            www.Dispose();
            www = null;
        }

        //
        jsonInfo = Util.ClassToJson<cRequestBase> (info.sending);
        if (isEncryptPacket)
        {
            jsonInfo = Util.SendString (jsonInfo);
        }
        
        byte[] byteData = Encoding.UTF8.GetBytes (jsonInfo);
        if (isNewVersion)
            www = UnityWebRequest.Put (string.Format ("{0}{1}", Global.TestSeverUrl, GetUrlInfo (info.protocolType)), byteData);
        else
            www = UnityWebRequest.Put (string.Format ("{0}{1}", TableManager.Instance.CurrSeverUrlInfo.WebUrl, GetUrlInfo (info.protocolType)), byteData);
        www.method = "POST";
        //www.downloadHandler = new DownloadHandlerBuffer();
        www.certificateHandler = new CertPublicKey { PUB_KEY = publicKey };
        www.SetRequestHeader ("Content-Type", "application/json");
        www.useHttpContinue = false;

        requestCoroutine = HTTPNetworkCoroutine ();
        StartCoroutine (requestCoroutine);
    }

    IEnumerator HTTPNetworkCoroutine()
    {
        float elapsed_time = Time.realtimeSinceStartup;
        StartNetworkDelayCoroutine ();

        yield return www.SendWebRequest ();

        if (www == null)
        {
            Debug.LogError("www Aborted");
            yield break;
        }

        if (www.isNetworkError || www.isHttpError || www.error != null)
        {
            string errString = www.error;
            www.Dispose();
            www = null;

#if DEVELOP
            if (UserInfoManager.Instance.Guid == 0)
            {
                CreateNetworkErrorOkPopup (Util.GetErrMassage (3) + " (" + errString + ")", Util.RestartApp);
            }
            else
            {
                if (Application.internetReachability == NetworkReachability.NotReachable)
                {
                    CreateNetworkErrorYesNoPopup(Util.GetErrMassage (2010) + " (" + errString + ")");
                }
                else
                {
                    CreateNetworkErrorYesNoPopup(Util.GetErrMassage (2020) + " (" + errString + ")");
                }
            }
#else
            if ( UserInfoManager.Instance.Guid == 0 )
            {
                CreateNetworkErrorOkPopup(Util.GetErrMassage(3), Util.RestartApp);
            }
            else
            {
                if (Application.internetReachability == NetworkReachability.NotReachable)
                {
                    CreateNetworkErrorYesNoPopup(Util.GetErrMassage(2010));
                }
                else
                {
                    CreateNetworkErrorYesNoPopup(Util.GetErrMassage(2020));
                }
            }
#endif

            Debug.LogErrorFormat ("recv error in {0}sec !! {1}", Time.realtimeSinceStartup - elapsed_time, errString);
            yield break;
        }

        StopNetworkDelayCoroutine ();

        if (currWebRequestInfo != null)
        {
            while (!www.downloadHandler.isDone)
                yield return null;            

            string recv = www.downloadHandler.text;

            if (string.IsNullOrEmpty(recv))
            {
                www.Dispose();
                www = null;

                if (isLastPacket)
                {
                    // req_last_packet 요청에 응답이 empty인 경우는, 서버에서 replay해줄 저장된 응답값이 없다는 뜻으로 저장된 응닶값이 유효기간이 지난 케이스.
                    // 패킷이 서버에 안간 경우일지도 모르니, auto-retry 이후에는 마지막 재전송을 시도해본다. 
                    CreateNetworkErrorYesNoPopup(Localization.Get("Error_Msg_0003"), RetryRequestQueueWebRequest);
                }
                else
                {
                    CreateNetworkErrorYesNoPopup(Localization.Get("Error_Msg_0003"));
                }

                yield break;
            }

            try
            {
                if (isEncryptPacket)
                {
                    recv = Util.ReceiveString(recv);
                }
                cResponseBase resp = Util.JsonToClass<cResponseBase>(recv);
                if (resp.error.error != 0)
                    Debug.LogErrorFormat("recv in {0}sec << {1}", Time.realtimeSinceStartup - elapsed_time, recv);
                else
                    Debug.LogFormat("recv in {0}sec << {1}", Time.realtimeSinceStartup - elapsed_time, recv);
            }
            catch (Exception exc)
            {
                Debug.LogException(exc);
                www.Dispose();
                www = null;
                CreateNetworkErrorYesNoPopup(Util.GetErrMassage(2010));
                yield break;
            }

            if (isLastPacket)
            {
                isLastPacket = false;

                cResponseBase resBase = Util.JsonToClass<cResponseBase>(recv);
                WebRequestInfo replayWebRequest = requestQueue.Peek();
                if (replayWebRequest != null)
                {
                    if (replayWebRequest.sending.packet_number == resBase.packet_number)
                    {
                        // 패킷이 이미 서버에 도착했던 케이스
                        replayWebRequest.requestDelegate(recv);
                    }
                    else
                    {
                        // 패킷이 서버에 도착 안했던 케이스
                        www.Dispose();
                        www = null;
                        RetryRequestQueueWebRequest();
                        yield break;
                    }
                }
                else
                {
                    Debug.LogError("Unexpected situation on HTTPNetworkCoroutine() - 1");
                }
            }
            else
            {
                currWebRequestInfo.requestDelegate(recv);
            }
        }
        else
        {
            Debug.LogError("Unexpected situation on HTTPNetworkCoroutine() - 2");
        }

        if (www != null)
        {
            www.Dispose();
            www = null;
        }
        
        requestQueue.Dequeue();
        currWebRequestInfo = null;
        requestCoroutine = null;
    }

	IEnumerator NotifyNetworkDelay() 
    {
        yield return new WaitForSecondsRealtime(notifyNetworkDelayTime);
        if (currWebRequestInfo != null)
        {
			if (currDelayPopup == null) 
			{
                currDelayPopup = ConfirmPopupManager.Instance.CreateConfirmPopup (Util.GetErrMassage(2050), parentObj:GetConfirmPopupRoot());
				currDelayPopup.confirmBtns[(int)UIConfirmPopup.CONFIRM_BTN.OK].gameObject.SetActive(false);
                currDelayPopup.SetBackObjTouchWorking(false);
                currDelayPopup.SetLoadingCircle(true);

                // 아래는 방어코드
                yield return new WaitForSecondsRealtime (notifyNetworkDelayDeadLine);

                if (requestCoroutine != null)
                {
                    StopCoroutine(requestCoroutine);
                    requestCoroutine = null;
                }

                if (www != null)
                {
                    www.Abort();
                    www.Dispose();
                    www = null;
                }

                CloseDelayPopup ();

                if (SceneStackManager.IsState())
                {
                    SceneStackManager.Instance.ClearShortCut();
                }

                //
                networkDelayCoroutine = null;
                CreateNetworkErrorYesNoPopup(Util.GetErrMassage(2020));
                yield break;
            }
		}
        networkDelayCoroutine = null;
    }

    //IEnumerator NotifyNetworkDelay2()
    //{
    //    yield return new WaitForSecondsRealtime(notifyNetworkDelayTime);
    //    if (LoadingScene.IsState() && LoadingScene.Instance.isSendingAppGuardProtocol)
    //    {
    //        if (currDelayPopup == null)
    //        {
    //            currDelayPopup = ConfirmPopupManager.Instance.CreateConfirmPopup(Util.GetErrMassage(2050), parentObj: GetConfirmPopupRoot());
    //            currDelayPopup.confirmBtns[(int)UIConfirmPopup.CONFIRM_BTN.OK].gameObject.SetActive(false);
    //            currDelayPopup.SetBackObjTouchWorking(false);
    //            currDelayPopup.SetLoadingCircle(true);
    //currDelayPopup.SetBackObjTouchWorking(false);

    //            yield return new WaitForSecondsRealtime(notifyNetworkDelayDeadLine);

    //            CloseDelayPopup();

    //            //
    //            if (LoadingScene.IsState() && LoadingScene.Instance.isSendingAppGuardProtocol)
    //            {
    //                LoadingScene.Instance.isSendingAppGuardProtocol = false;
    //                //
    //                currDelayPopup = ConfirmPopupManager.Instance.CreateConfirmPopup(Util.GetErrMassage(2030), CONFIRM_POPUP_TYPE.OK, Util.RestartApp, parentObj: GetConfirmPopupRoot(), eventOnOpenFinished: NetworkUnLock);
    //                if (currDelayPopup != null)
    //                {
    //                    currDelayPopup.SetHighestPopup();
    //                    currDelayPopup.SetConfirmBtnLockCheck(false);
    //currDelayPopup.SetBackObjTouchWorking(false);
    //                }
    //            }
    //        }
    //    }
    //    appguardDelayCoroutine = null;
    //}

    public string GetUrlInfo( eProtocolType type )
    {
        switch(type)
        {
            case eProtocolType.USER:
                return "user/";
            case eProtocolType.STAGE:
                return "stage/";
            case eProtocolType.CHARACTER:
                return "character/";
            case eProtocolType.CHEAT:
                return "cheat/";
            case eProtocolType.TABLE:
                return "table/";
            case eProtocolType.ITEM:
                return "item/";
            case eProtocolType.GM:
                return "gm/";
            case eProtocolType.PVP:
                return "pvp/";
            case eProtocolType.FRIEND:
                return "friend/";
            case eProtocolType.STORE:
                return "store/";
			case eProtocolType.MAIL:
				return "mail/";
			case eProtocolType.EVENT:
				return "event/";
            case eProtocolType.COMMENT:
                return "comment/";
            case eProtocolType.GUILD:
                return "guild/";
            case eProtocolType.GUILDRAID:
                return "guild_raid/";
            case eProtocolType.PACKAGE:
                return "package/";
            case eProtocolType.GUILDMATCH:
                return "guild_battle/";
            case eProtocolType.ETHER:
                return "ether/";
            case eProtocolType.GACHA:
                return "gacha/";
            case eProtocolType.ACADEMYCHARACTER:
                return "academy_character/";
            case eProtocolType.BOSSRAID:
                return "boss_raid/";
            case eProtocolType.DECK:
                return "deck/";
            case eProtocolType.PASS:
                return "pass/";
            case eProtocolType.SWEEP:
                return "sweep/";
            case eProtocolType.ELEMENTSTONE:
                return "element_stone/";
            case eProtocolType.CONNECTION:
                return "connection/";
            case eProtocolType.LOLLIPOP:
                return "lollipop/";
        }
        return string.Empty;
    }

    private bool ResponseErrorCheck(cError error,int stringID)
    {
        if (error.error == 0)
        {
            return true;
        }
        switch(error.action)
        {
            case eAction.NONE:
                {
                    CreateNetworkErrorOkPopup(string.Format("{0}({1})",Util.GetErrMassage(stringID),error.error), null);
                }
                break;
            case eAction.RESTART:
                {
                    CreateNetworkErrorOkPopup(string.Format("{0}({1})",Util.GetErrMassage(stringID),error.error), Util.RestartApp);
                }
                break;
            case eAction.OFF:
                {
                    CreateNetworkErrorOkPopup(string.Format("{0}({1})",Util.GetErrMassage(stringID),error.error), Application.Quit);
                }
                break;
            case eAction.TOWN:
                {
                    CreateNetworkErrorOkPopup(string.Format("{0}({1})",Util.GetErrMassage(stringID),error.error), 
                        () =>
                        {
                            if(TownScene.IsState())
                            {
                                MoveTownMain();
                            }
                            else
                            {
                                SceneStackManager.Instance.PopScenesUntilTown();
                            }
                        });
                }
                break;
			case eAction.UPDATE_TABLE:
				{
                    CreateNetworkErrorOkPopup(Util.GetErrMassage(stringID), Util.RestartApp); // 재부팅 처리가 간결해졌고, 로직에 통일성을 유지하기 좋아 변경
                    //TableManager.Instance.UpdateLatestTable (Util.GetErrMassage(stringID)); // cdn처리가 안되는 등 사이드 있음
				}
				break;
            case eAction.PASS:
                return false;
            case eAction.RELOAD:
                {
                    CreateNetworkErrorOkPopup(string.Format("{0}({1})",Util.GetErrMassage(stringID),error.error), 
                        () => { 
                        if (TownScene.IsState())
                        {
                            SceneStackManager.Instance.PushScene (SCENE_ID.TOWN);
                        }
                        else
                            ShortCutSystem.AutoMove(MOVE_INDEX.LOBBY);}
                    );
                }
                break;
            case eAction.PACKAGE_PURCHASE_DUPLICATION:
                return true;
            case eAction.PACKAGE_NOT_FOR_SALE:
                {
                    CreateNetworkErrorOkPopup(string.Format("{0}({1})",Util.GetErrMassage(stringID),error.error), ()=> {
                        if (TownScene.IsState ())
                        {
                            Transform popupTrans = TownScene.Instance.uiCamera.transform.Find ("PackagePopup_Normal(Clone)");
                            if (popupTrans == null)
                                popupTrans = TownScene.Instance.uiCamera.transform.Find ("PackagePopup_Interrupt(Clone)");
                            if (popupTrans != null)
                                popupTrans.GetComponent<AbstractUIPackagePopup> ().OnBtnBackClicked ();
                        }
                    });
                }
                break;

            case eAction.GUILD_LOBBY:
                {
                    CreateNetworkErrorOkPopup(string.Format("{0}({1})", Util.GetErrMassage(stringID), error.error), () => {
                        if(SceneStackManager.IsState() && TownScene.IsState())
                        {
                            MoveTownProcess(() =>
                            {
                                TownScene.Instance.ComeBackHome(true);
                                ShortCutSystem.AutoMove(MOVE_INDEX.GUILD);
                            });
                        }
                    });
                }
                break;

            case eAction.GUILD_BATTLE_LOBBY:
                {
                    CreateNetworkErrorOkPopup(string.Format("{0}({1})", Util.GetErrMassage(stringID), error.error), () => {
                        if(SceneStackManager.IsState() && TownScene.IsState())
                        {
                            MoveTownProcess(() =>
                            {
                                TownScene.Instance.ComeBackHome(true);
                                ShortCutSystem.AutoMove(MOVE_INDEX.GUILDBATTLELOBBY);
                            });
                        }
                    });
                }
                break;
        }
        return false;
    }

    /// <summary>
    /// 전투에서 숏컷 이동 시 전투 리소스를 해제하기 위해 이 프로세스를 통해서 타운으로 가야함.
    /// </summary>
    /// <param name="onEnd"></param>
    private void MoveTownProcess(Action onEnd)
    {
        if(TownScene.Instance.battleObject.activeSelf)
        {
            if(TownScene.Instance.battleEngine != null && TownScene.Instance.battleEngine.controller != null)
            {
                TownScene.Instance.battleEngine.controller.Clear(() =>
                {
                    if(onEnd != null)
                    {
                        onEnd();
                    }
                });
            }
        }
        else
        {
            onEnd();
        }
    }

    public void MoveTownMain()
    {
        TownScene.Instance.RefreshAll -= MoveTownMain;
        MoveTownProcess(() =>
        {
            TownScene.Instance.ComeBackHome();
        });
    }

    public bool ResponseErrorCheck(int result,eAction action = eAction.NONE)
    {
        cError err = new cError();
        err.error = result;
        err.action = action;
        return ResponseErrorCheck(err, result / 1000);
    }

    private void CreateNetworkErrorOkPopup(string msg, Action okAction)
    {        
        StopNetworkDelayCoroutine();

        if(SceneStackManager.IsState())
        {
            SceneStackManager.Instance.ClearShortCut();
        }

        if (TutorialManager.IsState() && TutorialManager.Instance.IsIng())
        {
            UITutorialPanel tutoPanel = TutorialManager.Instance.GetTutorialPanel();
            if (tutoPanel != null)
            {
                tutoPanel.CreateNetworkErrorOkPopup(msg, () => { NetworkLock(okAction); }, NetworkUnLock); 
            }
        }
        else
        {
            UIConfirmPopup popup = ConfirmPopupManager.Instance.CreateConfirmPopup(msg, CONFIRM_POPUP_TYPE.OK, () => { NetworkLock(okAction); }, parentObj:GetConfirmPopupRoot(), eventOnOpenFinished:NetworkUnLock);

            if (popup != null)
            {
                popup.SetHighestPopup();
                popup.SetConfirmBtnLockCheck(false);
                popup.SetBackObjTouchWorking(false);
            }
        }
    }

    public void CreateNetworkErrorYesNoPopup(string msg, System.Action retryAction = null)
    {
        StopNetworkDelayCoroutine();

        if (!isLastPacket)
        {
            lastPacketAutoRetryCount = 0;
        }

        if (lastPacketAutoRetryCount < lastPacketAutoRetryMax)
        {
            ++lastPacketAutoRetryCount;

            // auto-retry
            RequestLastPacket();
            return;
        }

        if (TutorialManager.IsState() && TutorialManager.Instance.IsIng())
        {
            UITutorialPanel tutoPanel = TutorialManager.Instance.GetTutorialPanel();
            if (tutoPanel != null)
            {
                tutoPanel.CreateNetworkErrorYesNoPopup(msg, Localization.Get("UI_Common_NetworkConnect_Retry"), Localization.Get("UI_Common_GameRestart"), () => { NetworkLock(retryAction != null ? retryAction : RequestLastPacket); }, Util.RestartApp,NetworkUnLock); 
            }
        }
        else
        {
            if (requestQueue.Count > 0)
            {
                UIConfirmPopup popup = ConfirmPopupManager.Instance.CreateConfirmPopup(msg, CONFIRM_POPUP_TYPE.YES_NO, null, () => { NetworkLock(retryAction != null ? retryAction : RequestLastPacket); }, Util.RestartApp, parentObj: GetConfirmPopupRoot(), eventOnOpenFinished: NetworkUnLock);
                if (popup != null)
                {
                    popup.SetYesBtnLabel(Localization.Get("UI_Common_NetworkConnect_Retry"));
                    popup.SetNoBtnLabel(Localization.Get("UI_Common_GameRestart"));
                    popup.SetHighestPopup();
                    popup.SetConfirmBtnLockCheck(false);
                    popup.SetBackObjTouchWorking(false);
                }
            }
            else
            {
                UIConfirmPopup popup = ConfirmPopupManager.Instance.CreateConfirmPopup(Util.GetErrMassage(2030), CONFIRM_POPUP_TYPE.OK, Util.RestartApp, parentObj: GetConfirmPopupRoot(), eventOnOpenFinished: NetworkUnLock);
                if (popup != null)
                {
                    popup.SetHighestPopup();
                    popup.SetConfirmBtnLockCheck(false);
                    popup.SetBackObjTouchWorking(false);
                }
            }
        }
    }

    //public void StartNetworkDelay2Coroutine()
    //{
    //    if (appguardDelayCoroutine != null)
    //        StopCoroutine (appguardDelayCoroutine);
    //    appguardDelayCoroutine = NotifyNetworkDelay2 ();
    //    CloseDelayPopup ();

    //    //
    //    StartCoroutine(appguardDelayCoroutine);
    //}

    //public void StopNetworkDelay2Coroutine()
    //{
    //    if (appguardDelayCoroutine != null)
    //        StopCoroutine (appguardDelayCoroutine);
    //    appguardDelayCoroutine = null;
    //    CloseDelayPopup ();
    //}

    private void StartNetworkDelayCoroutine()
    {
        if (networkDelayCoroutine != null)
            StopCoroutine (networkDelayCoroutine);
        networkDelayCoroutine = NotifyNetworkDelay ();
        CloseDelayPopup();

        //
        StartCoroutine (networkDelayCoroutine);
    }

    private void StopNetworkDelayCoroutine()
    {
        if (networkDelayCoroutine != null)
            StopCoroutine (networkDelayCoroutine);
        networkDelayCoroutine = null;
        CloseDelayPopup ();
    }

    private void CloseDelayPopup()
    {
        if (currDelayPopup != null)
        {
            if (ConfirmPopupManager.Instance.GetCurrentPopup() == currDelayPopup)
                ConfirmPopupManager.Instance.ClosePopup();
            else
                ConfirmPopupManager.Instance.ClosePopupAll(false);

            currDelayPopup = null;
        }
    }

    private GameObject GetConfirmPopupRoot()
    {
        if (SwitchScene.IsState())
        {
            return SwitchScene.Instance.popupRootObj;   // when network error occur
        }
        else
        {
            return null;    // use default
        }
    }

    public void NetworkUnLock()
    {
        Util.NetworkLockInput(true);
    }

    public void NetworkLock(Action action)
    {
        Util.NetworkLockInput(false);
        if (action != null)
            action();
    }

    private void RequestLastPacket()
    {
        isLastPacket = true;
        cRequestLastPacket request = new cRequestLastPacket();
        request.guid = UserInfoManager.Instance.Guid;
        StartWebRequest(new WebRequestInfo(eProtocolType.USER, request, null));
        SendJsonData(currWebRequestInfo);
        Debug.Log("패킷 재요청을 위한 사전 패킷 전송");
    }

    private void RetryRequestQueueWebRequest()
    {
        isLastPacket = false;
        RequestQueueWebRequest();
    }
}