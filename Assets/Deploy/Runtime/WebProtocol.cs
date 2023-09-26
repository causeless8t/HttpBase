using System;
using System.Collections;
using System.Collections.Generic;
using ServerCommon;

namespace WebProtocol
{
    public enum eProtocolType
    {
        USER,
        STAGE,
        CHARACTER,
        CHEAT,
        TABLE,
        ITEM,
        GM,
        PVP,
        FRIEND,
        STORE,
        MAIL,
        EVENT,
        COMMENT,
        GUILD,
        GUILDRAID,
        PACKAGE,
        ETHER,
        GUILDMATCH,
        GACHA,
        ACADEMYCHARACTER,
        BOSSRAID,
        DECK,
        PASS,
        SWEEP,
        ELEMENTSTONE,
        CONNECTION,
        LOLLIPOP
    }

    public enum eAction
    {
        NONE,
        RESTART,
        OFF,
        TOWN,
        UPDATE_TABLE,
        PASS,
        RELOAD,
        PACKAGE_PURCHASE_DUPLICATION,
        PACKAGE_NOT_FOR_SALE,
        GUILD_LOBBY,
        GUILD_BATTLE_LOBBY
    }

    public class cRequestBase
    {
        public string version = "2.0.12";
        public string tableUrl;
        public string token;
        public int packet_number;
    }

    public class cError
    {
        public int error;
        public eAction action;
    }

    public class cResponseBase
    {
        public cError error;
        public string token;
        public long time;   //동기화를 위한 서버 시간
        public cWebNotify notify = new cWebNotify();
        public int packet_number;

        public int GetErrStringID()
        {
            return error.error / 1000;
        }

    }

    #region user
    ///////////////////////////////////////////////////////////////////////////////
    //  url : address + '/user'
    ///////////////////////////////////////////////////////////////////////////////
    public class cRequestCdnUrl : cRequestBase
    {
        public string cmd = "req_cdn_url";
    }

    public class cResponseCdnUrl : cResponseBase
    {
        public string url;
    }

    public class cRequestIOSCdnUrl : cRequestBase
    {
        public string cmd = "req_ios_cdn_url";
    }

    public class cResponseIOSCdnUrl : cResponseBase
    {
        public bool isNewVersion = false;
        public string url;
    }

    public class cRequestCheckAccount : cRequestBase
    {
        public string cmd = "req_check_account";
        public string id;
    }

    public class cResponseCheckAccount : cResponseBase
    {
        public bool exist;
    }

    public class cRequestLoginWithHive : cRequestBase
    {
        public string cmd = "req_login_hive";
        public cAuthInfoWithHive authInfo = new cAuthInfoWithHive();
    }

    public class cResponseLoginWithHive : cResponseBase
    {
        public long guid;
        public string uniqueClientID = "";
        public bool existingAccount;    // 최초 생성 계정이면 true
        public int pending_count;
    }

    public class cRequestLogin : cRequestBase
    {
        public string cmd = "req_login";
        public string id;
        public byte grade = 1;
    }

    public class cResponseLogin : cResponseBase
    {
        public long guid;
        public bool existingAccount; // 최초 생성 계정이면 true
        public int pending_count;
    }

    public class cRequestCreateNickname : cRequestBase
    {
        public string cmd = "req_create_nickname";
        public string portalmasterName;
        public long academyCharacterID;
    }

    public class cResponseCreateNickname : cResponseBase
    {
    }

    //17.03.17 추가. 유져의 전체 데이터를 패킷을 나눠서 받지 않고 한번에 받을 수 있는 패킷.
    public class cRequestUserAllData : cRequestBase
    {
        public string cmd = "req_user_all_data";
    }

    public class cResponseUserAllData : cResponseBase
    {
        public cUserData userData = new cUserData();
        public List<cCharacter> characters = new List<cCharacter>();
        public List<cItem> items = new List<cItem>();
        public List<cEther> ethers = new List<cEther>();
        public List<cMission> missions = new List<cMission>();
        public cTrainingMissionInfo training = new cTrainingMissionInfo();
        public cTutorial tutorial = new cTutorial();
        public List<cAcademyCharacter> academyCharacters = new List<cAcademyCharacter>();
        public cUserLobbyDeck userLobbyDeck = new cUserLobbyDeck();
        public List<cDeck> deckList = new List<cDeck>();
        public int etherInventory;
        public List<cLobbyBg> lobbyBgDataList = new List<cLobbyBg>();
        public cGameGuide gameGuide = new cGameGuide();
    }

    //21.03.30 추가
    public class cRequestUserCharacterCategoryData : cRequestBase
    {
        public string cmd = "req_user_character_category_data";
    }
    public class cResponseUserCharacterCategoryData : cResponseBase
    {
        public cUserData userData = new cUserData();
        public List<cAcademyCharacter> academyCharacters = new List<cAcademyCharacter>();
        public List<cCharacter> characters = new List<cCharacter>();
        public cUserLobbyDeck userLobbyDeck = new cUserLobbyDeck();
        public List<cDeck> deckList = new List<cDeck>();
        public List<cLobbyBg> lobbyBgDataList = new List<cLobbyBg>();
    }

    public class cRequestItemCategoryData : cRequestBase
    {
        public string cmd = "req_user_item_category_data";
    }
    public class cResponseUserItemCategoryData : cResponseBase
    {
        public List<cItem> items = new List<cItem>();
        public List<cEther> ethers = new List<cEther>();
        public int etherInventory;
    }

    public class cRequestUserStateCategoryData : cRequestBase
    {
        public string cmd = "req_user_state_category_data";
    }
    public class cResponseUserStateCategoryData : cResponseBase
    {
        public List<cMission> missions = new List<cMission>();
        public cTrainingMissionInfo training = new cTrainingMissionInfo();
        public cTutorial tutorial = new cTutorial();
        public cGameGuide gameGuide = new cGameGuide();
    }

    public class cRequestStoreData : cRequestBase
    {
        public string cmd = "req_store_data";
    }

    public class cResponseStoreData : cResponseBase
    {
        public List<cGachaStore> gachastores = new List<cGachaStore>();
        public List<cGachaStoreEventInfo> eventinfo = new List<cGachaStoreEventInfo>();
        public List<cStore> storeInfo = new List<cStore>();
        public int extraResetCount;
    }

    public class cRequestStageData : cRequestBase
    {
        public string cmd = "req_stage_data";
    }

    public class cResponseStageData : cResponseBase
    {
        public long towerinittime;        // 1달에 1번 초기화 되는 도전형 던전의 초기화 시간, gamedev를 제외한 나머지는 하루에 1번으로 셋팅됨.
        public short towerMonthNormal;    // 이번 달 최초 달성 층 - normal
        public short towerMonthHard;    // 이번 달 최초 달성 층 - hard
        public short towerFirstNormal;    // 계정 최초 달성 층 - normal
        public short towerFirstHard;    // 계정 최초 달성 층 - hard
        public List<cStage> stages = new List<cStage>();
        public List<cThemeStarReward> theme_star_reward = new List<cThemeStarReward>();
        public cStageEntry stageEntry = new cStageEntry();
        public cStageEntry hotTimeEntry = new cStageEntry();    // 핫타임 용 Entry - 핫타임 진행중에만 보내줌.
        public byte elemental_rewarded_count = 0;    // 엘레멘탈(속성) 던전 보상 받은 횟수 0~2
        public List<cStage> elemental_stage = new List<cStage>();
        public List<cStage> towerStage = new List<cStage>();
        public List<cThemeStarReward> towerStarReward = new List<cThemeStarReward>();
        public List<cTowerMission> towerMissionList = new List<cTowerMission>();
        public List<cStage> episodeStage = new List<cStage>();
        public List<cThemeStarReward> episodeStarReward = new List<cThemeStarReward>();
        public int goldSum;             // 골드 던전 - 모험 골드 누적.
    }

    public class cRequestStageIDData : cRequestBase
    {
        public string cmd = "req_stage_id_data";
    }

    public class cResponseStageIDData : cResponseBase
    {
        public List<int> stages = new List<int>();
        public List<int> elemental_stage = new List<int>();
        public List<int> towerStage = new List<int>();
        public List<int> episodeStage = new List<int>();
    }

    public class cRequestFriendData : cRequestBase
    {
        public string cmd = "req_friend_data";
    }

    public class cResponseFriendData : cResponseBase
    {
        public List<cFriend> friends = new List<cFriend>();
    }

    public class cRequestCollectionData : cRequestBase
    {
        public string cmd = "req_collection_data";
    }

    public class cResponseCollectionData : cResponseBase
    {
        public List<cCollection> collections = new List<cCollection>();            // 인연 정보
    }

    public class cRequestDialog : cRequestBase
    {
        public string cmd = "req_dialog";
        public int dialogid;
    }

    public class cResponseDialog : cResponseBase
    {
        public List<int> dialog = new List<int>();
    }

    public class cRequestContentOpen : cRequestBase
    {
        public string cmd = "req_content";
        public List<int> openIds = new List<int>();
    }

    public class cResponseContentOpen : cResponseBase
    {
        public List<int> openContent = new List<int>();
    }

    public class cRequestMissionList : cRequestBase
    {
        public string cmd = "req_mission_list";
    }

    public class cResponseMissionList : cResponseBase
    {
        public long nextWeekResetTime;
        public long nextDayResetTime;
        public long exchangeID;
        public short stamp;
        public List<cMission> missions = new List<cMission>();
    }

    public class cRequestLobbyMissionList : cRequestBase
    {
        public string cmd = "req_lobby_mission_list";
    }

    public class cResponseLobbyMissionList : cResponseBase
    {
        public long exchangeID;
        public short stamp;
        public byte day;
        public long completeTime;
        public List<cMission> termMissions = new List<cMission>();
        public List<cTrainingMission> trainingMissions = new List<cTrainingMission>();
    }

    public class cRequestMissionReward : cRequestBase
    {
        public string cmd = "req_mission_reward";
        public byte missionType;
        public List<long> missionIds = new List<long>();
    }

    public class cResponseMissionReward : cResponseBase
    {
        public cItemEtherReward reward = new cItemEtherReward();
        public short stamp;
        public List<cMission> missions = new List<cMission>();
    }

    public class cRequestStampReward : cRequestBase
    {
        public string cmd = "req_stamp_reward";
        public long exchangeID;
    }

    public class cResponseStampReward : cResponseBase
    {
        public cItemEtherReward reward = new cItemEtherReward();
        public short stamp;
        public long exchangeID;
    }

    public class cRequestRefreshEnergy : cRequestBase
    {
        public string cmd = "req_refresh_energy";
    }

    public class cResponseRefreshEnergy : cResponseBase
    {
        public int energy;
        public long energy_recovertime;
    }

    public class cRequestRefreshGear : cRequestBase
    {
        public string cmd = "req_refresh_gear";
    }

    public class cResponseRefreshGear : cResponseBase
    {
        public int gear;
        public long gear_recovertime;
    }

    public class cRequestGetChattingServerInfo : cRequestBase
    {
        public string cmd = "req_get_chatting_server_info";
    }

    public class cResponseGetChattingServerInfo : cResponseBase
    {
        public cChattingServerInfo chatinfo = new cChattingServerInfo();
    }

    public class cRequestRefreshAll : cRequestBase
    {
        public string cmd = "req_refresh_all";
    }

    public class cResponseRefreshAll : cResponseBase
    {
        /// 추후 추가할 데이터들
        public bool isNewMail;
        public bool isNewFriend;
        public cAttendanceResult attendanceResult = new cAttendanceResult();
        public cAttendanceReturnResult attendanceReturnResult = new cAttendanceReturnResult();

        public List<cCheckNotice> checkNotice = new List<cCheckNotice>();
        public List<cTextNotice> textNotice = new List<cTextNotice>();

        public long guildDBKey;
        public string guildName;
        public int guildLevel;

        public long dayinittime;
        public long weekinittime;
        public int day;
        public int period;
        public string localtime;

        public long guardTime;
        public List<cGuard> guardList = new List<cGuard>();

        public int guardRewardMaxTime;
        public int guardSpecialRewardTime;
    }

    public class cRequestRedDotInfo : cRequestBase
    {
        public string cmd = "req_red_dot_info";
    }

    public class cResponseRedDotInfo : cResponseBase
    {
        public List<int> redDotList = new List<int>();
    }

    public class cRequestTutorialLog : cRequestBase
    {
        public string cmd = "req_tutorial_log";
        public long tutorialid;
        public int step;
        public string did;          // cAuthInfoWithHive 클래스의 did
        public long player_id = 0;    // cAuthInfoWithHive 클래스의 player_id
    }

    public class cResponseTutorialLog : cResponseBase
    {
        public long tutorial;
    }

    public class cRequestTutorial : cRequestBase        // 유져데이터 처리용 튜토리얼 패킷
    {
        public string cmd = "req_tutorial";
        public cTutorial tutorial = new cTutorial();
    }

    public class cResponseTutorial : cResponseBase
    {
        public cTutorial tutorial = new cTutorial();
    }

    public class cRequestTutorialTemp : cRequestBase
    {
        public string cmd = "req_tutorial_temp";
        public cTutorial tutorial = new cTutorial();
    }

    public class cResponseTutorialTemp : cResponseBase
    {
        public cTutorial tutorial = new cTutorial();
    }

    public class cRequestTutorialStart : cRequestBase
    {
        public string cmd = "req_tutorial_start";
        public cTutorial tutorial = new cTutorial();
    }

    public class cResponseTutorialStart : cResponseBase
    {
        public bool received;
        public List<cItem> items = new List<cItem>();
    }

    /// AppGuard Auth
    public class cRequestAppGuardAuth : cRequestBase
    {
        public string cmd = "req_appguard_auth";
        public string uniqueClientID = "";
        public bool client_auth_result = false;

    }

    public class cResponseAppGuardAuth : cResponseBase
    {
        public int status;
    }

    public class cRequestTalentLevelup : cRequestBase
    {
        public string cmd = "req_talent_levelup";
        public int talentid;

    }

    public class cResponseTalentLevelup : cResponseBase
    {
        public int gold;
        public int paidgold;
        public List<cItem> useitem = new List<cItem>();
        public cTalent talent = new cTalent();
    }

    public class cRequestTrainingMissionData : cRequestBase
    {
        public string cmd = "req_training_mission";
    }

    public class cResponseTrainingMissionData : cResponseBase
    {
        public cTrainingMissionInfo training = new cTrainingMissionInfo();
    }

    public class cRequestTrainingMissionReward : cRequestBase
    {
        public string cmd = "req_training_mission_reward";
        public long missionID;
    }

    public class cResponseTrainingMissionReward : cResponseBase
    {
        public cItemEtherReward reward = new cItemEtherReward();
        public List<cTrainingMission> missions = new List<cTrainingMission>();
        public long completeTime;
    }

    public class cRequestRefreshEvent : cRequestBase
    {
        public string cmd = "req_refresh_event";
    }

    public class cResponseRefreshEvent : cResponseBase
    {
        public List<cHotTime> hotTime = new List<cHotTime>();
    }

    public class cRequestUserRemove : cRequestBase
    {
        public string cmd = "req_user_remove";
        public long guid;
    }

    public class cResponseUserRemove : cResponseBase
    {

    }

    // 유저의 기본정보 (계정이름, 레벨) : 하이브 계정 충돌시 호출
    public class cRequestUserBaseInfo : cRequestBase
    {
        public string cmd = "req_user_base_info";
        public string id;
    }

    public class cResponseUserBaseInfo : cResponseBase
    {
        public string accountName;
        public int accountLevel;
    }

    public class cRequestChangeProfileUrl : cRequestBase
    {
        public string cmd = "req_change_profileurl";
        public string profileUrl;
    }

    public class cResponseChangeProfileUrl : cResponseBase
    {
        public string profileUrl;
    }

    public class cRequestLastPacket : cRequestBase
    {
        public string cmd = "req_last_packet";
        public long guid;
    }


    // 로비 덱 업데이트
    public class cRequestUserLobbyDeckUpdate : cRequestBase
    {
        public string cmd = "req_user_lobby_deck_update";
        public long academyCharacterDBKey;
        public List<long> characterDBKeyList = new List<long>();    // characterDBKey의 리스트형태[399,400,401,402] 순서대로 1,2,3,4
        public long lobbyBgDbKey;
    }

    public class cResponseUserLobbyDeckUpdate : cResponseBase
    {
        // public cUserData userData = new cUserData();
        public long lobbyBgDbKey;
        public cUserLobbyDeck userLobbyDeck = new cUserLobbyDeck();
        // public List<cLobbyBg>  lobbyBgDataList = new List<cLobbyBg>();
    }

    // 강제종료시 이전 상황을 보여줘야 하는 경우 접속시 1회 호출
    public class cRequestUserRestoreData : cRequestBase
    {
        public string cmd = "req_user_restore_data";
    }

    public class cResponseUserRestoreData : cResponseBase
    {
        public List<cGachaData> gachaList = new List<cGachaData>();
        public long gachaStoreId;
        public byte gachaStoreType; // 0 : none, 1 : store, 2 : item
        public cGachaStoreEnchant gachaStoreEnchant = new cGachaStoreEnchant();

        public int gachaPickupCount;
        public int gachaPickupReward;
        public List<cAttendanceRewardItem> gachaPickupItem = new List<cAttendanceRewardItem>();
        public cGachaMileage mileage = new cGachaMileage();

        public cGachaStoreElementEnchant gachaStoreElementEnchant = new cGachaStoreElementEnchant();

        public int gachaResetCount;
    }

    public class cRequestBuyLobbyBgData : cRequestBase
    {
        public string cmd = "req_buy_lobby_bg_data";
        public long id;
    }

    public class cResponseBuyLobbyBgData : cResponseBase
    {
        public cLobbyBg lobbyBgData = new cLobbyBg();
        public cUserMoney userMoney = new cUserMoney();
    }

    public class cRequestGameGuide : cRequestBase
    {
        public string cmd = "req_game_guide";
        public long guideId;
    }

    public class cResponseGameGuide : cResponseBase
    {
        public cUserMoney userMoney = new cUserMoney();
        public cGameGuide gameGuideData = new cGameGuide();
    }

    public class cRequestTopCharacterPowerLog : cRequestBase
    {
        public string cmd = "req_top_character_power_log";
        public List<cCharacterPower> characterList = new List<cCharacterPower>();
    }

    public class cResponseTopCharacterPowerLog : cResponseBase
    {
    }

    public class cRequestClientMsgLog : cRequestBase
    {
        public string cmd = "req_client_msg_log";
        public string msgLog;
    }

    public class cResponseClientMsgLog : cResponseBase
    {
    }

    public class cRequestGachaScheduleData : cRequestBase
    {
        public string cmd = "req_gacha_store_schedule";
    }

    public class cResponseGachaScheduleData : cResponseBase
    {
        public List<cGachaStoreScheduleData> gachaScheduleList = new List<cGachaStoreScheduleData>();
    }


    #endregion

    #region stage
    ///////////////////////////////////////////////////////////////////////////////
    //  url : address + '/stage'
    ///////////////////////////////////////////////////////////////////////////////

    public class cRequestStagePlayStart : cRequestBase
    {
        public string cmd = "req_stageplay_start";
        public int stageIndex;
        public cDeck deck = new cDeck();
        public bool battleRepeat = false;                // 로그용 - 반복전투:true 아니면 false
    }

    public class cResponseStagePlayStart : cResponseBase
    {
        public string playtoken;
        public int stageIndex;
        public int energy;
        public List<cItem> useitem = new List<cItem>();
        public long energy_recovertime;
        public string sugar;
        public long timeSugar;
    }

    // 테마 고정 보상 데이터 추가된 패킷
    public class cRequestStagePlayEnd : cRequestBase
    {
        public string cmd = "req_stageplay_end";
        public string playtoken;
        public int stageIndex;
        public int success;
        public List<int> star = new List<int>();    // [0,0,0] ~ [1,1,1]
        public cDeck deck = new cDeck();
        public int deathcount;                           // 죽은 아군 캐릭터수
        public List<long> monsters = new List<long>(); // 죽인 몬스터 아이디 리스트 ( 클리어 실패시에도 보낸다 )
        public int monstersPower;
        public string snack = "";
        public int playTurn;                           // 로그용 - 턴 수
        public bool battleRepeat = false;               // 로그용 - 반복전투:true 아니면 false
        public int autoPlay = 0;                       // 로그용 - 자동전투여부(종료 시점에 켜져있으면 1)
        public int knockDown = 0;                      // 로그용 - 녹다운 횟 수.
        public long damageOneHit = 0;                   // 로그용 - 1회 최고 피해량
        public long topCharacterID = 0;                 // 로그용 - 가장 높은 전투력 높은 캐릭터 ID
        public long topCharacterPower = 0;              // 로그용 - 가장 높은 전투력 높은 캐릭터 전투력
    }

    public class cResponseStagePlayEnd : cResponseBase
    {
        public int success;
        public bool themelaststage;
        public cStage stage = new cStage();
        public cThemeStarReward theme_star_reward = new cThemeStarReward();    //<- cThemeStarReward에 테마 고정 보상 변수 추가
        public cPortalMasterInfo pminfo = new cPortalMasterInfo();
        public cStageReward reward = new cStageReward();
        public cStageReward firstreward = new cStageReward();
        public cStageReward specialReward = new cStageReward();
        public cEntry entry = new cEntry();
        public cEntry entryHotTime = new cEntry();  // 핫타임 진행중일 때 핫타임 사용개수
        public int goldSum;                             // 골드 던전 - 모험 골드 누적.
    }

    public class cRequestCheckTowerInit : cRequestBase
    {
        public string cmd = "req_check_tower_init";
    }

    public class cResponseCheckTowerInit : cResponseBase
    {
        public long towerinittime;        // 다음 초기화 시간
        public short towerMonthNormal;    // 이번 달 최초 달성 층 - normal
        public short towerMonthHard;    // 이번 달 최초 달성 층 - hard
        public short towerFirstNormal;    // 계정 최초 달성 층 - normal
        public short towerFirstHard;    // 계정 최초 달성 층 - hard
        public List<cStage> towerStage = new List<cStage>();
        public List<cThemeStarReward> towerStarReward = new List<cThemeStarReward>();
        public List<cTowerMission> towerMissionList = new List<cTowerMission>();
    }

    // 던전 초기화 패킷
    public class cRequestResetEntry : cRequestBase
    {
        public string cmd = "req_reset_entry";
        public int themeid;
    }

    public class cResponseResetEntry : cResponseBase
    {
        public int gem; // 현재 남은 유져의 잼
        public int p_gem; // 현재 남은 유져의  유료 잼
        public byte count; // 초기화 된 입장 횟수
        public byte resetcount; //초기화 누적 횟수
    }

    // 신기루의 탑 패킷
    public class cRequestStageTowerPlayStart : cRequestBase
    {
        public string cmd = "req_tower_stageplay_start";
        public int stageIndex;
        public cDeck deck = new cDeck();
    }

    public class cResponseStageTowerPlayStart : cResponseBase
    {
        public string playtoken;
        public int stageIndex;
        public int energy;
        public long energy_recovertime;
        public List<cItem> useitem = new List<cItem>();
        public string sugar;
        public long timeSugar;
    }

    public class cRequestStageTowerPlayEnd : cRequestBase
    {
        public string cmd = "req_tower_stageplay_end";
        public string playtoken;
        public int stageIndex;
        public int success;
        public List<int> star = new List<int>();    // [0,0,0] ~ [1,1,1]
        public cDeck deck = new cDeck();
        public int deathcount;                           // 죽은 아군 캐릭터수
        public List<long> monsters = new List<long>(); // 죽인 몬스터 아이디 리스트 ( 클리어 실패시에도 보낸다 )
        public int monstersPower;
        public string snack = "";
        public int playTurn;                           // 로그용 - 턴 수
        public int autoPlay = 0;                       // 로그용 - 자동전투여부(종료 시점에 켜져있으면 1)
        public int knockDown = 0;                      // 로그용 - 녹다운 횟 수.
        public long damageOneHit = 0;                   // 로그용 - 1회 최고 피해량
        public long topCharacterID = 0;                 // 로그용 - 가장 높은 전투력 높은 캐릭터 ID
        public long topCharacterPower = 0;              // 로그용 - 가장 높은 전투력 높은 캐릭터 전투력
    }

    public class cResponseStageTowerPlayEnd : cResponseBase
    {
        public int success;
        public long towerinittime;        // 다음 초기화 시간
        public short towerMonthNormal;    // 이번 달 최초 달성 층 - normal
        public short towerMonthHard;    // 이번 달 최초 달성 층 - hard
        public short towerFirstNormal;    // 계정 최초 달성 층 - normal
        public short towerFirstHard;    // 계정 최초 달성 층 - hard
        public cThemeStarReward theme_star_reward = new cThemeStarReward();    //<- cThemeStarReward에 테마 고정 보상 변수 추가
        public cPortalMasterInfo pminfo = new cPortalMasterInfo();
        public cStageReward reward = new cStageReward();
        public cStageReward firstreward = new cStageReward();
        public cStageReward monthReward = new cStageReward();
        public cStage stage = new cStage();
    }

    // 속성 던전 패킷
    public class cRequestElementalStagePlayStart : cRequestBase
    {
        public string cmd = "req_elemental_stageplay_start";
        public int stageIndex;
        public cDeck deck = new cDeck();
        public bool battleRepeat = false;                // 로그용 - 반복전투:true 아니면 false
    }

    public class cResponseElementalStagePlayStart : cResponseBase
    {
        public string playtoken;
        public int stageIndex;
        public int energy;
        public long energy_recovertime;
        public byte elemental_rewarded_count;  // 속성던전 금일 2배보상 횟수
        public string sugar;
        public long timeSugar;
    }

    public class cRequestElementalStagePlayEnd : cRequestBase
    {
        public string cmd = "req_elemental_stageplay_end";
        public string playtoken;
        public int stageIndex;
        public int success;
        public List<int> star = new List<int>();    // [0,0,0] ~ [1,1,1]
        public cDeck deck = new cDeck();
        public int deathcount;                           // 죽은 아군 캐릭터수
        public List<long> monsters = new List<long>(); // 죽인 몬스터 아이디 리스트 ( 클리어 실패시에도 보낸다 )
        public int monstersPower;
        public string snack = "";
        public int playTurn;                           // 로그용 - 턴 수
        public bool battleRepeat = false;                // 로그용 - 반복전투:true 아니면 false
        public int autoPlay = 0;                       // 로그용 - 자동전투여부(종료 시점에 켜져있으면 1)
        public int knockDown = 0;                      // 로그용 - 녹다운 횟 수.
        public long damageOneHit = 0;                   // 로그용 - 1회 최고 피해량
        public long topCharacterID = 0;                 // 로그용 - 가장 높은 전투력 높은 캐릭터 ID
        public long topCharacterPower = 0;              // 로그용 - 가장 높은 전투력 높은 캐릭터 전투력
    }

    public class cResponseElementalStagePlayEnd : cResponseBase
    {
        public int success;
        public cStage stage = new cStage();
        public cPortalMasterInfo pminfo = new cPortalMasterInfo();
        public cStageReward reward = new cStageReward();
        public cStageReward extraReward = new cStageReward();        // firstreward 와 동일한 방식으로 동작함. null이면 추가보상 없음.
        public byte elemental_rewarded_count;  // 속성던전 금일 2배보상 횟수
    }

    public class cRequestEpisodeChallengeStagePlayStart : cRequestBase
    {
        public string cmd = "req_episode_challenge_stageplay_start";
        public int stageIndex;                                          // StageInfoID
        public int episodeStageID;                                      // SL_Table_EpisodeStageInfo 의 EpisodeStageInfoID
        public List<cChallengeCondition> conditions = new List<cChallengeCondition>();
        public cDeck deck = new cDeck();
        public bool battleRepeat = false;                // 로그용 - 반복전투:true 아니면 false
    }

    public class cResponseEpisodeChallengeStagePlayStart : cResponseBase
    {
        public string playtoken;
        public int stageIndex;
        public int energy;
        public List<cItem> useitem = new List<cItem>();
        public long energy_recovertime;
        public string sugar;
        public long timeSugar;
    }

    // 테마 고정 보상 데이터 추가된 패킷
    public class cRequestEpisodeChallengeStagePlayEnd : cRequestBase
    {
        public string cmd = "req_episode_challenge_stageplay_end";
        public string playtoken;
        public int stageIndex;
        public int episodeStageID;                                      // SL_Table_EpisodeStageInfo 의 EpisodeStageInfoID
        public int success;
        public List<int> star = new List<int>();    // [0,0,0] ~ [1,1,1]
        public cDeck deck = new cDeck();
        public int deathcount;                           // 죽은 아군 캐릭터수
        public List<long> monsters = new List<long>(); // 죽인 몬스터 아이디 리스트 ( 클리어 실패시에도 보낸다 )
        public int monstersPower;
        public string snack = "";
        public int playTurn;                           // 로그용 - 턴 수
        public bool battleRepeat = false;               // 로그용 - 반복전투:true 아니면 false
        public int autoPlay = 0;                       // 로그용 - 자동전투여부(종료 시점에 켜져있으면 1)
        public int knockDown = 0;                      // 로그용 - 녹다운 횟 수.
        public long damageOneHit = 0;                   // 로그용 - 1회 최고 피해량
        public long topCharacterID = 0;                 // 로그용 - 가장 높은 전투력 높은 캐릭터 ID
        public long topCharacterPower = 0;              // 로그용 - 가장 높은 전투력 높은 캐릭터 전투력
        public long missionValue = 0;                   // 미션용 - 중독 데미지 피해량
    }

    public class cResponseEpisodeChallengeStagePlayEnd : cResponseBase
    {
        public int success;
        public bool themelaststage;
        public cStage stage = new cStage();
        public cThemeStarReward theme_star_reward = new cThemeStarReward();    //<- cThemeStarReward에 테마 고정 보상 변수 추가
        public cPortalMasterInfo pminfo = new cPortalMasterInfo();
        public cStageReward reward = new cStageReward();
        public cStageReward firstreward = new cStageReward();
        public cStageReward specialReward = new cStageReward();
        public cEntry entry = new cEntry();
    }

    public class cRequestGoldStagePlayStart : cRequestBase
    {
        public string cmd = "req_gold_stageplay_start";
        public int stageIndex;
        public cDeck deck = new cDeck();
    }

    public class cResponseGoldStagePlayStart : cResponseBase
    {
        public string playtoken;
        public int stageIndex;
        public string sugar;
        public long timeSugar;
        public int goldSum;
    }

    // 테마 고정 보상 데이터 추가된 패킷
    public class cRequestGoldStagePlayEnd : cRequestBase
    {
        public string cmd = "req_gold_stageplay_end";
        public string playtoken;
        public int stageIndex;
        public int damage;
        public int bonusDamage;
        public cDeck deck = new cDeck();
        public int deathcount;                           // 죽은 아군 캐릭터수
        public List<long> monsters = new List<long>(); // 죽인 몬스터 아이디 리스트 ( 클리어 실패시에도 보낸다 )
        public int monstersPower;
        public string snack = "";
        public int playTurn;                           // 로그용 - 턴 수
        public int autoPlay = 0;                       // 로그용 - 자동전투여부(종료 시점에 켜져있으면 1)
        public int knockDown = 0;                      // 로그용 - 녹다운 횟 수.
        public long damageOneHit = 0;                   // 로그용 - 1회 최고 피해량
        public long topCharacterID = 0;                 // 로그용 - 가장 높은 전투력 높은 캐릭터 ID
        public long topCharacterPower = 0;              // 로그용 - 가장 높은 전투력 높은 캐릭터 전투력
    }

    public class cResponseGoldStagePlayEnd : cResponseBase
    {
        public int success;
        public cStage stage = new cStage();
        public cStageReward reward = new cStageReward();   // 보상 받은 후 상태 - 최종 유저 상태 용
        public int addGold;                                     // totalDamage 로 얻은 골드보상 량 - 결과 UI 용.
        public int goldSum;                                     // 최종 goldSum
    }

    //공략정보
    public class cRequestStageTacticInfo : cRequestBase
    {
        public string cmd = "req_stage_tactic_info";
        public byte battleContentsType;
    }

    public class cResponseStageTacticInfo : cResponseBase
    {
        public List<cStageTacticInfoList> stageTacticInfoLists = new List<cStageTacticInfoList>();
    }

    //공략정보
    public class cRequestEpisodeTacticInfo : cRequestBase
    {
        public string cmd = "req_episode_tactic_info";
        public long episodeID;
    }

    public class cResponseEpisodeTacticInfo : cResponseBase
    {
        public List<cStageTacticInfoList> stageTacticInfoLists = new List<cStageTacticInfoList>();
    }

    public class cRequestStageThemeStarReward : cRequestBase
    {
        public string cmd = "req_theme_star_reward";
        public int themeid;
        public byte order;        // 1 ~ 3(단계)
    }

    public class cResponseStageThemeStarReward : cResponseBase
    {
        public cItemEtherReward reward = new cItemEtherReward();
        public cThemeStarReward theme_star_reward = new cThemeStarReward();
    }

    public class cRequestSellContinuousBattleItemAndRune : cRequestBase
    {
        public string cmd = "req_sell_continuous_battle_item_and_rune";
        public List<cItem> items = new List<cItem>();
        public List<long> rune_dbKeys = new List<long>();
    }

    public class cResponseSellContinuousBattleItemAndRune : cResponseBase
    {
        public int gold;
        public List<cItem> update_item = new List<cItem>();
        public List<long> delete_item = new List<long>();
        public List<long> delete_rune = new List<long>();
    }

    // 수호대
    public class cRequestUpdateStageGuard : cRequestBase
    {
        public string cmd = "req_update_stage_guard";
        public List<cGuard> guardList = new List<cGuard>();
    }

    public class cResponseUpdateStageGuard : cResponseBase
    {
        public long guardStartTime;
        public List<cGuard> guardList = new List<cGuard>();
        public int guardRewardMaxTime;
        public int guardSpecialRewardTime;
        public int redPotion;
        public int bluePotion;
        public int gold;
    }

    // 수호대
    public class cRequestGetStageGuardReward : cRequestBase
    {
        public string cmd = "req_get_stage_guard_reward";
        public List<cGuard> guardList = new List<cGuard>();
    }

    public class cResponseGetStageGuardReward : cResponseBase
    {
        public long guardStartTime;
        public List<cGuard> guardList = new List<cGuard>();
        public int guardRewardMaxTime;
        public int guardSpecialRewardTime;
        public int redPotion;
        public int bluePotion;
        public int gold;
    }


    // 수호대
    public class cRequestRewardStageGuard : cRequestBase
    {
        public string cmd = "req_reward_stage_guard";
        public List<cGuard> guardList = new List<cGuard>();
    }

    public class cResponseRewardStageGuard : cResponseBase
    {
        public long guardStartTime;
        public List<cItem> ItemList = new List<cItem>();
        //public List<cEther> etherList = new List<cEther>();
        public cUserMoney userMoney = new cUserMoney();
        public int guardRewardMaxTime;
        public int guardSpecialRewardTime;
        public List<long> specialRewardId = new List<long>();
    }

    // 수호대
    public class cRequestGetStageGuard : cRequestBase
    {
        public string cmd = "req_get_stage_guard";
    }

    public class cResponseGetStageGuard : cResponseBase
    {
        public long guardStartTime;
        public List<cGuard> guardList = new List<cGuard>();
    }

    public class cRequestStageTowerStarReward : cRequestBase
    {
        public string cmd = "req_tower_star_reward";
        public int themeid;
        public byte order;        // 1 ~ 10(단계)
    }

    public class cResponseStageTowerStarReward : cResponseBase
    {
        public long gem;
        public long p_gem;
        public cThemeStarReward tower_star_reward = new cThemeStarReward();
    }

    // 수호대 토벌 - state
    public class cRequestStageGuardBattleState : cRequestBase
    {
        public string cmd = "req_stage_guard_battle_state";
    }
    public class cResponseStageGuardBattleState : cResponseBase
    {
        public cStageGuardBattle stageGuardBattleData = new cStageGuardBattle();
    }

    // 수호대 토벌 - start
    public class cRequestStageGuardBattleStart : cRequestBase
    {
        public string cmd = "req_stage_guard_battle_start";
        public int guardBattleID;       // 수호대 토벌 ID GuardBattleID
        public int battleCount;         // 유저 선택 토벌횟수
        public int battlePower;         // 전투력
        public List<long> characters = new List<long>();   // 캐릭터 DBKey List
    }
    public class cResponseStageGuardBattleStart : cResponseBase
    {
        public cStageGuardBattle stageGuardBattleData = new cStageGuardBattle();
        public cUserMoney userMoney = new cUserMoney();                             // 변동된 재화
    }
    /** req_arena_match_start 에러 코드
        18501000    (GUARD_BATTLE_INVALID_STATE_FOR_START)  : 토벌 시작 할 수 없는 상태임.
        18503000    (GUARD_BATTLE_NOT_ENOUGH_STAR)          : 별개수 부족함.
        18001000    (GUARD_BATTLE_NOT_ENOUGH_ENERGY)        : 에너지 부족함.
    **/

    // 수호대 토벌 - finish
    public class cRequestStageGuardBattleFinish : cRequestBase
    {
        public string cmd = "req_stage_guard_battle_finish";
        public int guardBattleID;       // 수호대 토벌 ID GuardBattleID
    }
    public class cResponseStageGuardBattleFinish : cResponseBase
    {
        public cStageGuardBattle stageGuardBattleData = new cStageGuardBattle();
        public cPortalMasterInfo pminfo = new cPortalMasterInfo();
        public cStageReward reward = new cStageReward();
        public int goldSum;                                         // 골드 던전 - 모험 골드 누적.
        public int useEnergy;                                       // 실제 사용한 에너지
        public int returnEnergy;                                    // 반환 에너지
        public int count;                                           // 실제 토벌 횟수
    }
    /** req_arena_match_start 에러 코드
        18502000    (GUARD_BATTLE_INVALID_STATE_FOR_REWARD) : 토벌 보상 받을 수 없는 상태임.
        18504000    (GUARD_BATTLE_NOT_ENOUGH_BATTLE_COUNT)  : 보상 받기엔 전투 횟수가 부족함.
    **/
    #endregion

    #region character
    ///////////////////////////////////////////////////////////////////////////////
    //  url : address + '/character'
    ///////////////////////////////////////////////////////////////////////////////

    public class cRequestSkillLevelUp : cRequestBase
    {
        public string cmd = "req_skill_levelup";
        public long characterDBKey;
        public long skillID;
    }

    public class cResponseSkillLevelUp : cResponseBase
    {
        public int currGold;
        public int currPaidGold;
        public long characterDBKey;
        public cSkill skill = new cSkill();
        public List<cItem> update_item = new List<cItem>();
        public List<long> delete_item = new List<long>();
    }

    public class cRequestEvolve : cRequestBase
    {
        public string cmd = "req_evolve";
        public long characterDBKey;
    }

    public class cResponseEvolve : cResponseBase
    {
        public int currGold;
        public int currPaidGold;
        public cCharacter character = new cCharacter();
        public List<cItem> items = new List<cItem>();
    }

    public class cRequestArouse : cRequestBase
    {
        public string cmd = "req_arouse";
        public long characterDBKey;
    }

    public class cResponseArouse : cResponseBase
    {
        public int currGold;
        public int currPaidGold;
        public cCharacter character = new cCharacter();
        public List<cItem> items = new List<cItem>();
    }

    //인연 정보!!
    public class cRequestCollection : cRequestBase
    {
        public string cmd = "req_collection";
        public long collectionID;        //인연 아이디
        public int collectionType;        //타입( 최초획득, 6성완료, 각성)  enum eCOLLECTION_TYPE 에 정의함.
    }

    public class cResponseCollection : cResponseBase
    {
        public List<cCollection> collections = new List<cCollection>();
        public cUserMoney reward = new cUserMoney();
    }

    /* public class cRequestPowerUp : cRequestBase
     {
         public string cmd = "req_power_up";
         public long characterDBKey;
     }

     public class cResponsePowerUp : cResponseBase
     {
         public int gold;
         public int paidGold;
         public long dbKey;
         public byte powerup;
         public List<cItem> update_item = new List<cItem>();
         public List<long> delete_item = new List<long>();
     }*/

    //성장보상
    public class cRequestGrowupReward : cRequestBase
    {
        public string cmd = "req_growup_reward";
        public long characterDBKey;    //캐릭터 dbkey
        public int rewardType;        //보상타입(레벨, 각성)  enum eGROWUP_REWARD_TYPE 에 정의함.
    }

    public class cResponseGrowupReward : cResponseBase
    {
        public cGrowupReward growupRewardData = new cGrowupReward();
        public cUserMoney reward = new cUserMoney();
    }

    //돌파
    public class cRequestCharacterLimitBreak : cRequestBase
    {
        public string cmd = "req_limitbreak";
        public long characterDBKey;
    }

    public class cResponseCharacterLimitBreak : cResponseBase
    {
        public List<cItem> items = new List<cItem>();
        public cCharacter character = new cCharacter();
        public long currGold;
        public long currPaidGold;
    }

    //초월
    public class cRequestCharacterTranscend : cRequestBase
    {
        public string cmd = "req_transcend";
        public long characterDBKey;
    }

    public class cResponseCharacterTranscend : cResponseBase
    {
        public List<cItem> items = new List<cItem>();
        public cCharacter character = new cCharacter();
        public long currGold;
        public long currPaidGold;
    }

    // 채팅 캐릭터 데이터
    public class cRequestChatCharacterData : cRequestBase
    {
        public string cmd = "req_chat_character_info";
        public long characterDBKey;
        public long targetGuid;
    }

    public class cResponseChatCharacterData : cResponseBase
    {
        public cCharacterBattleSet character = new cCharacterBattleSet();
    }

    #endregion

    #region cheat
    ///////////////////////////////////////////////////////////////////////////////
    //  url : address + '/cheat'
    ///////////////////////////////////////////////////////////////////////////////
    public class cRequestCheatCharacter : cRequestBase
    {
        public string cmd = "req_cheat_character";
    }

    public class cResponseCheatCharacter : cResponseBase
    {

    }

    public class cRequestCheatClearItem : cRequestBase
    {
        public string cmd = "req_cheat_clear_item";
    }

    public class cResponseCheatClearItem : cResponseBase
    {

    }

    public class cRequestCheatClearStage : cRequestBase
    {
        public string cmd = "req_cheat_clear_stage";
    }

    public class cResponseCheatClearStage : cResponseBase
    {

    }

    public class cRequestCheatCommand : cRequestBase
    {
        public string cmd = "req_cheat_command";
        public string command; // gold,
        public long value1;
        public long value2;
        public long value3;
        public long value4;
    }

    public class cResponseCheatCommand : cResponseBase
    {
        public cUserData userData = new cUserData();
        public List<cItem> items = new List<cItem>();
    }

    // 에테르 생성 10개
    public class cRequestCheatMakeEther : cRequestBase
    {
        public string cmd = "req_cheat_make_ether";
    }

    public class cResponseCheatMakeEther : cResponseBase
    {

    }

    public class cRequestCheatPortalMasterLevelUp : cRequestBase
    {
        public string cmd = "req_cheat_portalmaster_levelup";
        public int level;
    }

    public class cResponseCheatPortalMasterLevelUp : cResponseBase
    {

    }

    // 모든 에테르 삭제
    public class cRequestCheatClearEther : cRequestBase
    {
        public string cmd = "req_cheat_clear_ether";
    }

    public class cResponseCheatClearEther : cResponseBase
    {

    }


    //케릭터 소환 치트!! 소환버튼 눌렀을 때 소울스톤 아이디로 획득(소울스톤은 차감 안됨 )
    public class cRequestCheatGetCharacter : cRequestBase
    {
        public string cmd = "req_cheat_portalmaster_levelup";
        public long guid;
        public long soulstoneID;
    }

    public class cResponseCheatGetCharacter : cResponseBase
    {
        // 리부팅 시켜주세용
    }

    // 공지용
    public class cRequestCheatChatNotice : cRequestBase
    {
        public string cmd = "req_cheat_chat_notice";
        public string message;
    }

    public class cResponseCheatChatNotice : cResponseBase
    {

    }

    //특정 그룹의 유져에게 뿌리는 메세지
    public class cRequestCheatChatToGroup : cRequestBase
    {
        public string cmd = "req_cheat_chat_to_group";
        public int groupid;
        public string message;
    }

    public class cResponseCheatChatToGroup : cResponseBase
    {

    }

    //특정 유져에게 뿌리는 메세지
    public class cRequestCheatChatToUsers : cRequestBase
    {
        public string cmd = "req_cheat_chat_to_users";
        public List<long> userids = new List<long>();
        public string message;
    }

    public class cResponseCheatChatToUsers : cResponseBase
    {

    }

    public class cRequestCheatUpgradeEther : cRequestBase
    {
        public string cmd = "req_cheat_upgrade_ether";
        public long characterDBKey;
        public int upgradeValue;
    }

    public class cResponseCheatUpgradeEther : cResponseBase
    {
        public cCharacterBattleSet character = new cCharacterBattleSet();
    }

    public class cRequestCheatCharacterSkillLevel : cRequestBase
    {
        public string cmd = "req_cheat_character_skill_level";
        public long characterDBKey;
        public int level;
    }

    public class cResponseCheatCharacterSkillLevel : cResponseBase
    {
        public cCharacterBattleSet character = new cCharacterBattleSet();
    }

    public class cRequestCheatCharacterStatusAll : cRequestBase
    {
        public string cmd = "req_cheat_character_status_all_change";
        public long characterDBKey;
        public int evolved;
        public int arouse;
        public int limitbreak;
        public int transcend;
        public int characterLv;
        public int skillLv;
    }

    public class cResponseCheatCharacterStatusAll : cResponseBase
    {
        public cCharacterBattleSet character = new cCharacterBattleSet();
    }

    public class cRequestCheatCharacterArouse : cRequestBase
    {
        public string cmd = "req_cheat_character_arouse";
        public long characterDBKey;
        public int arouse;
    }

    public class cResponseCheatCharacterArouse : cResponseBase
    {
        public cCharacterBattleSet character = new cCharacterBattleSet();
    }

    public class cRequestCheatDialogReset : cRequestBase
    {
        public string cmd = "req_cheat_dialog_reset";
    }

    public class cResponseCheatDialogReset : cResponseBase
    {

    }

    public class cRequestCheatContentReset : cRequestBase
    {
        public string cmd = "req_cheat_content_reset";
    }

    public class cResponseCheatContentReset : cResponseBase
    {

    }

    public class cRequestCheatAcademyCharacterStatusAll : cRequestBase
    {
        public string cmd = "req_cheat_academy_character_status_all_change";
        public long dbKey;
        public int level;
        public int skillLevel;
    }

    public class cResponseCheatAcademyCharacterStatusAll : cResponseBase
    {
        public cAcademyCharacter academyCharacter = new cAcademyCharacter();
    }

    public class cRequestCheatAcademyCharacterSkillLevel : cRequestBase
    {
        public string cmd = "req_cheat_academy_character_skill_level";
        public long dbKey;
        public int skillLevel;
    }

    public class cResponseCheatAcademyCharacterSkillLevel : cResponseBase
    {
        public cAcademyCharacter academyCharacter = new cAcademyCharacter();
    }

    public class cRequestCheatAcademyCharacterLevel : cRequestBase
    {
        public string cmd = "req_cheat_academy_character_level";
        public long dbKey;
        public int level;
    }

    public class cResponseCheatAcademyCharacterLevel : cResponseBase
    {
        public cAcademyCharacter academyCharacter = new cAcademyCharacter();
    }
    #endregion

    #region GM
    ///////////////////////////////////////////////////////////////////////////////
    //  url : address + '/gm'
    ///////////////////////////////////////////////////////////////////////////////

    #endregion

    #region ITEM
    ///////////////////////////////////////////////////////////////////////////////
    //  url : address + '/item'
    ///////////////////////////////////////////////////////////////////////////////
    public class cRequestSellItem : cRequestBase
    {
        public string cmd = "req_sell_item";
        public long itemDBKey;
        public int count;
    }

    public class cResponseSellItem : cResponseBase
    {
        public int currGold;
        public int paidGold;
        public long itemID;
        public cItem item = new cItem();
    }

    public class cRequestSellSelectedItem : cRequestBase
    {
        public string cmd = "req_sell_selected_item";
        public List<long> dbKeys = new List<long>();
    }

    public class cResponseSellSelectedItem : cResponseBase
    {
        public int currGold;
        public int paidGold;
    }

    public class cRequestUseItemCharacterExp : cRequestBase
    {
        public string cmd = "req_use_item_character_exp";
        public long dbKey; // 해당 케릭터 dbKey
        public List<cItem> useitem = new List<cItem>();
    }

    public class cResponseUseItemCharacterExp : cResponseBase
    {
        // 변동된 재화
        public cUserMoney userMoney = new cUserMoney();

        //변경될 케릭터 정보
        public long dbKey;
        public int level;
        public int exp;

        //변경된 아이템 정보
        public List<cItem> useitem = new List<cItem>();
    }

    public class cRequestSupplyBox : cRequestBase
    {
        public string cmd = "req_supply_box";
        public long itemDBKey;
        public int count;
    }

    public class cResponseSupplyBox : cResponseBase
    {
        public cItem UseItem = new cItem();
        public cItemEtherReward reward = new cItemEtherReward();
    }

    public class cRequestSupportBox : cRequestBase
    {
        public string cmd = "req_support_box";
        public long itemDBKey;
    }

    public class cResponseSupportBox : cResponseBase
    {
        public cItem UseItem = new cItem();
        public cItemEtherReward reward = new cItemEtherReward();
    }

    public class cRequestLuckyBox : cRequestBase
    {
        public string cmd = "req_lucky_box";
        public long itemDBKey;
        public int count;
    }

    public class cResponseLuckyBox : cResponseBase
    {
        public cItem UseItem = new cItem();
        public cItemEtherReward reward = new cItemEtherReward();
        public List<cDisplayReward> displayReward = new List<cDisplayReward>();
    }

    public class cRequestUseItemCharacterTranscendExp : cRequestBase
    {
        public string cmd = "req_use_item_character_transcend_exp";
        public long dbKey; // 해당 캐릭터 dbKey
        public List<cItem> useitem = new List<cItem>();
    }

    public class cResponseUseItemCharacterTranscendExp : cResponseBase
    {
        public cCharacter character = new cCharacter();
        public List<cItem> useitem = new List<cItem>();
    }

    public class cRequestUseItemAcademyCharacterExp : cRequestBase
    {
        public string cmd = "req_use_item_academy_character_exp";
        public long dbKey; // 해당 캐릭터 dbKey
        public List<cItem> useitem = new List<cItem>();
    }

    public class cResponseUseItemAcademyCharacterExp : cResponseBase
    {
        // 변동된 재화
        public cUserMoney userMoney = new cUserMoney();

        //변경될 케릭터 정보
        public long dbKey;
        public int level;
        public int exp;

        //변경된 아이템 정보
        public List<cItem> useitem = new List<cItem>();
    }

    public class cRequestUseGachaScroll : cRequestBase
    {
        public string cmd = "req_use_gacha_scroll";
        public long dbKey;
        public int useCount;        // default 는 1 (에테르 스크롤은 여러개 소환이 가능)
    }
    public class cResponseUseGachaScroll : cResponseBase
    {
        public List<cItem> updateItem = new List<cItem>();          // 사용한 아이템 및 획득한 아이템
        public List<cGachaData> gachaList = new List<cGachaData>();     // 뽑기 데이터
        public cCharacter updateCharacter = new cCharacter();           // 뽑은 캐릭터
        public cItemEtherReward resultEther = new cItemEtherReward();     // 에테르 또는 아이템 가챠 결과
    }

    public class cRequestUseGachaEnchantScroll : cRequestBase
    {
        public string cmd = "req_use_gacha_enchant_scroll";
        public long etherDBKey;
        public long itemDBKey;
    }
    public class cResponseUseGachaEnchantScroll : cResponseBase
    {
        public List<cItem> updateItem = new List<cItem>();
        public cUserMoney userMoney = new cUserMoney();
        public cGachaStoreEnchant gachaStoreEnchant = new cGachaStoreEnchant();
    }

    public class cRequestComposeItem : cRequestBase
    {
        public string cmd = "req_compose_item";
        public long itemId; // 합성할 예정인 아이템의 id(만들어질 아이템)
        public int count; // 아이템을 몇개 만들 것인가(원하는 수량)
    }
    public class cResponseComposeItem : cResponseBase
    {
        public cUserMoney userMoney = new cUserMoney();
        public List<cItem> updateItem = new List<cItem>();
    }

    public class cRequestUseSkillLevelDownItem : cRequestBase
    {
        public string cmd = "req_use_skill_level_down";
        public long characterDBKey;
        public long skillID;
        public long itemDBKey;
    }

    public class cResponseUseSkillLevelDownItem : cResponseBase
    {
        public long characterDBKey;
        public cSkill skill = new cSkill();
        public cItem useItem = new cItem();
        public List<long> deleteItem = new List<long>();
        public cItemEtherReward reward = new cItemEtherReward();
    }

    public class cRequestUseSkillLevelAllDownItem : cRequestBase
    {
        public string cmd = "req_use_skill_level_all_down";
        public long characterDBKey;
        public long itemDBKey;
    }

    public class cResponseUseSkillLevelAllDownItem : cResponseBase
    {
        public long characterDBKey;
        public List<cSkill> skills = new List<cSkill>();
        public cItem useItem = new cItem();
        public List<long> deleteItem = new List<long>();
        public cItemEtherReward reward = new cItemEtherReward();
    }

    #endregion

    #region pvp
    ///////////////////////////////////////////////////////////////////////////////
    //  url : address + '/pvp'
    ///////////////////////////////////////////////////////////////////////////////
    // pvp battle info - 친선전, ...?
    public class cRequestBattleInfo : cRequestBase
    {
        public string cmd = "req_battle_info";
    }
    public class cResponseBattleInfo : cResponseBase
    {
        public cFriendlyArenaInfo friendlyInfo = new cFriendlyArenaInfo();
    }

    public class cRequestScheduleInfo : cRequestBase
    {
        public string cmd = "req_schedule_info";
    }
    public class cResponseScheduleInfo : cResponseBase
    {
        public cArenaStateData arenaState = new cArenaStateData();                        // 아레나 스케줄
        public cArenaSpecialStateData arenaSpecialState = new cArenaSpecialStateData();   // 아레나2 스케줄
    }

    // ARENA
    public class cRequestArenaInfo : cRequestBase
    {
        public string cmd = "req_arena_info";
        public cDeck defenceDeck = null;                                            // cDeck이 들어있으면 arena deck update / null이면 pass
    }
    public class cResponseArenaInfo : cResponseBase
    {
        public cArenaStateData arenaState = new cArenaStateData();                    // 아레나 스케줄
        public cArenaInfo arenaInfo = new cArenaInfo();                                // 아레나 유저 정보
        public cDeck defenceDeck = new cDeck();                                     // 방어덱
        public List<cArenaUser> matchList = new List<cArenaUser>();                 // 매칭 리스트
    }

    public class cRequestArenaUserInfo : cRequestBase
    {
        public string cmd = "req_arena_user_info";
        public long targetGuid;                                                        // 타깃정보
        public int type;                                                            // 타입 0:ANY, 1:매치리스트, 2:복수(uuid필요)
        public string uuid = "";                                                    // 복수 일 때 전투 uuid - 방어로그의 uuid
    }

    public class cResponseArenaUserInfo : cResponseBase
    {
        public cArenaUser targetArenaInfo = new cArenaUser();                                       // 아레나 유저 정보
        public cAcademyCharacter targetAcademyCharacter = new cAcademyCharacter();                  // 아카데미 캐릭터 정보
        public List<cCharacterBattleSet> targetCharacters = new List<cCharacterBattleSet>();        // 캐릭터들 정보 - cCharacter + 에테르정보
        public List<cAcademyCharacter> targetAcademyCharacters = new List<cAcademyCharacter>();     // 아카데미 캐릭터들 정보 - 유저의 전체 아카데미 캐릭터 리스트
        public List<cConnectionData> targetPVPConnections = new List<cConnectionData>();                // 유저의 PVP 인연 정보 (PVP 인연 정보만 준다.)     
    }
    /** req_arena_user_info 에러 코드
    9043         (NOT_EXIST_ARENA_TARGET)                : 타깃 유저가 없음 (탈퇴 or 초기화)
    **/

    // ARENA - 전적로그
    public class cRequestArenaBattleLogInfo : cRequestBase
    {
        public string cmd = "req_arena_battle_log_info";
    }
    public class cResponseArenaBattleLogInfo : cResponseBase
    {
        public List<cArenaBattleLog> listAttackLog = new List<cArenaBattleLog>();      // 공격 전적
        public List<cArenaBattleLog> listDefenceLog = new List<cArenaBattleLog>();     // 방어 전적
    }

    public class cRequestArenaMatchStart : cRequestBase
    {
        public string cmd = "req_arena_match_start";
        public long targetGuid;                                                         // 전투대상
        public cDeck deck = new cDeck();
        public string uuid = "";                                                        // 복수일때 uuid - 방어로그의 uuid
    }

    public class cResponseArenaMatchStart : cResponseBase
    {
        public cArenaUser targetArenaInfo = new cArenaUser();                           // cResponseArenaUserInfo와 동일
        public cAcademyCharacter targetAcademyCharacter = new cAcademyCharacter();      // cResponseArenaUserInfo와 동일
        public List<cCharacterBattleSet> targetCharacters = new List<cCharacterBattleSet>();// cResponseArenaUserInfo와 동일
        public List<cConnectionData> targetPVPConnections = new List<cConnectionData>();                // cResponseArenaUserInfo와 동일     
        public int playCount;                                                           // 아레나 플레이 횟수 (0~5) - 하루 두번 0으로 초기화 됨.
        public long timeMidDayNextReset;                                                // 아레나 플레이 횟수 다음 초기화 시간
        public long targetGuid;
        public string playToken;
        public string sugar;
        public long timeSugar;
    }
    /** req_arena_match_start 에러 코드
    9043        (NOT_EXIST_ARENA_TARGET)                : 타깃 유저가 없음 (탈퇴 or 초기화)
    30001000    (ARENA_REVENGE_RETRY_COUNT_OVER)        : 복수 횟수제한
    30002000    (ARENA_REVENGE_INVALID_STATE)           : 패배한 경기만 복수가능.
    30000001    (ARENA_NEED_INITIATE)                   : 초기화 필요. req_battle_info 호출 필요.
    30000002    (ARENA_ALREADY_VICTORY)                 : 이미 이긴유저(매치리스트 내에서)
    30000003    (ARENA_PLAY_COUNT_FULL)                 : 12시간에 5회까지만 플레이 가능.
    30000004    (ARENA_ALREADY_RECEIVED_WEEKLY_REWARD)  : 복수 횟수제한
    **/

    public class cRequestArenaMatchEnd : cRequestBase
    {
        public string cmd = "req_arena_match_end";
        public byte result;
        public long targetGuid;
        public string playToken;
        public string snack = "";
        public int playTurn;                           // 로그용 - 턴 수
        public string uuid = "";            // 복수일때 uuid - 방어로그의 uuid
        public int autoPlay = 0;                       // 로그용 - 자동전투여부(종료 시점에 켜져있으면 1)
        public int knockDown = 0;                      // 로그용 - 녹다운 횟 수.
        public long damageOneHit = 0;                   // 로그용 - 1회 최고 피해량
        public long topCharacterID = 0;                 // 로그용 - 가장 높은 전투력 높은 캐릭터 ID
        public long topCharacterPower = 0;              // 로그용 - 가장 높은 전투력 높은 캐릭터 전투력
    }

    public class cResponseArenaMatchEnd : cResponseBase
    {
        public long targetGuid;
        public byte result;                 //eARENA_STATE
        public int beforePoint;             // 계산 전 포인트
        public int addPoint;                // 포인트 변동량
        public int point;                   // 포인트
        public int tier;                    // 현재 랭크 (티어 테이블 ID)
        public int ranking;                 // 현재 등수
        public int playCount;               // 아레나 플레이 횟수 (0~5) - 하루 두번 0으로 초기화 됨.
        public long timeMidDayNextReset;    // 아레나 플레이 횟수 다음 초기화 시간
        public int arenaMedal;              // 보상 받은 현재 메달
    }
    /** req_arena_match_end 에러 코드
    9043         (NOT_EXIST_ARENA_TARGET)                : 타깃 유저가 없음 (탈퇴 or 초기화)
    30000001     (ARENA_NEED_INITIATE)                     : 초기화 필요. req_battle_info 호출 필요.
    **/

    public class cRequestArenaListRefresh : cRequestBase
    {
        public string cmd = "req_arena_list_refresh";
    }

    public class cResponseArenaListRefresh : cResponseBase
    {
        public List<cArenaUser> matchList = new List<cArenaUser>();                    // 매칭 리스트
        public long timeMatchListRefresh;                                            // 매칭리스트 초기화 시간
        public int gem;                                                                // 현재 젬
        public int p_gem;                                                             // 현재 유료 잼
    }

    public class cRequestArenaRanking : cRequestBase
    {
        public string cmd = "req_arena_ranking";
    }

    public class cResponseArenaRanking : cResponseBase
    {
        public List<cRank> topRank = new List<cRank>();            // 이번 시즌 탑 랭크(1~3)
        public List<cRank> tierRank = new List<cRank>();        // 이번 시즌 내 그룹 랭크
        public List<cRank> lastTopRank = new List<cRank>();        // 지난 시즌 탑 랭크(1~3)
        public List<cRank> lastTierRank = new List<cRank>();    // 지난 시즌 내 그룹 랭크
    }

    /// 주간 랭크 보상
    public class cRequestArenaWeeklyReward : cRequestBase
    {
        public string cmd = "req_arena_weekly_reward";
    }

    public class cResponseArenaWeeklyReward : cResponseBase
    {
        public cItemEtherReward tierReward = new cItemEtherReward();
        public string dateRewarded = null;                            // 보상받은 날짜(각서버기준 스트링)
    }
    /** req_arena_weekly_reward 에러 코드
    30000005     (ARENA_NOT_ATTEND_LAST_SEASON)                : 지난주 참가 안함.
    30000004     (ARENA_ALREADY_RECEIVED_WEEKLY_REWARD)         : 주간보상을 이미 받음.
    **/

    // ARENA_SPECIAL
    public class cRequestArenaSpecialInfo : cRequestBase
    {
        public string cmd = "req_arena_special_info";
        public List<cDeck> defenceDeckList = new List<cDeck>();     // cDeck이 들어있으면 arena deck update
    }
    public class cResponseArenaSpecialInfo : cResponseBase
    {
        public cArenaSpecialStateData arenaSpecialState = new cArenaSpecialStateData(); // 아레나 스케줄
        public cArenaSpecialInfo arenaSpecialInfo = new cArenaSpecialInfo();            // 아레나 유저 정보
        public List<cDeck> defenceDeckList = new List<cDeck>();                         // 방어덱 리스트
        public List<cArenaSpecialUser> matchList = new List<cArenaSpecialUser>();       // 매칭 리스트
        public List<cArenaSpecialTierUserCountInfo> tierUserCountList = new List<cArenaSpecialTierUserCountInfo>(); // 유저의 채널의 티어별 인원 수
        public List<cArenaSpecialFriend> friendArenaSpecialInfoList = new List<cArenaSpecialFriend>();      // 같은 채널에 있는 친구의 정보.
    }

    /** req_arena_special_info 에러 코드
        ** 덱관련..
        42001     (DECK_CHARACTER_EMPTY)                    : 캐릭터가 한 개 이상 필요.
        42002     (DECK_ACADEMY_CHARACTER_DBKEY_EMPTY)      : 아카데미 캐릭터가 비어있음.
        42003     (DECK_INVALID_DECK_TYPE)                  : 서버에 저장 할 수 없는 deckType
        42004     (DECK_DISABLE_ARENA_SPECIAL_DECK_TYPE)    : 아레나 방덱은 한개씩 세팅 불가.
        42005     (DECK_DECK_SET_NOT_ENOUGH_DECK)           : 아레나 방덱은 3개 한 번에 등록해야됨.
        42006     (DECK_DECK_SET_CHARACTER_ALREADY_USED)    : 아레나 방덱에 캐릭터는 중복안 됨.
        42007     (DECK_CHARACTER_MAIN_SLOT_EMPTY)          : 메인슬롯(1~4)은 빌 수 없음.
    **/

    public class cRequestArenaSpecialUserInfo : cRequestBase
    {
        public string cmd = "req_arena_special_user_info";
        public long targetGuid;                                                        // 타깃정보
        public int type;                                                            // 타입 0:ANY, 1:매치리스트
    }

    public class cResponseArenaSpecialUserInfo : cResponseBase
    {
        public List<byte> resultList = new List<byte>();                                                // 라운드별 승패 정보
        public cArenaSpecialUser targetArenaInfo = new cArenaSpecialUser();                             // 아레나 유저 정보
        public List<cArenaSpecialDeckInfo> targetDeckInfoList = new List<cArenaSpecialDeckInfo>();      // 아레나2 덱 정보(301, 302, 303)
        public List<cAcademyCharacter> targetAcademyCharacters = new List<cAcademyCharacter>();         // 아카데미 캐릭터들 정보 - 유저의 전체 아카데미 캐릭터 리스트
        public List<cConnectionData> targetPVPConnections = new List<cConnectionData>();                    // 유저의 PVP 인연 정보
    }
    /** req_arena_user_info 에러 코드
    9048         (NOT_EXIST_ARENA_SPECIAL_TARGET)                : 타깃 유저가 없음 (탈퇴 or 초기화)
    **/

    // ARENA - 전적로그
    public class cRequestArenaSpecialBattleLogInfo : cRequestBase
    {
        public string cmd = "req_arena_special_battle_log_info";
    }
    public class cResponseArenaSpecialBattleLogInfo : cResponseBase
    {
        public List<cArenaSpecialBattleLog> listAttackLog = new List<cArenaSpecialBattleLog>();      // 공격 전적
        public List<cArenaSpecialBattleLog> listDefenceLog = new List<cArenaSpecialBattleLog>();     // 방어 전적
    }

    public class cRequestArenaSpecialMatchStart : cRequestBase
    {
        public string cmd = "req_arena_special_match_start";
        public long targetGuid;                                                        // 전투대상
        public List<cDeck> deckList = new List<cDeck>();
    }

    public class cResponseArenaSpecialMatchStart : cResponseBase
    {
        public List<byte> resultList = new List<byte>();                                        // 라운드별 승패 정보
        public cArenaUser targetArenaInfo = new cArenaUser();                                   // cResponseArenaSpecialUserInfo 동일
        public cAcademyCharacter targetAcademyCharacter = new cAcademyCharacter();              // cResponseArenaSpecialUserInfo 동일
        public List<cCharacterBattleSet> targetCharacters = new List<cCharacterBattleSet>();    // cResponseArenaSpecialUserInfo 동일
        public List<cConnectionData> targetPVPConnections = new List<cConnectionData>();            // cResponseArenaSpecialUserInfo 동일  
        public int playCount;                                                                   // 아레나 플레이 횟수 (0~) - 참여보상여부
        public int enterCount;                                                                  // 입장권 수 (0~5) - 전투시작시 PvP2EnterUseValue 만큼 차감
        public long timeDayNextReset;                                                           // 아레나 플레이 횟수 다음 초기화 시간
        public long targetGuid;
        public string playToken;                                                                // countinue, end 호출 시 사용.
        public string sugar;
        public long timeSugar;
    }
    /** req_arena_match_start 에러 코드
    9048         (NOT_EXIST_ARENA_SPECIAL_TARGET)       : 타깃 유저가 없음 (탈퇴 or 초기화)
    30011000     (ARENA_SPECIAL_NEED_INITIATE)          : 초기화 필요. req_arena_special_info 호출 필요.
    30012000    (ARENA_SPECIAL_ALREADY_PLAYED_USER)     : 이미 이긴유저(매치리스트 내에서)
    30013000    (ARENA_SPECIAL_NOT_ENOUGH_ENTER_COUNT)  : 입장권부족
    30020000     (ARENA_SPECIAL_NOT_FOUND_IN_MATCHINGLIST)  : 매칭리스트에 없는 유저를 호출함.
    **/

    public class cRequestArenaSpecialMatchContinue : cRequestBase
    {
        public string cmd = "req_arena_special_match_continue";
        public byte round;                                  // 방금 끝난 round 1~3
        public byte result;                                 // 방금 끝난 round 결과
        public List<long> liveList = new List<long>();      // 라운드 생존 캐릭터 DBKey 리스트(유저)
        public List<long> liveListTarget = new List<long>();// 라운드 생존 캐릭터 DBKey 리스트(상대)
        public long targetGuid;
        public string playToken;
        public string snack = "";
        public int playTurn;                                // 로그용 - 턴 수
        public int autoPlay = 0;                            // 로그용 - 자동전투여부(종료 시점에 켜져있으면 1)
        public int knockDown = 0;                           // 로그용 - 녹다운 횟 수.
        public long damageOneHit = 0;                       // 로그용 - 1회 최고 피해량
        public long topCharacterID = 0;                     // 로그용 - 가장 높은 전투력 높은 캐릭터 ID
        public long topCharacterPower = 0;                  // 로그용 - 가장 높은 전투력 높은 캐릭터 전투력
    }

    public class cResponseArenaSpecialMatchContinue : cResponseBase
    {
        public long targetGuid;
        public List<byte> resultList = new List<byte>();    //라운드별 승패 정보
        public string playToken;                            // 다음 countinue, end 호출 시 사용.
        public string sugar;
        public long timeSugar;
    }
    /** req_arena_match_end 에러 코드
    9048         (NOT_EXIST_ARENA_SPECIAL_TARGET)               : 타깃 유저가 없음 (탈퇴 or 초기화)
    30011000     (ARENA_SPECIAL_NEED_INITIATE)                  : 초기화 필요. req_arena_special_info 호출 필요.
    30016000     (ARENA_SPECIAL_INVALID_ROUND)                  : 잘못된 round 정보
    30020000     (ARENA_SPECIAL_NOT_FOUND_IN_MATCHINGLIST)  : 매칭리스트에 없는 유저를 호출함.
    **/

    public class cRequestArenaSpecialMatchEnd : cRequestBase
    {
        public string cmd = "req_arena_special_match_end";
        public byte round;                                  // 방금 끝난 round 1~3(체크용.)
        public byte result;                                 // 방금 끝난 round 결과
        public List<long> liveList = new List<long>();      // 라운드 생존 캐릭터 DBKey 리스트(유저)
        public List<long> liveListTarget = new List<long>();// 라운드 생존 캐릭터 DBKey 리스트(상대)
        public long targetGuid;
        public string playToken;
        public string snack = "";
        public int playTurn;                                // 로그용 - 턴 수
        public int autoPlay = 0;                            // 로그용 - 자동전투여부(종료 시점에 켜져있으면 1)
        public int knockDown = 0;                           // 로그용 - 녹다운 횟 수.
        public long damageOneHit = 0;                       // 로그용 - 1회 최고 피해량
        public long topCharacterID = 0;                     // 로그용 - 가장 높은 전투력 높은 캐릭터 ID
        public long topCharacterPower = 0;                  // 로그용 - 가장 높은 전투력 높은 캐릭터 전투력
    }

    public class cResponseArenaSpecialMatchEnd : cResponseBase
    {
        public long targetGuid;
        public List<byte> resultList = new List<byte>();    //라운드별 승패 정보
        public int beforePoint;                             // 계산 전 포인트
        public int addPoint;                                // 포인트 변동량
        public int point;                                   // 포인트
        public int tier;                                    // 현재 랭크 (티어 테이블 ID)
        public int ranking;                                 // 현재 등수
        public int playCount;                               // 아레나 플레이 횟수 (0~) - 참여보상여부
        public int enterCount;                              // 입장권 수 (0~5) - 전투시작시 PvP2EnterUseValue 만큼 차감
        public long timeMidDayNextReset;                    // 아레나 플레이 횟수 다음 초기화 시간
        public int arenaMedal;                              // 보상 받은 현재 메달
    }
    /** req_arena_match_end 에러 코드
    9048         (NOT_EXIST_ARENA_SPECIAL_TARGET)           : 타깃 유저가 없음 (탈퇴 or 초기화)
    30011000     (ARENA_SPECIAL_NEED_INITIATE)              : 초기화 필요. req_arena_special_info 호출 필요.
    30016000     (ARENA_SPECIAL_INVALID_ROUND)              : 잘못된 round 정보
    30020000     (ARENA_SPECIAL_NOT_FOUND_IN_MATCHINGLIST)  : 매칭리스트에 없는 유저를 호출함.
    **/

    public class cRequestArenaSpecialListRefresh : cRequestBase
    {
        public string cmd = "req_arena_special_list_refresh";
    }

    public class cResponseArenaSpecialListRefresh : cResponseBase
    {
        public List<cArenaSpecialUser> matchList = new List<cArenaSpecialUser>();    // 매칭 리스트
        public long timeMatchListRefresh;                                            // 매칭리스트 초기화 시간
        public int gem;                                                              // 현재 젬
        public int p_gem;                                                            // 현재 유료 잼
    }

    public class cRequestArenaSpecialRanking : cRequestBase
    {
        public string cmd = "req_arena_special_ranking";
        public int channel = 1;                                 // 보고자 하는 채널 - 이번주
        public int lastChannel = 1;                             // 보고자 하는 채널 - 지난주
    }

    public class cResponseArenaSpecialRanking : cResponseBase
    {
        public List<cRank> topRank = new List<cRank>();         // 이번 시즌 탑 랭크(1~3)
        public List<cRank> tierRank = new List<cRank>();        // 이번 시즌 내 그룹 랭크
        public List<cRank> lastTopRank = new List<cRank>();     // 지난 시즌 탑 랭크(1~3)
        public List<cRank> lastTierRank = new List<cRank>();    // 지난 시즌 내 그룹 랭크
    }

    /** req_arena_weekly_reward 에러 코드
        30019000     (ARENA_SPECIAL_INVALID_CHANNEL)        : 잘못된 채널 요청.
        **/

    ///  티어 달성 보상
    public class cRequestArenaSpecialTierReward : cRequestBase
    {
        public string cmd = "req_arena_special_tier_reward";
        public byte tier;                                            // 보상 받을 달성 tier
    }

    public class cResponseArenaSpecialTierReward : cResponseBase
    {
        public cArenaSpecialInfo arenaSpecialInfo = new cArenaSpecialInfo();    // 아레나 유저 정보
        public cItemEtherReward tierReward = new cItemEtherReward();            // 보상 정보
    }
    /** req_arena_weekly_reward 에러 코드
    30015000     (ARENA_SPECIAL_TIER_REWARD_NOT_ENOUGH_TIER)        : 티어가 부족함.
    30014000     (ARENA_SPECIAL_TIER_REWARD_ALREADY_RECEIVED)       : 티어보상 이미 받음
    **/

    /// 입장권 구매
    public class cRequestArenaSpecialBuyEnterCount : cRequestBase
    {
        public string cmd = "req_arena_special_buy_enter_count";
    }

    public class cResponseArenaSpecialBuyEnterCount : cResponseBase
    {
        public cArenaSpecialInfo arenaSpecialInfo = new cArenaSpecialInfo();    // 아레나 유저 정보
        public int currGem;                                                     // 현재 유저 젬
        public int currPaidGem;                                                 // 현재 유저 젬(유료)
    }
    /** req_arena_weekly_reward 에러 코드
    30017000     (ARENA_SPECIAL_ENTER_COUNT_NOT_EMPTY)      : 입장권 0 개가 아니면 충전 불가.
    30018000     (ARENA_SPECIAL_ENTER_CHARGING_COUNT_FULL)  : 입장권 추가 충전 가능 횟수 초과
    **/

    /// 유저의 채널의 티어별 인원 수
    public class cRequestArenaSpecialNavigationInfo : cRequestBase
    {
        public string cmd = "req_arena_special_navigation_info";
    }

    public class cResponseArenaSpecialChannelTierCount : cResponseBase
    {
        public List<cArenaSpecialTierUserCountInfo> tierUserCountList = new List<cArenaSpecialTierUserCountInfo>();
        public List<cArenaSpecialFriend> friendArenaSpecialInfoList = new List<cArenaSpecialFriend>();
    }

    // 같은 채널에 있는 친구의 정보.
    public class cRequestArenaSpecialFriendInfo : cRequestBase
    {
        public string cmd = "req_arena_special_friend_info";
    }
    public class cResponseArenaSpecialFriendInfo : cResponseBase
    {
        public List<cArenaSpecialFriend> friendArenaSpecialInfoList = new List<cArenaSpecialFriend>();
    }



    public class cRequestChampionShipInfo : cRequestBase
    {
        public string cmd = "req_championship_info";
    }

    public class cResponseChampionShipInfo : cResponseBase
    {
        public cChampionShip champInfo = new cChampionShip();
        public int remainSeasonTime;
    }

    public class cRequestChampionShipRanking : cRequestBase
    {
        public string cmd = "req_championship_ranking";
    }

    public class cResponseChampionShipRanking : cResponseBase
    {
        public List<cRank> totalRanking = new List<cRank>();
    }

    public class cRequestChampionShipRankReward : cRequestBase
    {
        public string cmd = "req_championship_rank_reward";
    }

    public class cResponseChampionShipRankReward : cResponseBase
    {

    }

    public class cRequestChampionShipStart : cRequestBase
    {
        public string cmd = "req_championship_start";
    }

    public class cResponseChampionShipStart : cResponseBase
    {

    }
    #endregion

    #region deck
    ///////////////////////////////////////////////////////////////////////////////
    //  url : address + '/deck'
    ///////////////////////////////////////////////////////////////////////////////
    // 전체 덱 리스트 - 서버에 저장되는 것 전부
    public class cRequestDeckList : cRequestBase
    {
        public string cmd = "req_deck_list";
    }

    public class cResponseDeckList : cResponseBase
    {
        public List<cDeck> deckList = new List<cDeck>();
        public cGuildBattleStateData guildBattleState = new cGuildBattleStateData();  // 길드전 스케줄 정보
        public cGuildBattleMemberData guildBattleMember = new cGuildBattleMemberData(); // 길드전 멤버 정보 - 초기화 안 됐으면 null 로 줌(초기화는 only 길드초기화에서)
    }

    // [DEPRECATED] deckType 에 해당하는 deck 세팅 - cRequestSetDeckList를 써주세요.
    public class cRequestSetDeck : cRequestBase
    {
        public string cmd = "req_set_deck";
        public int deckType;                // 의미없음(cRequestSetDeckList 사용 권고)
        public cDeck deck;
    }

    public class cResponseSetDeck : cResponseBase
    {
        public cDeck deck = new cDeck();
    }
    /** req_set_deck 에러 코드
    42001     (DECK_CHARACTER_EMPTY)                    : 캐릭터가 한 개 이상 필요.
    42002     (DECK_ACADEMY_CHARACTER_DBKEY_EMPTY)      : 아카데미 캐릭터가 비어있음.
    42003     (DECK_INVALID_DECK_TYPE)                  : 서버에 저장 할 수 없는 deckType
    42004     (DECK_DISABLE_ARENA_SPECIAL_DECK_TYPE)    : 아레나 방덱은 한개씩 세팅 불가.
    42005     (DECK_DECK_SET_NOT_ENOUGH_DECK)           : 아레나 방덱은 3개 한 번에 등록해야됨.
    42006     (DECK_DECK_SET_CHARACTER_ALREADY_USED)    : 아레나 방덱에 캐릭터는 중복안 됨.
    42007     (DECK_CHARACTER_MAIN_SLOT_EMPTY)          : 메인슬롯(1~4)은 빌 수 없음.
    **/

    // [DEPRECATED] deckType 에 해당하는 deck 세팅 - cRequestSetDeckList를 써주세요.
    public class cRequestSetDeckList : cRequestBase
    {
        public string cmd = "req_set_deck_list";
        public List<cDeck> deckList = new List<cDeck>();
    }

    public class cResponseSetDeckList : cResponseBase
    {
        public List<cDeck> deckList = new List<cDeck>();
    }
    /** req_set_deck 에러 코드
    42001     (DECK_CHARACTER_EMPTY)                    : 캐릭터가 한 개 이상 필요.
    42002     (DECK_ACADEMY_CHARACTER_DBKEY_EMPTY)      : 아카데미 캐릭터가 비어있음.
    42003     (DECK_INVALID_DECK_TYPE)                  : 서버에 저장 할 수 없는 deckType
    42004     (DECK_DISABLE_ARENA_SPECIAL_DECK_TYPE)    : 아레나 방덱은 한개씩 세팅 불가.
    42005     (DECK_DECK_SET_NOT_ENOUGH_DECK)           : 아레나 방덱은 3개 한 번에 등록해야됨.
    42006     (DECK_DECK_SET_CHARACTER_ALREADY_USED)    : 아레나 방덱에 캐릭터는 중복안 됨.
    **/

    #endregion

    #region FRIEND
    ///////////////////////////////////////////////////////////////////////////////
    //  url : address + '/friend'
    ///////////////////////////////////////////////////////////////////////////////
    public class cRequestFriendFind : cRequestBase
    {
        public string cmd = "req_friend_find";
        public string portalmasterName;
        public short page;
    }

    public class cResponseFriendFind : cResponseBase
    {
        public List<cFriend> friend = new List<cFriend>();
    }

    public class cRequestFriendRequest : cRequestBase
    {
        public string cmd = "req_friend_request";
        public long friendid; //cFriend 에 있는 guid
    }

    public class cResponseFriendRequest : cResponseBase
    {
        public bool requestResult;
    }

    public class cRequestFriendRefuse : cRequestBase
    {
        public string cmd = "req_friend_refuse";
        public long friendid;
    }

    public class cResponseFriendRefuse : cResponseBase
    {
        public List<cFriend> requestlist = new List<cFriend>();
    }

    public class cRequestFriendAccept : cRequestBase
    {
        public string cmd = "req_friend_accept";
        public long friendid;
    }

    public class cResponseFriendAccept : cResponseBase
    {
        public List<cFriend> friends = new List<cFriend>();
        public List<cFriend> requestlist = new List<cFriend>();
        public bool isSuccess;
    }

    public class cRequestFriendDelete : cRequestBase
    {
        public string cmd = "req_friend_delete";
        public long friendid;
    }

    public class cResponseFriendDelete : cResponseBase
    {
        public byte delete_friend; // 지울수 있는 횟수
        public List<cFriend> friends = new List<cFriend>();
    }

    public class cRequestFriendList : cRequestBase
    {
        public string cmd = "req_friend_list";
    }

    public class cResponseFriendList : cResponseBase
    {
        public byte delete_friend;
        public List<cFriend> friends = new List<cFriend>();
        public List<cFriend> recommendfriends = new List<cFriend>();
        public List<cFriend> requestlist = new List<cFriend>();
        public long recommendresettime;
    }

    public class cRequestSendWishStone : cRequestBase
    {
        public string cmd = "req_send_wishstone";
        public List<cFriend> friends = new List<cFriend>();    // 위시스톤을 보낼 친구 리스트
    }

    public class cResponseSendWishStone : cResponseBase
    {
        public int wishstone;
        public List<cFriend> friends = new List<cFriend>();    //    보낸 다음 나의 친구 리스트 : 보냈다는 상태 갱신용
    }

    public class cRequestRecvWishStone : cRequestBase
    {
        public string cmd = "req_recv_wishstone";
        public List<cFriend> friends = new List<cFriend>(); // 위시스톤을 받을 친구 리스트
    }

    public class cResponseRecvWishStone : cResponseBase
    {
        public int wishstone;
        public List<cFriend> friends = new List<cFriend>();    //    받은 다음 나의 친구 리스트 : 받았다는 상태 갱신용
    }

    public class cRequestWishStone : cRequestBase
    {
        public String cmd = "req_wishstone";
    }
    public class cResponseWishStone : cResponseBase
    {
        public int getWishstone;
        public int sendWishstone;
    }

    //// 친구 요청 리스트 관리 /////
    public class cRequestReqFriendList : cRequestBase
    {
        public string cmd = "req_request_friend_list";
    }

    public class cResponseReqFriendList : cResponseBase
    {
        public List<cFriend> reqFriendList = new List<cFriend>();
    }

    public class cRequestCancelReqFriend : cRequestBase
    {
        public string cmd = "req_cancel_request_friend";
        public long reqGuid;
    }

    public class cResponseCancelReqFriend : cResponseBase
    {

    }

    public class cRequestBlockList : cRequestBase
    {
        public string cmd = "req_get_block_list";
    }

    public class cResponseBlockList : cResponseBase
    {
        public List<cFriend> blockList = new List<cFriend>();
        public int blockCount;
    }

    public class cRequestAddBlockUser : cRequestBase
    {
        public string cmd = "req_add_block_user";
        public long reqGuid;
    }

    public class cResponseAddBlockUser : cResponseBase
    {
        public byte delete_friend;
        public List<cFriend> friends = new List<cFriend>();
        public cFriend blockUser = new cFriend();
        public int blockCount;
        public List<cFriend> requestList = new List<cFriend>();
        public List<cFriend> requestFriends = new List<cFriend>();
    }

    public class cRequestDeleteBlockUser : cRequestBase
    {
        public string cmd = "req_delete_block_user";
        public long reqGuid;
    }

    public class cResponseDeleteBlockUser : cResponseBase
    {
        public List<cFriend> blockList = new List<cFriend>();
        public int blockCount;
    }

    public class cRequestResetRecommendFriend : cRequestBase
    {
        public string cmd = "req_reset_recommend_friend";
    }

    public class cResponseResetRecommendFriend : cResponseBase
    {
        public List<cFriend> recommendfriends = new List<cFriend>();
        public long recommendresettime;
    }

    public class cRequestDeleteAllMyRequest : cRequestBase
    {
        public string cmd = "req_delete_all_my_request_list"; // 내가 보낸 목록
    }

    public class cResponseDeleteAllMyRequest : cResponseBase
    {
    }

    public class cRequestCancelAllRequestFriend : cRequestBase
    {
        public string cmd = "req_cancel_all_request_friend"; // 내게 온 목록
    }

    public class cResponseCancelAllRequestFriend : cResponseBase
    {
    }


    #endregion

    #region STORE
    ///////////////////////////////////////////////////////////////////////////////
    //  url : address + '/store'
    ///////////////////////////////////////////////////////////////////////////////
    public class cRequestBuyStore : cRequestBase
    {
        public string cmd = "req_buy_store";
        public long storeId;
        public int count;
    }

    public class cResponseBuyStore : cResponseBase
    {
        public cItem useItem = new cItem();
        public cItemEtherReward reward = new cItemEtherReward();
        public cStore updateStoreInfo = new cStore();
    }

    public class cRequestBuyStoreAcademyCharacter : cRequestBase
    {
        public string cmd = "req_buy_store_academy_character";
        public long storeId;
    }

    public class cResponseBuyStoreAcademyCharacter : cResponseBase
    {
        public cUserMoney userMoney = new cUserMoney();
        public cAcademyCharacter academyCharacter = new cAcademyCharacter();
    }

    #endregion

    #region MAIL
    ///////////////////////////////////////////////////////////////////////////////
    //  url : address + '/mail'
    ///////////////////////////////////////////////////////////////////////////////

    public class cRequestMailList : cRequestBase
    {
        public string cmd = "req_list_mail";
    }
    public class cResponseMailList : cResponseBase
    {
        public List<cMail> mailList = new List<cMail>();
    }

    public class cRequestReceiveMail : cRequestBase
    {
        public string cmd = "req_receive_mail";
        public List<long> dbKeyList;
    }
    public class cResponseReceiveMail : cResponseBase
    {
        public cItemEtherReward reward = new cItemEtherReward();
        public List<cRecvMailReward> rewardList = new List<cRecvMailReward>();
        public List<cMail> mailList = new List<cMail>();
    }

    public class cRequestDeleteMail : cRequestBase
    {
        public string cmd = "req_delete_mail";
    }
    public class cResponseDeleteMail : cResponseBase
    {
        public List<cMail> mailList = new List<cMail>();
    }

    #endregion

    #region EVENT
    ///////////////////////////////////////////////////////////////////////////////
    //  url : address + '/event'
    ///////////////////////////////////////////////////////////////////////////////

    public class cRequestCheckAttendance : cRequestBase
    {
        public string cmd = "req_check_attendance";
    }
    public class cResponseCheckAttendance : cResponseBase
    {
        public cAttendanceResult attendanceResult = new cAttendanceResult();
        //public cAttendanceReturnResult attendanceReturnResult = new cAttendanceReturnResult();
    }

    public class cRequestEventMissionInfo : cRequestBase
    {
        public string cmd = "req_event_mission_info";
        public long groupID;
    }

    public class cResponseEventMissionInfo : cResponseBase
    {
        public List<cEventMission> missions = new List<cEventMission>();
    }

    public class cRequestEventMissionReward : cRequestBase
    {
        public string cmd = "req_event_mission_reward";
        public long groupID;
        public List<long> missionIds = new List<long>();
    }
    public class cResponseEventMissionReward : cResponseBase
    {
        public cItemEtherReward reward = new cItemEtherReward();
    }

    public class cRequestEventRouletteInfo : cRequestBase
    {
        public string cmd = "req_event_roulette_info";
        public long eventID;
    }
    public class cResponseEventRouletteInfo : cResponseBase
    {
        public long eventID;
        public int point;
        public int pointReward;
    }

    public class cRequestEventRouletteReward : cRequestBase
    {
        public string cmd = "req_event_roulette_reward";
        public long eventID;
    }
    public class cResponseEventRouletteReward : cResponseBase
    {
        public cItemEtherReward reward = new cItemEtherReward();
        public cItem item = new cItem();
        public int cursor;
        public int point;
        public int pointReward;
    }

    public class cRequestEventRoulettePointReward : cRequestBase
    {
        public string cmd = "req_event_roulette_point_reward";
        public long eventID;
    }
    public class cResponseEventRoulettePointReward : cResponseBase
    {
        public cItemEtherReward reward = new cItemEtherReward();
        public int point;
        public int pointReward;
    }

    public class cRequestEventShopInfo : cRequestBase
    {
        public string cmd = "req_event_shop_info";
        public long eventID;
    }
    public class cResponseEventShopInfo : cResponseBase
    {
        public List<cEventShop> list = new List<cEventShop>();
    }

    public class cRequestEventShopBuy : cRequestBase
    {
        public string cmd = "req_event_shop_buy";
        public long eventShopID;
        public int count;
    }
    public class cResponseEventShopBuy : cResponseBase
    {
        public cItemEtherReward reward = new cItemEtherReward();
        public int count;
        public cItem useItem = new cItem();
    }

    public class cRequestWebBannerInfo : cRequestBase
    {
        public string cmd = "req_web_banner_info";
    }
    public class cResponseWebBannerInfo : cResponseBase
    {
        public List<cWebEventInfo> list = new List<cWebEventInfo>();
    }

    public class cRequestEventManageInfo : cRequestBase
    {
        public string cmd = "req_event_manage_info";
    }
    public class cResponseEventManageInfo : cResponseBase
    {
        public List<cEventManageInfo> list = new List<cEventManageInfo>();
    }

    public class cRequestEventPromotionInfo : cRequestBase
    {
        public string cmd = "req_event_promotion_info";
        public long groupID;
    }

    public class cResponseEventPromotionInfo : cResponseBase
    {
        public List<cEventPromotion> list = new List<cEventPromotion>();
    }

    public class cRequestEventPromotionReward : cRequestBase
    {
        public string cmd = "req_event_promotion_reward";
        public long groupID;
        public long eventID;
    }

    public class cResponseEventPromotionReward : cResponseBase
    {
        public cItemEtherReward reward = new cItemEtherReward();
    }

    public class cRequestEventSumMissionInfo : cRequestBase
    {
        public string cmd = "req_event_sum_mission_info";
        public long groupID;
    }

    public class cResponseEventSumMissionInfo : cResponseBase
    {
        public int point;
        public List<cEventSumMission> list = new List<cEventSumMission>();
    }

    public class cRequestEventSumMissionReward : cRequestBase
    {
        public string cmd = "req_event_sum_mission_reward";
        public long groupID;
        public long eventID;
    }

    public class cResponseEventSumMissionReward : cResponseBase
    {
        public cItemEtherReward reward = new cItemEtherReward();
    }

    public class cRequestEventBingoInfo : cRequestBase
    {
        public string cmd = "req_event_bingo_info";
        public long eventID;
    }
    public class cResponseEventBingoInfo : cResponseBase
    {
        public List<int> numbers = new List<int>();
        public int randomPick;
        public int fixedPick;
        public int overlapCount;// 중복 픽 카운트
        public int line;
        public int lineReward;
    }

    public class cRequestEventBingoPick : cRequestBase
    {
        public string cmd = "req_event_bingo_pick";
        public long eventID;
        public int pickType;    // 1.random 2.fixed 3.gem
    }

    public class cResponseEventBingoPick : cResponseBase
    {
        public List<int> numbers = new List<int>();
        public int randomPick;  // 랜덤 누적 픽 카운트
        public int fixedPick;   // 확정 누적 픽 카운트
        public int overlapCount;// 중복 픽 카운트
        public int line;        // 완성 라인 수
        public int lineReward;  // 라인 보상 획득 수
        public cItemEtherReward reward = new cItemEtherReward();
        public cItem item = new cItem();
        public int pickNumber;  // 픽 요청에 나온 번호
        public cUserMoney beforeUserMoney = new cUserMoney(); // 보상받기 이전 상태 ( 잼차감 -> 잼보상 대응 )
    }

    public class cRequestEventBingoReward : cRequestBase
    {
        public string cmd = "req_event_bingo_reward";
        public long eventID;
    }

    public class cResponseEventBingoReward : cResponseBase
    {
        public List<int> numbers = new List<int>();
        public int randomPick;
        public int fixedPick;
        public int overlapCount;// 중복 픽 카운트
        public int line;
        public int lineReward;
        public cItemEtherReward reward = new cItemEtherReward();
    }

    #endregion

    #region COMMENT
    ///////////////////////////////////////////////////////////////////////////////
    //  url : address + '/comment'
    ///////////////////////////////////////////////////////////////////////////////

    public class cRequestGetCharacterCommentList : cRequestBase
    {
        public string cmd = "req_get_character_comment_list";
        public long characterId;
        public int page;
        public byte type; // 1 : 추천, 0 : 기본 출력
    }
    public class cResponseGetCharacterCommentList : cResponseBase
    {
        public List<cComment> commentList = new List<cComment>();
        public List<cComment> bestCommentList = new List<cComment>();
        public cComment myComment = new cComment();
        public cTotalEvaluation totalEvaluation = new cTotalEvaluation();
        public cEvaluation myEvaluation = new cEvaluation();
    }

    public class cRequestSetCharacterComment : cRequestBase
    {
        public string cmd = "req_set_character_comment";
        public long characterId;
        public string comment = "";
        public byte isCharacterInfo;
        public long dbKey;
    }
    public class cResponseSetCharacterComment : cResponseBase
    {
        public cComment comment = new cComment();
    }

    public class cRequestDelCharacterComment : cRequestBase
    {
        public string cmd = "req_del_character_comment_info";
        public string key;
        public long characterId;
    }
    public class cResponseDelCharacterComment : cResponseBase
    {
    }

    public class cRequestVoteCharacterComment : cRequestBase
    {
        public string cmd = "req_character_comment_vote";
        public string key;
        public long characterId;
    }
    public class cResponseVoteCharacterComment : cResponseBase
    {
        public byte result;
        public List<cComment> bestCommentList = new List<cComment>();
    }

    public class cRequestCharacterEvaluationUpdate : cRequestBase
    {
        public string cmd = "req_character_evaluation_update";
        public long characterId;
        public byte star;
        public List<int> contentList = new List<int>();
    }
    public class cResponseCharacterEvaluationUpdate : cResponseBase
    {
        public cTotalEvaluation totalEvaluation = new cTotalEvaluation();
        public cEvaluation myEvaluation = new cEvaluation();
    }

    #endregion

    #region GUILD
    ///////////////////////////////////////////////////////////////////////////////
    //  url : address + '/guild'
    ///////////////////////////////////////////////////////////////////////////////

    public class cRequestGuildCreate : cRequestBase
    {
        public string cmd = "req_guild_create";
        public string guildName;
        public int emblem_fg;
        public int emblem_bg;
        public byte auto_accept;
        public int require_level;
        public string comment;
    }

    public class cResponseGuildCreate : cResponseBase
    {
        public cGuild guild = new cGuild();
        public int currGold;
        public int currPaidGold;
    }

    public class cRequestGuildRecommend : cRequestBase
    {
        public string cmd = "req_guild_recommend";
    }

    public class cResponseGuildRecommend : cResponseBase
    {
        public long guildDBKey;
        public List<cGuild> guilds = new List<cGuild>();
    }

    public class cRequestGuildRequest : cRequestBase
    {
        public string cmd = "req_guild_request";
        public long dbKey;
    }

    public class cResponseGuildRequest : cResponseBase
    {
        public byte auto_accept;
        public List<cGuildRequest> guilds = new List<cGuildRequest>();
    }

    public class cRequestGuildRequestList : cRequestBase
    {
        public string cmd = "req_guild_request_list";
    }

    public class cResponseGuildRequestList : cResponseBase
    {
        public List<cGuildRequest> guilds = new List<cGuildRequest>();
    }

    public class cRequestGuildRequestCancel : cRequestBase
    {
        public string cmd = "req_guild_request_cancel";
        public long dbKey;
    }

    public class cResponseGuildRequestCancel : cResponseBase
    {
        public List<cGuildRequest> guilds = new List<cGuildRequest>();
    }

    public class cRequestGuildLeave : cRequestBase
    {
        public string cmd = "req_guild_leave";
        public long dbKey;
    }

    public class cResponseGuildLeave : cResponseBase
    {
    }

    public class cRequestGuildMemberList : cRequestBase
    {
        public string cmd = "req_guild_member_list";
        public long dbKey;
    }

    public class cResponseGuildMemberList : cResponseBase
    {
        public List<cGuildMember> members = new List<cGuildMember>();
    }

    public class cRequestGuildMasterRequestList : cRequestBase
    {
        public string cmd = "req_guild_master_request_list";
        public long dbKey;
    }

    public class cResponseGuildMasterRequestList : cResponseBase
    {
        public List<cGuildRequestMember> members = new List<cGuildRequestMember>();
    }

    public class cRequestGuildMasterRequestAccept : cRequestBase
    {
        public string cmd = "req_guild_master_request_accept";
        public long dbKey;
        public long requestid;
    }

    public class cResponseGuildMasterRequestAccept : cResponseBase
    {
        public List<cGuildRequestMember> members = new List<cGuildRequestMember>();
    }

    public class cRequestGuildMasterRequestReject : cRequestBase
    {
        public string cmd = "req_guild_master_request_reject";
        public long dbKey;
        public long requestid;
    }

    public class cResponseGuildMasterRequestReject : cResponseBase
    {
        public List<cGuildRequestMember> members = new List<cGuildRequestMember>();
    }

    public class cRequestGuildMasterMemberAssign : cRequestBase
    {
        public string cmd = "req_guild_master_member_assign";
        public long dbKey;
        public long memberid;
        public byte memberGrade;
    }

    public class cResponseGuildMasterMemberAssign : cResponseBase
    {
        public List<cGuildMember> members = new List<cGuildMember>();
    }

    public class cRequestGuildMasterMemberDelete : cRequestBase
    {
        public string cmd = "req_guild_master_member_delete";
        public long dbKey;
        public long memberid;
    }

    public class cResponseGuildMasterMemberDelete : cResponseBase
    {
        public List<cGuildMember> members = new List<cGuildMember>();
    }

    public class cRequestGuildInfo : cRequestBase
    {
        public string cmd = "req_guild_info";
        public long dbKey;
    }

    public class cResponseGuildInfo : cResponseBase
    {
        public cGuild guild = new cGuild();
        public long myGuildDBKey;
    }

    public class cRequestMyGuildInfo : cRequestBase
    {
        public string cmd = "req_myguild_info";
    }

    public class cResponseMyGuildInfo : cResponseBase
    {
        public cGuild guild = new cGuild();
        public cGuildMember member = new cGuildMember();
        public bool canGuildAttendence;
        public bool canGuildStampReward;
        public bool canDowngradeFlag;
    }

    public class cRequestGuildSearch : cRequestBase
    {
        public string cmd = "req_guild_search";
        public string guildName;
    }

    public class cResponseGuildSearch : cResponseBase
    {
        public long guildDBKey;
        public List<cGuild> guilds = new List<cGuild>();
    }

    public class cRequestGuildInviteSearch : cRequestBase
    {
        public string cmd = "req_guild_invite_search";
        public string userName;
    }

    public class cResponseGuildInviteSearch : cResponseBase
    {
        public cGuildRequestMember user = new cGuildRequestMember();
    }

    public class cRequestGuildInvite : cRequestBase
    {
        public string cmd = "req_guild_invite";
        public long requestid;
    }

    public class cResponseGuildInvite : cResponseBase
    {
        public List<cGuildRequestMember> users = new List<cGuildRequestMember>();
    }

    public class cRequestGuildInviteCancel : cRequestBase
    {
        public string cmd = "req_guild_invite_cancel";
        public long requestid;
    }

    public class cResponseGuildInviteCancel : cResponseBase
    {
        public List<cGuildRequestMember> users = new List<cGuildRequestMember>();
    }

    public class cRequestGuildMasterInviteList : cRequestBase
    {
        public string cmd = "req_guild_master_invite_list";
        public long dbKey;
    }

    public class cResponseGuildMasterInviteList : cResponseBase
    {
        public List<cGuildRequestMember> users = new List<cGuildRequestMember>();
    }

    public class cRequestGuildInviteList : cRequestBase
    {
        public string cmd = "req_guild_invite_list";
    }

    public class cResponseGuildInviteList : cResponseBase
    {
        public List<cGuildRequest> guilds = new List<cGuildRequest>();
    }

    public class cRequestGuildInviteAccept : cRequestBase
    {
        public string cmd = "req_guild_invite_accept";
        public long dbKey;
    }

    public class cResponseGuildInviteAccept : cResponseBase
    {
        public cGuild guild = new cGuild();
    }

    public class cRequestGuildInviteReject : cRequestBase
    {
        public string cmd = "req_guild_invite_reject";
        public long dbKey;
    }

    public class cResponseGuildInviteReject : cResponseBase
    {
        public List<cGuildRequest> guilds = new List<cGuildRequest>();
    }

    public class cRequestGuildMemberDeck : cRequestBase
    {
        public string cmd = "req_guild_member_deck";
        public long memberid;
    }

    public class cResponseGuildMemberDeck : cResponseBase
    {
        public List<cCharacterBattleSet> characters = new List<cCharacterBattleSet>();    // 캐릭터들 정보 - cCharacter + 에테르정보
        public cAcademyCharacter academyCharacter = new cAcademyCharacter();              // 아카데미 캐릭터 정보
    }

    public class cRequestGuildMessageUpdate : cRequestBase
    {
        public string cmd = "req_guild_message_update";
        public long dbKey;
        public string comment;
        public string notice;
    }

    public class cResponseGuildMessageUpdate : cResponseBase
    {
        public cGuild guild = new cGuild();
    }

    public class cRequestGuildSettingUpdate : cRequestBase
    {
        public string cmd = "req_guild_setting_update";
        public long dbKey;
        public int emblem_fg;
        public int emblem_bg;
        public byte auto_accept;
        public int require_level;
    }

    public class cResponseGuildSettingUpdate : cResponseBase
    {
        public cGuild guild = new cGuild();
    }

    public class cRequestGuildRewardAttendence : cRequestBase
    {
        public string cmd = "req_guild_reward_attendence";
        public long dbKey;
    }

    public class cResponseGuildRewardAttendence : cResponseBase
    {
        public cGuild guild = new cGuild();
        public cGuildMember member = new cGuildMember();
        public int attendID;
    }

    public class cRequestGuildMission : cRequestBase
    {
        public string cmd = "req_guild_mission";
    }

    public class cResponseGuildMission : cResponseBase
    {
        public List<cMission> missions = new List<cMission>();
        public short guildStamp;
        public short myStamp;
    }

    public class cRequestGuildRewardMission : cRequestBase
    {
        public string cmd = "req_guild_reward_mission";
        public List<long> missionIds = new List<long>();
    }

    public class cResponseGuildRewardMission : cResponseBase
    {
        public cItemEtherReward reward = new cItemEtherReward();
        public List<cMission> missions = new List<cMission>();
        public short guildStamp;
        public short myStamp;
    }

    public class cRequestGuildRewardStamp : cRequestBase
    {
        public string cmd = "req_guild_reward_stamp";
    }

    public class cResponseGuildRewardStamp : cResponseBase
    {

    }

    #endregion

    #region GUILDMATCH
    ///////////////////////////////////////////////////////////////////////////////
    //  url : address + '/guild_battle'
    ///////////////////////////////////////////////////////////////////////////////
    // 로비 정보
    public class cRequestGuildBattleGetInfo : cRequestBase
    {
        public string cmd = "req_guild_battle_get_info";
        public List<cDeck> defenceDeckList = new List<cDeck>();     // cDeck이 들어있으면 arena deck update
    }

    public class cResponseGuildBattleGetInfo : cResponseBase
    {
        // 스케줄
        public cGuildBattleStateData guildBattleState = new cGuildBattleStateData();
        // 내 길드
        //public cGuild guild                                         = new cGuild();
        public List<cGuildMember> members = new List<cGuildMember>();
        public cGuildBattleGuildData guildBattleGuild = new cGuildBattleGuildData();              // 길드전 정보
        public List<cGuildBattleMemberData> guildBattleMemberList = new List<cGuildBattleMemberData>();       // 길드전 멤버 정보
        public List<cGuildBattleBaseData> baseList = new List<cGuildBattleBaseData>();         // 길드전 거점 정보
        public List<cDeck> defenceDeckList = new List<cDeck>();                        // 방어덱 리스트
        //public List<cGuildBattleBaseDeckData> baseDeckListNext      = new List<cGuildBattleBaseDeckData>();     // 길드전 거점 덱 정보(다음주)
        public int validBaseDeckCount;                                                                          // 다음 차전 거점덱 세팅된 개수(0~30)
        // 상대 길드
        //public cGuild guildTarget                                   = new cGuild();                             // 상대 길드 정보
        public cGuildBattleGuildData guildBattleGuildTarget = new cGuildBattleGuildData();              // 상대 길드전 정보
        public List<cGuildBattleBaseData> baseTargetList = new List<cGuildBattleBaseData>();         // 상대 길드 거점 정보
        // 보스
        public cGuildBattleBossData guildBattleBoss = new cGuildBattleBossData();               // 보스 정보.
    }
    /** req_guild_battle_get_info 에러 코드
    errorCode.GUILD_BATTLE_GUILD_WAIT_INIT                          = 29107000;      // 길드전 - 길드전 초기화 진행중(다른 멤버가 초기화 중 몇초 후 재시도 필요)
    errorCode.GUILD_BATTLE_GUILD_TARGET_NEED_INIT_SEASON            = 29111000;      // 길드전 - 상대편 길드전 초기화 필요(시즌)
    errorCode.GUILD_BATTLE_GUILD_TARGET_NEED_INIT_TURN              = 29112000;      // 길드전 - 상대편 길드전 초기화 필요(회차)
    errorCode.GUILD_BATTLE_INVALID_STATE_LOBBY                      = 29117000;      // 길드전 - 로비 입장 불가능 한 상태
    **/

    // 길드전 멤버 덱 리스트 - 길드전 참가 신청용(길드장 전용)
    public class cRequestGuildBattleGetMemberDeckList : cRequestBase
    {
        public string cmd = "req_guild_battle_get_member_valid_deck_list";
        public long guildDBKey;
    }

    public class cResponseGuildBattleGetMemberDeckList : cResponseBase
    {
        public List<cGuildBattleBaseDeckData> baseDeckListNext = new List<cGuildBattleBaseDeckData>(); // 길드전 거점 덱 정보(다음주)
        public List<cShowDeck> showDeckList = new List<cShowDeck>();                // 거점 방덱 세팅 가능한 덱 리스트
        public int isRegistered;            // 현재 참가 신청 여부
        public string dateBaseDeckUpdated;  // 길드 거점 덱 마지막 저장시간 - req_guild_battle_set_register 에 넘길 때 필요.
        public int validBaseDeckCount;      // 다음 차전 거점덱 세팅된 개수(0~30)
    }

    // 길드전 참가 신청
    public class cRequestGuildBattleSetRegister : cRequestBase
    {
        public string cmd = "req_guild_battle_set_register";
        public long guildDBKey;
        public List<cGuildBattleBaseDeckInfo> baseInfoList = new List<cGuildBattleBaseDeckInfo>();
        public string dateBaseDeckUpdated;  // 길드 거점 덱 마지막 저장시간 - 저장 시도 시 다르면 29138000 에러.
        public byte isForced;               // dateBaseDeckUpdated 에 관계 없이 무조건 저장
    }

    public class cResponseGuildBattleSetRegister : cResponseBase
    {
        public List<cGuildBattleBaseDeckData> baseDeckListNext = new List<cGuildBattleBaseDeckData>(); // 길드전 거점 덱 정보(다음주)
        public int isRegistered;    // 참가 신청 여부  cGuildBattleGuild.isRegistered 에 이값을 덮어 쓸것.
        public string dateBaseDeckUpdated;  // 길드 거점 덱 마지막 저장시간
        public int validBaseDeckCount;      // 다음 차전 거점덱 세팅된 개수(0~30)
    }
    /**
    errorCode.GUILD_BATTLE_GUILD_REGISTER_INVALID_BASE_INFO_COUNT   = 29113000;      // 길드전 - 길드전 참가 신청 - 거점, 덱 세팅 오류 baseInfo 개수 10개 보다 부족
    errorCode.GUILD_BATTLE_GUILD_REGISTER_NOT_FOUND_BASE_INFO       = 29114000;      // 길드전 - 길드전 참가 신청 - 거점, 덱 세팅 오류 특정 거점 정보 누락
    errorCode.GUILD_BATTLE_GUILD_REGISTER_INVALID_BASE_INFO_MEMBER  = 29115000;      // 길드전 - 길드전 참가 신청 - 거점, 특정 거점의 sub 방덱 관련 유저정보가 비정상임(탈퇴)
    errorCode.GUILD_BATTLE_GUILD_REGISTER_INVALID_BASE_INFO_DECK    = 29116000;      // 길드전 - 길드전 참가 신청 - 거점, 특정 거점의 sub 방덱 관련 덱 정보가 비정상임(방덱 세팅안됨)
    errorCode.GUILD_BATTLE_INVALID_STATE_BATTLE_LOBBY               = 29118000;      // 길드전 - 전장 입장 불가능 한 상태
    errorCode.GUILD_BATTLE_GUILD_REGISTER_ALREADY_BASE_INFO_UPDATED = 29138000;      // 길드전 - 길드전 참가 신청 - 길마, 부길마 동시에 덱 편집 시 덱 저장 전 이미 바꼈을 때 강제저장 유도.
    errorCode.GUILD_BATTLE_GUILD_REGISTER_DUPLICATE_DECK_TYPE       = 29139000;      // 길드전 - 길드전 참가 신청 - 멤버의 덱이 같은게 다른거점 방덱에 사용됨.
    */

    // 길드전 공지 세팅
    public class cRequestGuildBattleSetNotice : cRequestBase
    {
        public string cmd = "req_guild_battle_set_notice";
        public long guildDBKey;
        public string notice;               // 길드전 공지(80byte)
    }

    public class cResponseGuildBattleSetNotice : cResponseBase
    {
        public cGuildBattleGuildData guildBattleGuild = new cGuildBattleGuildData();          // 길드전 정보
    }

    /** 길드전 X차전 보상
    * 조건 : 초기화 완료(req_guild_battle_get_info)
    *       해당 시즌 내에만 받을 수 있음.
    *       cGuildBattleMember.dateTurnRewarded == null && cGuildBattleMember.lastTurnWing > 0   - 보상 안 받았고 지난회차 플레이 했음.
    **/
    public class cRequestGuildBattleReceiveTurnReward : cRequestBase
    {
        public string cmd = "req_guild_battle_receive_turn_reward";
        public long guildDBKey;
    }

    public class cResponseGuildBattleReceiveTurnReward : cResponseBase
    {
        public cItemEtherReward turnReward = new cItemEtherReward();
        public string dateTurnRewarded = null;                     // 보상받은 날짜(각서버기준 스트링)
    }
    /**
    errorCode.GUILD_BATTLE_INVALID_STATE_LOBBY                      = 29117000;      // 길드전 - 로비 입장 불가능 한 상태
    errorCode.GUILD_BATTLE_REWARD_TURN_GUILD_NOT_ATTEND             = 29134000;      // 길드전 - X차전 보상 - 길드전에 참가 안함.
    errorCode.GUILD_BATTLE_REWARD_TURN_MEMBER_NOT_ATTEND            = 29135000;      // 길드전 - X차전 보상 - 길드전에 참가 안함.
    errorCode.GUILD_BATTLE_REWARD_TURN_ALREADY_RECEIVED             = 29136000;      // 길드전 - X차전 보상- 이미 보상 받음.
    */

    /** 길드전 시즌 보상
    * 조건 : 초기화 완료(req_guild_battle_get_info)
    *       정산 후 ~ 다음시즌 정산 전까지
    *       cGuildBattleMember.dateSeasonRewarded == null && cGuildBattleMember.lastSeasonWing > 0   - 보상 안 받았고 지난시즌 플레이 했음.
    *       cGuildBattleGuildData.lastReward > 0                                                     - 지난 시즌 길드가 길드전 참여 했음.
    **/
    public class cRequestGuildBattleReceiveSeasonReward : cRequestBase
    {
        public string cmd = "req_guild_battle_receive_season_reward";
        public long guildDBKey;
    }

    public class cResponseGuildBattleReceiveSeasonReward : cResponseBase
    {
        public cItemEtherReward seasonReward = new cItemEtherReward();
        public string dateSeasonRewarded = null;                     // 보상받은 날짜(각서버기준 스트링)
    }
    /**
    errorCode.GUILD_BATTLE_INVALID_STATE_LOBBY                      = 29117000;      // 길드전 - 로비 입장 불가능 한 상태
    errorCode.GUILD_BATTLE_REWARD_SEASON_GUILD_NOT_ATTEND           = 29131000;      // 길드전 - 시즌보상 - 길드전에 참가 안함.
    errorCode.GUILD_BATTLE_REWARD_SEASON_MEMBER_NOT_ATTEND          = 29132000;      // 길드전 - 시즌보상 - 길드전에 참가 안함.
    errorCode.GUILD_BATTLE_REWARD_SEASON_ALREADY_RECEIVED           = 29133000;      // 길드전 - 시즌보상 - 이미 보상 받음.
    */

    // 길드전 거점 상세 정보 - BaseData(내구도, 버프레벨), BaseDeckData(덱), 전적
    public class cRequestGuildBattleGetBaseDetailInfo : cRequestBase
    {
        public string cmd = "req_guild_battle_get_base_detail_info";
        public long guildDBKey;     // 내길드ID
        public int campType;        // 1: 내길드정보 /  2: 상대길드정보 /  3: 보스전
        public int baseID;          // 거점ID
    }

    public class cResponseGuildBattleGetBaseDetailInfo : cResponseBase
    {
        public cGuildBattleBaseData baseInfo = new cGuildBattleBaseData();       // 거점 정보
        public cGuildBattleBaseDeckData baseDeckInfo = new cGuildBattleBaseDeckData();   // 거점 덱 정보
        public List<cGuildBattleMemberBaseLog> baseDefLog = new List<cGuildBattleMemberBaseLog>();      // 거점 기준 방어 전적 - 거점 소유 길드가 방어자가 됨을 주의
    }
    /**
    errorCode.GUILD_BATTLE_GUILD_REGISTER_NO_SET_BASE_INFO_YET      = 29143000;      // 길드전 - 길마가 한번도 거점 덱 세팅 전이라 데이터가 없음.
    **/

    // 길드전 거점 공격중 정보 - 거점 방어력 갱신, 길드별 공격중 상태 리스트
    public class cRequestGuildBattleGetBaseAttackingInfo : cRequestBase
    {
        public string cmd = "req_guild_battle_get_base_attacking_info";
        public long guildDBKey;     // 내길드ID
    }

    public class cResponseGuildBattleGetBaseAttackingInfo : cResponseBase
    {
        // 아군 길드
        public List<cGuildBattleBaseData> baseList = new List<cGuildBattleBaseData>();             // 길드전 거점 정보
        public List<cGuildBattleMemberAttackingInfo> attackingList = new List<cGuildBattleMemberAttackingInfo>();  // 아군 길드 공격중 리스트
        // 상대 길드
        public List<cGuildBattleBaseData> baseTargetList = new List<cGuildBattleBaseData>();             // 상대 길드 거점 정보
        public List<cGuildBattleMemberAttackingInfo> attackingTargetList = new List<cGuildBattleMemberAttackingInfo>();  // 상대 길드 공격중 리스트
        // 보스
        public cGuildBattleBossData guildBattleBoss = new cGuildBattleBossData();                   // 보스 정보.
    }

    // 길드전 거점 목표 설정
    public class cRequestGuildBattleSetBaseTarget : cRequestBase
    {
        public string cmd = "req_guild_battle_set_base_target";
        public long guildDBKey;     // 내길드ID
        public int baseID;          // 상대 거점ID
        public int isTarget;        // 목표 설정(0:목표아님 / 1:목표)
    }

    public class cResponseGuildBattleSetBaseTarget : cResponseBase
    {
        public cGuildBattleBaseData baseInfo = new cGuildBattleBaseData();     // 거점 정보
    }

    // 길드전 거점 목표 설정
    public class cRequestGuildBattleSetBossTarget : cRequestBase
    {
        public string cmd = "req_guild_battle_set_boss_target";
        public long guildDBKey;     // 내길드ID
        public int isTarget;        // 목표 설정(0:목표아님 / 1:목표)
    }

    public class cResponseGuildBattleSetBossTarget : cResponseBase
    {
        public cGuildBattleBossData guildBattleBoss = new cGuildBattleBossData();     // 보스 정보
    }

    // 길드전 길드원 정보 - 날개, 기여도
    public class cRequestGuildBattleGetBattleMemberInfo : cRequestBase
    {
        public string cmd = "req_guild_battle_get_battle_member_info";
        public long guildDBKey;
    }

    public class cResponseGuildBattleGetBattleMemberInfo : cResponseBase
    {
        public List<cGuildMember> members = new List<cGuildMember>();
        public List<cGuildBattleMemberData> guildBattleMemberList = new List<cGuildBattleMemberData>();   // 길드전 멤버 정보
    }

    // 길드전 길드원 1명 전적 정보
    public class cRequestGuildBattleGetBattleMemberLog : cRequestBase
    {
        public string cmd = "req_guild_battle_get_battle_member_log";
        public long guildDBKey;
        public long memberGuid;
    }

    public class cResponseGuildBattleGetBattleMemberLog : cResponseBase
    {
        public List<cGuildBattleMemberBaseLog> memberAtkLog = new List<cGuildBattleMemberBaseLog>();    // 멤버 기준 공격 전적
        public List<cGuildBattleMemberBaseLog> memberDefLog = new List<cGuildBattleMemberBaseLog>();    // 멤버 기준 방어 전적
        public List<cGuildBattleMemberBossLog> memberBossLog = new List<cGuildBattleMemberBossLog>();    // 멤버 기준 보스전 전적
    }

    // 길드전 보스 전적 - 보스 정산 로그 리스트
    public class cRequestGuildBattleGetBossResultList : cRequestBase
    {
        public string cmd = "req_guild_battle_get_boss_result_list";
        public long guildDBKey;
    }

    public class cResponseGuildBattleGetBossResultList : cResponseBase
    {
        public long targetGuildDBKey;       // 현재회차 상대 길드
        public string targetGuildName;      // 현재 X차전 상대 길드 - 길드명
        public int targetEmblemFg;          // 현재 X차전 상대 길드 - 길드 엠블렘 BG
        public int targetEmblemBg;          // 현재 X차전 상대 길드 - 길드 임블렘 FG
        public cGuildBattleBossData guildBattleBoss = new cGuildBattleBossData();           // 현재 진행중 보스 정보
        public List<cGuildBattleGuildBossLog> guildBossResultLogList = new List<cGuildBattleGuildBossLog>();
    }

    // 길드전 보스 전적 - 보스 정산 로그 상세(+멤버별 누적 데미지 리스트)
    public class cRequestGuildBattleGetBossResultDetail : cRequestBase
    {
        public string cmd = "req_guild_battle_get_boss_result_detail";
        public long guildDBKey;
        public int bossOrder;
    }

    public class cResponseGuildBattleGetBossResultDetail : cResponseBase
    {
        public long targetGuildDBKey;       // 현재회차 상대 길드
        public string targetGuildName;      // 현재 X차전 상대 길드 - 길드명
        public int targetEmblemFg;          // 현재 X차전 상대 길드 - 길드 엠블렘 BG
        public int targetEmblemBg;          // 현재 X차전 상대 길드 - 길드 임블렘 FG
        public cGuildBattleBossData guildBattleBoss = new cGuildBattleBossData();   // 현재 진행중 보스 정보
        public cGuildBattleGuildBossLog guildBossResultLog = null;                 // 현재 진행중 bossOrder 요청 시 null.
        public List<cGuildBattleGuildBossMemberLog> guildBossMemberLogList = new List<cGuildBattleGuildBossMemberLog>();
    }
    /**
    errorCode.GUILD_BATTLE_LOG_BOSS_NOT_FOUND_BOSS_ORDER            = 29137000;      // 길드전 - 보스전적 - bossOrder 에 해당 하는 로그가 없음(아직 진행전)
    **/

    // 길드전 보스 전적 - 보스 정산 로그 상세(+멤버별 누적 데미지 리스트)
    public class cRequestGuildBattleGetLastBossResult : cRequestBase
    {
        public string cmd = "req_guild_battle_get_last_boss_result";
        public long guildDBKey;
    }

    public class cResponseGuildBattleGetLastBossResult : cResponseBase
    {
        public cGuildBattleGuildBossLog guildBossResultLog = null;                         // 현재 진행중 bossOrder 요청 시 null.
        public cGuildBattleMemberData guildBattleMember = new cGuildBattleMemberData(); // lastBossOrderResult 업데이트 용.
    }

    // 길드전 랭킹
    public class cRequestGuildBattleGetRanking : cRequestBase
    {
        public string cmd = "req_guild_battle_get_ranking";
        public long guildDBKey;
    }

    public class cResponseGuildBattleGetRanking : cResponseBase
    {
        public List<cGuildRank> topRank = new List<cGuildRank>();   // 이번 시즌 탑 랭크(1~3)
        public List<cGuildRank> tierRank = new List<cGuildRank>();   // 이번 시즌 내 그룹 랭크
        public List<cGuildRank> lastTopRank = new List<cGuildRank>();   // 지난 시즌 탑 랭크(1~3)
        public List<cGuildRank> lastTierRank = new List<cGuildRank>();   // 지난 시즌 내 그룹 랭크
    }
    /**
    errorCode.GUILD_BATTLE_INVALID_STATE_LOBBY                      = 29117000;      // 길드전 - 로비 입장 불가능 한 상태
    */

    // 길드전 길드 기준 차전 전적 리스트
    public class cRequestGuildBattleGetTurnResult : cRequestBase
    {
        public string cmd = "req_guild_battle_get_turn_result";
        public long guildDBKey;
    }

    public class cResponseGuildBattleGetTurnResult : cResponseBase
    {
        public List<cGuildBattleTurnResult> turnResultList = new List<cGuildBattleTurnResult>();   // 차전 전적 리스트 - 미참 시 없음.
    }
    /**
    errorCode.GUILD_BATTLE_INVALID_STATE_LOBBY                      = 29117000;      // 길드전 - 로비 입장 불가능 한 상태
    */

    // 거점 전투 시작
    public class cRequestGuildBattleMatchStart : cRequestBase
    {
        public string cmd = "req_guild_battle_match_start";
        public long guildDBKey;             // 자신의 길드 DBKey
        public long targetGuildDBKey;       // 상대의 길드 DBKey
        public int targetBaseID;            // 거점ID baseID
        public int targetBaseIndex;         // 거점내 덱 순서 baseIndex
        public cDeck deck = new cDeck();
    }

    public class cResponseGuildBattleMatchStart : cResponseBase
    {
        public int wing;                                                                        // 날개 개수
        public long targetGuid;
        public long targetGuildDBKey;
        public int targetBaseID;                                                                // 거점ID baseID
        public int targetBaseIndex;                                                             // 거점내 덱 순서 baseIndex
        public cGuildBattleGuildInfo targetGuildBattleInfo = new cGuildBattleGuildInfo();       // 길드전 정보
        public cBattleDeckSnap targetDeckSnap = new cBattleDeckSnap();                          // 덱 스냅샷 정보(아카데미 캐릭터 정보, 캐릭터들 정보 - cCharacter + 에테르정보, 아카데미 캐릭터들 정보, 유저정보)
        public cGuildBattleBaseData targetBaseInfo = new cGuildBattleBaseData();                // 거점 정보 - 전투시작 시점의 보스 버프 정보를 전투에 반영위함.
        public string playToken;                                                                // end 호출 시 사용.
        public string sugar;
        public long timeSugar;
    }
    /**
    errorCode.GUILD_BATTLE_INVALID_STATE_BATTLE                     = 29119000;      // 길드전 - 전투 불가능 한 상태
    errorCode.GUILD_BATTLE_MATCH_INVALID_TARGET                     = 29123000;      // 길드전 - 전투 - 잘못된 상대 길드와 전투 시도
    errorCode.GUILD_BATTLE_MATCH_INVALID_TARGET_BASE                = 29124000;      // 길드전 - 전투 - 잘못된 상대 거점과 전투 시도
    errorCode.GUILD_BATTLE_MATCH_INVALID_TARGET_BASE_INDEX          = 29125000;      // 길드전 - 전투 - 잘못된 상대 거점 내 방어덱에 전투 시도
    errorCode.GUILD_BATTLE_MATCH_NOT_YET_ACTIVATED                  = 29126000;      // 길드전 - 전투 - 아직 열리지 않은 거점 시도
    errorCode.GUILD_BATTLE_MATCH_NOT_ENOUGH_WING                    = 29127000;      // 길드전 - 전투 - 남은 날개가 없음.
    errorCode.GUILD_BATTLE_MATCH_BLOCKED_ACADEMY_CHARACTER          = 29128000;      // 길드전 - 전투 - 이미 사용된 아카데미 캐릭터
    errorCode.GUILD_BATTLE_MATCH_BLOCKED_CHARACTER                  = 29129000;      // 길드전 - 전투 - 이미 사용된 캐릭터
    **/

    // 거점 전투 종료
    public class cRequestGuildBattleMatchEnd : cRequestBase
    {
        public string cmd = "req_guild_battle_match_end";
        public byte result;                                 // 방금 끝난 round 결과
        public string playToken;
        public List<long> liveList = new List<long>();      // 라운드 생존 캐릭터 DBKey 리스트(유저)
        public List<long> liveListTarget = new List<long>();// 라운드 생존 캐릭터 DBKey 리스트(상대)
        public long guildDBKey;                             // 자신의 길드 DBKey
        public long targetGuildDBKey;                       // 상대의 길드 DBKey
        public int targetBaseID;                            // 거점ID baseID
        public int targetBaseIndex;                         // 거점내 덱 순서 baseIndex
        public int playTurn;                                // 로그용 - 턴 수
        public int autoPlay = 0;                            // 로그용 - 자동전투여부(종료 시점에 켜져있으면 1)
        public int knockDown = 0;                           // 로그용 - 녹다운 횟 수.
        public long damageOneHit = 0;                       // 로그용 - 1회 최고 피해량
        public long topCharacterID = 0;                     // 로그용 - 가장 높은 전투력 높은 캐릭터 ID
        public long topCharacterPower = 0;                  // 로그용 - 가장 높은 전투력 높은 캐릭터 전투력
        public string snack = "";                           // 아레나 처럼 생성.
    }

    public class cResponseGuildBattleMatchEnd : cResponseBase
    {
        public long guildDBKey;                 // 자신의 길드 DBKey
        public long targetGuildDBKey;           // 상대의 길드 DBKey
        public int targetBaseID;                // 거점ID baseID
        public int targetBaseIndex;             // 거점내 덱 순서 baseIndex
        public byte result;                     // 승패
        public byte isValidFinished;            // 0: 차전 종료 됨(결과반영없음, 길드포인트 보상은 줌) / 1: 보스전 시간내에 정상 종료됨.
        public int defencePointBefore;          // 전투 결과 시의 계산 전 포인트
        public int defencePointSub;             // 포인트 변동량(기획상 변동량)
        public int defencePointSubReal;         // 포인트 변동량 실제(0이 최저기 때문에)
        public int defencePointNow;             // 현재 방어포인트.
        public int wing;                        // 날개 개수
        public int guildpoint;                  // 보상 받은 길드 포인트
        public cGuildBattleMemberData guildBattleMember = new cGuildBattleMemberData(); // block 덱 정보
    }
    /**
    errorCode.GUILD_BATTLE_INVALID_STATE_BATTLE                     = 29119000;      // 길드전 - 전투 불가능 한 상태
    errorCode.GUILD_BATTLE_MATCH_INVALID_TARGET                     = 29123000;      // 길드전 - 전투 - 잘못된 상대 길드와 전투 시도
    errorCode.GUILD_BATTLE_MATCH_INVALID_TARGET_BASE                = 29124000;      // 길드전 - 전투 - 잘못된 상대 거점과 전투 시도
    errorCode.GUILD_BATTLE_MATCH_INVALID_TARGET_BASE_INDEX          = 29125000;      // 길드전 - 전투 - 잘못된 상대 거점 내 방어덱에 전투 시도
    errorCode.GUILD_BATTLE_MATCH_NOT_YET_ACTIVATED                  = 29126000;      // 길드전 - 전투 - 아직 열리지 않은 거점 시도
    **/

    // 보스 전투 시작
    public class cRequestGuildBattleMatchBossStart : cRequestBase
    {
        public string cmd = "req_guild_battle_match_boss_start";
        public long guildDBKey;             // 자신의 길드 DBKey
        public int stageID;                 // 보스에 알맞은 stageID
        public cDeck deck = new cDeck();
    }

    public class cResponseGuildBattleMatchBossStart : cResponseBase
    {
        public int wing;                                                                // 날개 개수
        public cGuildBattleBossData guildBattleBoss = new cGuildBattleBossData();       // 현재 보스 상태
        public string playToken;                                                        // end 호출 시 사용.
        public string sugar;
        public long timeSugar;
    }
    /**
    errorCode.GUILD_BATTLE_INVALID_STATE_BATTLE                     = 29119000;      // 길드전 - 전투 불가능 한 상태
    errorCode.GUILD_BATTLE_MATCH_NOT_ENOUGH_WING                    = 29127000;      // 길드전 - 전투 - 남은 날개가 없음.
    errorCode.GUILD_BATTLE_MATCH_BLOCKED_ACADEMY_CHARACTER          = 29128000;      // 길드전 - 전투 - 이미 사용된 아카데미 캐릭터
    errorCode.GUILD_BATTLE_MATCH_BLOCKED_CHARACTER                  = 29129000;      // 길드전 - 전투 - 이미 사용된 캐릭터
    **/

    // 보스 전투 종료
    public class cRequestGuildBattleMatchBossEnd : cRequestBase
    {
        public string cmd = "req_guild_battle_match_boss_end";
        public string playToken;
        public long damage;                                 // 데미지 - snack 값에 들어가 있는 걸로 대체 될 것임.
        public long guildDBKey;                             // 자신의 길드 DBKey
        public int stageID;                                 // 보스에 알맞은 stageID
        public List<long> liveList = new List<long>();      // 라운드 생존 캐릭터 DBKey 리스트(유저)
        public int playTurn;                                // 로그용 - 턴 수
        public int autoPlay = 0;                            // 로그용 - 자동전투여부(종료 시점에 켜져있으면 1)
        public int knockDown = 0;                           // 로그용 - 녹다운 횟 수.
        public long damageOneHit = 0;                       // 로그용 - 1회 최고 피해량
        public long topCharacterID = 0;                     // 로그용 - 가장 높은 전투력 높은 캐릭터 ID
        public long topCharacterPower = 0;                  // 로그용 - 가장 높은 전투력 높은 캐릭터 전투력
        public string snack = "";                           // 보스전, 골드던전 처럼 적용.
    }

    public class cResponseGuildBattleMatchBossEnd : cResponseBase
    {
        public long guildDBKey;         // 자신의 길드 DBKey
        public long damage;             // snack 에서 빼낸 damage
        public byte isValidFinished;    // 0: 보스전 종료 후(보스점수반영없음, 날개반환, 길드포인트보상없음) / 1: 보스전 시간내에 정상 종료됨.
        public int beforePoint;         // 전투 결과 시의 계산 전 점수
        public int addPoint;            // 포인트 변동량(보스전이 종료된 경우 변동이 없음)
        public int nowPoint;            // 현재 점수
        public int wing;                // 날개 개수
        public int guildpoint;          // 보상 받은 길드 포인트(보스전이 종료 되어도 보상은 받을 수 있음)
        public cGuildBattleBossData guildBattleBoss = new cGuildBattleBossData();   // 현재 보스 상태(우리길드vs상대길드)
        public cGuildBattleMemberData guildBattleMember = new cGuildBattleMemberData(); // block 덱 정보, 보스전 종료로 날개 돌려받을 수 있음.
    }
    /**
    errorCode.GUILD_BATTLE_INVALID_STATE_BATTLE                     = 29119000;      // 길드전 - 전투 불가능 한 상태
    **/
    #endregion

    #region ETHER
    ///////////////////////////////////////////////////////////////////////////////
    //  url : address + '/ether'
    ///////////////////////////////////////////////////////////////////////////////
    public class cRequestEtherEquip : cRequestBase
    {
        public string cmd = "req_ether_equip";
        public long etherDBKey;
        public long characterDBKey;
        public int slotIndex; // 1~6
    }

    public class cResponseEtherEquip : cResponseBase
    {
        public cCharacter character = new cCharacter();
        public cEther ether = new cEther();
        public long etherDBKey;
    }

    public class cRequestEtherUnEquip : cRequestBase
    {
        public string cmd = "req_ether_unequip";
        public long etherDBKey;
        public long characterDBKey;
        public int slotIndex; // 1~6
    }

    public class cResponseEtherUnEquip : cResponseBase
    {
        public cCharacter character = new cCharacter();
        public cEther ether = new cEther();
        public long etherDBKey;
        public cItem item = new cItem();
        public cUserMoney userMoney = new cUserMoney();
    }

    public class cRequestUpgradeEther : cRequestBase
    {
        public string cmd = "req_ether_upgrade";
        public long etherDBKey;
    }

    public class cResponseUpgradeEther : cResponseBase
    {
        public cUserMoney reward = new cUserMoney();
        public cEther ether = new cEther();
        public bool isSuccess;
    }

    public class cRequestDisassembleEther : cRequestBase
    {
        public string cmd = "req_ether_disassemble";
        public List<long> etherDBKeys = new List<long>();
    }

    public class cResponseDisassembleEther : cResponseBase
    {
        public List<long> deleteEthers = new List<long>();
        public cUserMoney reward = new cUserMoney();
        public List<cCharacter> characters = new List<cCharacter>();
    }

    public class cRequestLockChangeEther : cRequestBase
    {
        public string cmd = "req_ether_lock_change";
        public long etherDBKey;
    }

    public class cResponseLockChangeEther : cResponseBase
    {
        public cEther ether = new cEther();
    }

    public class cRequestEnchantEther : cRequestBase
    {
        public string cmd = "req_ether_enchant";
        public long etherDBKey;
        public int selectEnchant;
    }

    public class cResponseEnchantEther : cResponseBase
    {
        public cEther ether = new cEther();
        public cItem item = new cItem();
        public cUserMoney reward = new cUserMoney();
        public long resultSetID;
        public cGachaStoreEnchant gachaStoreEnchant = new cGachaStoreEnchant();
    }

    public class cRequestExpandEtherInventory : cRequestBase
    {
        public string cmd = "req_ether_inventory_expand";
    }

    public class cResponseExpandEtherInventory : cResponseBase
    {
        public cUserMoney userMoney = new cUserMoney();
        public int etherInventory;
    }
    #endregion

    #region ACADEMYCHARACTER

    ///////////////////////////////////////////////////////////////////////////////
    //  url : address + '/academy_character'
    ///////////////////////////////////////////////////////////////////////////////
    public class cRequestAcademyCharacterSkillUp : cRequestBase
    {
        public string cmd = "req_academy_character_skill_up";
        public long academyCharacterDBKey;
        public long itemDBKey;
        public long skillID;
    }

    public class cResponseAcademyCharacterSkillUp : cResponseBase
    {
        public cAcademyCharacter academyCharacter = new cAcademyCharacter();
        public cUserMoney userMoney = new cUserMoney();
        public cItem item = new cItem();
    }
    #endregion

    #region gacha
    ///////////////////////////////////////////////////////////////////////////////
    //  url : address + '/gacha'
    ///////////////////////////////////////////////////////////////////////////////
    public class cRequestBuyGachaStore : cRequestBase
    {
        public string cmd = "req_buy_gacha_store";
        public long storeId;
    }
    public class cResponseBuyGachaStore : cResponseBase
    {
        public cUserMoney userMoney = new cUserMoney();
        public cItem updateItem = new cItem();
        public List<cGachaData> gachaList = new List<cGachaData>();
        public int pickupCount;
        public int pickupReward;
        public cGachaMileage mileage = new cGachaMileage();
        public List<cGachaStore> gachastores = new List<cGachaStore>();
    }

    public class cRequestPickGachaData : cRequestBase
    {
        public string cmd = "req_pick_gacha";
        public byte index;
    }
    public class cResponsePickGachaData : cResponseBase
    {
        public cUserMoney userMoney = new cUserMoney();
        public List<cItem> updateItem = new List<cItem>();
        public List<cGachaData> gachaList = new List<cGachaData>();
        public cCharacter character = new cCharacter();
        public int pickupCount;
        public int pickupReward;
        public List<cAttendanceRewardItem> pickupItem = new List<cAttendanceRewardItem>();
        public cGachaMileage mileage = new cGachaMileage();
    }

    public class cRequestClearGachaData : cRequestBase
    {
        public string cmd = "req_clear_gacha_data";
    }
    public class cResponseClearGachaData : cResponseBase
    {
    }

    public class cRequestBuyGachaStoreBundle : cRequestBase
    {
        public string cmd = "req_buy_gacha_store_bundle";
        public long storeId;
    }
    public class cResponseBuyGachaStoreBundle : cResponseBase
    {
        public cUserMoney userMoney = new cUserMoney();
        public List<cItem> updateItem = new List<cItem>();
        public List<cCharacter> updateCharacter = new List<cCharacter>();
        public List<cGachaData> gachaList = new List<cGachaData>();
        public int pickupCount;
        public int pickupReward;
        public List<cAttendanceRewardItem> pickupItem = new List<cAttendanceRewardItem>();
        public cGachaMileage mileage = new cGachaMileage();
        public List<cGachaStore> gachastores = new List<cGachaStore>();
    }

    public class cRequestRewardGachaMileage : cRequestBase
    {
        public string cmd = "req_reward_gacha_mileage";
        public long storeId;
        public short rewardId;
    }
    public class cResponseRewardGachaMileage : cResponseBase
    {
        public cAttendanceRewardItem rewardItem = new cAttendanceRewardItem();
        public cAttendanceRewardMoney rewardMoney = new cAttendanceRewardMoney();
        public cGachaMileage mileage = new cGachaMileage();
    }

    public class cRequestBuyGachaStoreNonePick : cRequestBase
    {
        public string cmd = "req_buy_gacha_store_none_pick";
        public long storeId;
    }
    public class cResponseBuyGachaNonePick : cResponseBase
    {
        public cUserMoney userMoney = new cUserMoney();
        public List<cItem> updateItem = new List<cItem>();
        public List<cCharacter> updateCharacter = new List<cCharacter>();
        public List<cGachaData> gachaList = new List<cGachaData>();
        public int pickupCount;
        public int pickupReward;
        public List<cAttendanceRewardItem> pickupItem = new List<cAttendanceRewardItem>();
        public cGachaMileage mileage = new cGachaMileage();
        public List<cGachaStore> gachastores = new List<cGachaStore>();
    }

    public class cRequestBuyGachaStoreExtraBundle : cRequestBase
    {
        public string cmd = "req_buy_gacha_store_extra_bundle";
        public long storeId;
    }
    public class cResponseBuyGachaStoreExtraBundle : cResponseBase
    {
        public cUserMoney userMoney = new cUserMoney();
        public List<cItem> updateItem = new List<cItem>();
        public List<cGachaData> gachaList = new List<cGachaData>();
        public int resetCount;
        public List<cGachaStore> gachastores = new List<cGachaStore>();
    }

    public class cRequestExtraGachaData : cRequestBase
    {
        public string cmd = "req_reset_gacha_extra_data";
    }
    public class cResponseExtraGachaData : cResponseBase
    {
        public List<cGachaData> gachaList = new List<cGachaData>();
        public int resetCount;
    }

    public class cRequestFixGachaStoreExtraBundle : cRequestBase
    {
        public string cmd = "req_fix_gacha_extra_data";
    }
    public class cResponseFixGachaStoreExtraBundle : cResponseBase
    {
        public cUserMoney userMoney = new cUserMoney();
        public List<cItem> updateItem = new List<cItem>();
        public List<cCharacter> updateCharacter = new List<cCharacter>();
        public List<cGachaData> gachaList = new List<cGachaData>();
    }
    #endregion

    #region BOSSRAID
    ///////////////////////////////////////////////////////////////////////////////
    //  url : address + '/boss_raid'
    ///////////////////////////////////////////////////////////////////////////////

    public class cRequestBossRaidInfo : cRequestBase
    {
        public string cmd = "req_boss_raid_info";
    }

    public class cResponseBossRaidInfo : cResponseBase
    {
        public int themeID;
        public long seasonResetTime;
        public int myRanking;
        public float myRankingRate;          // 랭킹 퍼센트
        public long dayDamage;               // 오늘 최고 기록
        public long bestDamage;              // 시즌 최고 피해량
        public long seasonDamage;            // 시즌 누적 피해량
        public byte bossRaidTryCount;
        public byte bossRaidTryCountHotTime;
        public int lastReward;
        public cBossRaidRankingInfo currSessionRankingInfo = new cBossRaidRankingInfo();
        public cBossRaidRankingInfo lastSessionRankingInfo = new cBossRaidRankingInfo();
    }

    public class cRequestBossRaidPlayStart : cRequestBase
    {
        public string cmd = "req_boss_raidplay_start";
        public int stageIndex;
        public cDeck deck = new cDeck();
    }

    public class cResponseBossRaidPlayStart : cResponseBase
    {
        public string playtoken;
        public int stageIndex;
        public string sugar;
        public long timeSugar;
    }

    public class cRequestBossRaidPlayEnd : cRequestBase
    {
        public string cmd = "req_boss_raidplay_end";
        public string playtoken;
        public int stageIndex;
        public long damage;
        public cDeck deck = new cDeck();
        public string snack = "";
        public int playTurn;                           // 로그용 - 턴 수
        public int autoPlay = 0;                       // 로그용 - 자동전투여부(종료 시점에 켜져있으면 1)
        public int knockDown = 0;                      // 로그용 - 녹다운 횟 수.
        public long damageOneHit = 0;                   // 로그용 - 1회 최고 피해량
        public long topCharacterID = 0;                 // 로그용 - 가장 높은 전투력 높은 캐릭터 ID
        public long topCharacterPower = 0;              // 로그용 - 가장 높은 전투력 높은 캐릭터 전투력
    }

    public class cResponseBossRaidPlayEnd : cResponseBase
    {
        public cItemReward reward = new cItemReward();
        public byte bossRaidTryCount;
        public byte bossRaidTryCountHotTime;
        public long damage;
        public long diffDamage;
        public long seasonDamage;
    }

    public class cRequestBossRaidLastReward : cRequestBase
    {
        public string cmd = "req_boss_raid_last_reward";
    }

    public class cResponseBossRaidLastReward : cResponseBase
    {
        public cItemReward reward = new cItemReward();
        public int lastRanking;
        public long lastSessionDamage;
    }

    #endregion

    #region PACKAGE
    ///////////////////////////////////////////////////////////////////////////////
    //  url : address + '/package'
    ///////////////////////////////////////////////////////////////////////////////

    // req_package_purchase 호출 전 패키지 구매가능 서버체크 (Restore시에는 호출안함)
    public class cRequestPackagePurchaseCheck : cRequestBase
    {
        public string cmd = "req_package_purchase_check";
        public string marketPID;
    }

    public class cResponsePackagePurchaseCheck : cResponseBase
    {
        public string marketPid;
    }

    public class cRequestPackagePurchase : cRequestBase
    {
        public string cmd = "req_package_purchase";
        public cBilling billingInfo = new cBilling();
    }

    public class cResponsePackagePurchase : cResponseBase
    {
        public string marketPid;
        public string orderId;
    }

    public class cRequestPackagePurchaseNonCash : cRequestBase
    {
        public string cmd = "req_package_purchase_non_cash";
        public long packageId;
    }

    public class cResponsePackagePurchaseNonCash : cResponseBase
    {
        public cUserMoney userMoney = new cUserMoney();
    }

    public class cRequestPackageList : cRequestBase
    {
        public string cmd = "req_package_list";
    }

    public class cResponsePackageList : cResponseBase
    {
        public List<cPackage> packageDataList = new List<cPackage>();
    }

    public class cRequestPackageTimeLimitList : cRequestBase
    {
        public string cmd = "req_package_time_limit_list";
    }

    public class cResponsePackageTimeLimitList : cResponseBase
    {
        public List<cPackage> packageDataList = new List<cPackage>();
    }

    public class cRequestPackageSubscribeStateList : cRequestBase
    {
        public string cmd = "req_package_subscribe_state_list";
    }

    public class cResponsePackageSubscribeStateList : cResponseBase
    {
        public List<cPackageSubscribeState> packageSubscribeStateList = new List<cPackageSubscribeState>();
    }

    public class cRequestPackageRewardMission : cRequestBase
    {
        public string cmd = "req_package_reward_mission";
        public long packageId;       // PakcageStoreInfo의 PackageStoreItemID
        public long missionRewardId; // PackageMissionType 기획테이블의 PackageMissionCheckID
    }

    public class cResponsePackageRewardMission : cResponseBase
    {
        public List<cPackage> packageDataList = new List<cPackage>();
    }

    public class cRequestPackageRewardPass : cRequestBase
    {
        public string cmd = "req_package_reward_pass";
        public List<cPackagePassRewardRequest> rewardPass = new List<cPackagePassRewardRequest>();
    }

    public class cResponsePackageRewardPass : cResponseBase
    {
        public List<cPackage> packageDataList = new List<cPackage>();
    }
    #endregion

    #region pass
    ///////////////////////////////////////////////////////////////////////////////
    //  url : address + '/pass'
    ///////////////////////////////////////////////////////////////////////////////

    public class cRequestPassMissionInfo : cRequestBase
    {
        public string cmd = "req_pass_mission_info";
        public long passID;
    }

    public class cResponsePassMissionInfo : cResponseBase
    {
        public cItem pointItem = new cItem();
        public List<cPassMission> missions = new List<cPassMission>();
    }

    public class cRequestPassMissionReward : cRequestBase
    {
        public string cmd = "req_pass_mission_reward";
        public long passID;
        public List<long> missionIds = new List<long>();
    }

    public class cResponsePassMissionReward : cResponseBase
    {
        public cItem pointItem = new cItem();
        public List<cPassMission> missions = new List<cPassMission>();
    }

    public class cRequestPassStepUp : cRequestBase
    {
        public string cmd = "req_pass_step_up";
        public long passID;
        public int stepIndex;
    }

    public class cResponsePassStepUp : cResponseBase
    {
        public cItem pointItem = new cItem();
        public cUserMoney userMoney = new cUserMoney();
    }
    #endregion

    #region sweep
    ///////////////////////////////////////////////////////////////////////////////
    //  url : address + '/sweep'
    ///////////////////////////////////////////////////////////////////////////////

    public class cRequestSweepReward : cRequestBase
    {
        public string cmd = "req_sweep_reward";
        public int battleContentsType;
        public long stageIndex;
    }

    public class cResponseSweepReward : cResponseBase
    {
        public SweepBossResult bossResult = new SweepBossResult();
        public SweepGoldResult goldResult = new SweepGoldResult();
        public SweepStageResult stageResult = new SweepStageResult();
        public cSweep sweepData = new cSweep();
    }

    public class cRequestSweepInfo : cRequestBase
    {
        public string cmd = "req_sweep_info";
    }

    public class cResponseSweepInfo : cResponseBase
    {
        public cSweep sweepData = new cSweep();
    }

    #endregion

    #region ELEMENTSTONE
    ///////////////////////////////////////////////////////////////////////////////
    //  url : address + '/element_stone'
    ///////////////////////////////////////////////////////////////////////////////

    public class cRequestElementEnchantStore : cRequestBase
    {
        public string cmd = "req_element_stone_enchant_store";
        public long characterDBKey;
        public long itemDBKey;
    }

    public class cResponseElementEnchantStore : cResponseBase
    {
        public List<cItem> updateItem = new List<cItem>();          // 사용한 아이템 및 획득한 아이템
        public cGachaStoreElementEnchant gachaStoreElementEnchant = new cGachaStoreElementEnchant();
        public cUserMoney userMoney = new cUserMoney();
        public bool isSuccess; // 1 : 성공, 0: 실패
    }

    public class cRequestElementEnchant : cRequestBase
    {
        public string cmd = "req_element_stone_enchant";
        public int selectEnchant; // 1 : 새로운효과 , 2: 기존효과
    }

    public class cResponseElementEnchant : cResponseBase
    {
        public cCharacter character = new cCharacter();
        public cUserMoney userMoney = new cUserMoney();
        public cGachaStoreElementEnchant gachaStoreElementEnchant = new cGachaStoreElementEnchant();
    }

    #endregion

    #region CONNECTION
    ///////////////////////////////////////////////////////////////////////////////
    //  url : address + '/connection'
    ///////////////////////////////////////////////////////////////////////////////

    public class cRequestConnectionList : cRequestBase
    {
        public string cmd = "req_connection_list";
    }

    public class cResponseConnectionList : cResponseBase
    {
        public cConnectionDataList connectionDataList = new cConnectionDataList();
    }

    public class cRequestConnectionReward : cRequestBase
    {
        public string cmd = "req_connection_reward";
        public long connectionConditionID; // connectionConditionTable.ConnectionConditionID
    }

    public class cResponseConnectionReward : cResponseBase
    {
        public cConnectionDataList connectionDataList = new cConnectionDataList();
        public cItemEtherReward reward = new cItemEtherReward();
    }

    #endregion


    #region LOLLIPOP
    ///////////////////////////////////////////////////////////////////////////////
    //  url : address + '/lollipop'
    ///////////////////////////////////////////////////////////////////////////////

    public class cRequestLollipopMissionInfo : cRequestBase
    {
        public string cmd = "req_mission_info";
    }

    public class cResponseLollipopMissionInfo : cResponseBase
    {
        public List<cEventMission> missions = new List<cEventMission>();    // 기존 cEventMission 과 구조가 비슷해서 이걸로 쓰기로함.
    }

    public class cRequestLollipopMissionReward : cRequestBase
    {
        public string cmd = "req_mission_reward";
        public List<long> missionIds = new List<long>();
    }

    public class cResponseLollipopMissionReward : cResponseBase
    {
        public cItemEtherReward reward = new cItemEtherReward();
        public List<cEventMission> missions = new List<cEventMission>();
    }

    public class cRequestLollipopShopInfo : cRequestBase
    {
        public string cmd = "req_lollipop_shop_info";
    }

    public class cResponseLollipopShopInfo : cResponseBase
    {
        public List<cEventShop> list = new List<cEventShop>();
    }

    public class cRequestLollipopBuyShop : cRequestBase
    {
        public string cmd = "req_lollipop_buy_shop";
        public long eventShopID;
        public int count;
    }

    public class cResponseLollipopBuyShop : cResponseBase
    {
        public cItemEtherReward reward = new cItemEtherReward();
        public int count;
        public cItem useItem = new cItem();
    }

    public class cRequestLollipopGacha : cRequestBase
    {
        public string cmd = "req_lollipop_gacha";
    }

    public class cResponseLollipopGacha : cResponseBase
    {
        public List<cItem> updateItem = new List<cItem>();          // 사용한 아이템 및 획득한 아이템
    }

    public class cRequestLollipopCompose : cRequestBase
    {
        public string cmd = "req_lollipop_compose";
        public int functionID;
    }

    public class cResponseLollipopCompose : cResponseBase
    {
        public List<cItem> updateItem = new List<cItem>();          // 사용한 아이템 및 획득한 아이템
    }

    public class cRequestLollipopExchange : cRequestBase
    {
        public string cmd = "req_lollipop_exchange";
        public int functionID;
    }

    public class cResponseLollipopExchange : cResponseBase
    {
        public List<cItem> updateItem = new List<cItem>();          // 사용한 아이템 및 획득한 아이템
    }

    #endregion
}
