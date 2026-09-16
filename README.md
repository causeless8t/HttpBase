# HttpBase

Unity 프로젝트에서 Handler 기반 JSON HTTP 요청을 구성하기 위한 경량 UPM 패키지입니다.

`HttpManager`가 요청 대기열, 동시 실행 제한, 연결 오류 재시도, 요청 병합과 수명 주기를 관리합니다. 각 API의 요청 생성, 응답 파싱 및 오류 처리는 `RequestHandler` 구현에서 정의합니다.

## 주요 기능

- `UnityWebRequest` 기반 JSON POST 요청
- Handler 어셈블리 자동 등록 및 인스턴스 수동 등록
- 최대 동시 요청 수 제한
- 연결 오류 재시도
- API별 요청 병합과 지연 전송
- 요청별 메시지와 콜백 격리
- 응답 완료 순서와 무관한 진행 요청 추적
- `RequestInfo` 오브젝트 풀링
- Manager 종료 시 대기·진행·재시도 요청 정리
- UnityCore, UniTask 등 외부 패키지 의존성 없음
- IL2CPP Handler 보존을 위한 `APIAttribute : PreserveAttribute`

## 요구 사항

- Unity 2022.3 이상

## 설치

Unity Package Manager에서 **Add package from git URL...**을 선택하고 다음 주소를 입력합니다.

```text
https://github.com/causeless8t/HttpBase.git
```

특정 버전을 고정하려면 Git 태그를 사용합니다.

```text
https://github.com/causeless8t/HttpBase.git#2.0.0
```

또는 `Packages/manifest.json`에 추가합니다.

```json
{
  "dependencies": {
    "com.causeless3t.httpbase": "https://github.com/causeless8t/HttpBase.git#2.0.0"
  }
}
```

## 빠른 시작

### 1. HttpManager 배치

Scene의 GameObject에 `HttpManager` 컴포넌트를 추가합니다. 패키지는 Singleton을 강제하지 않으므로 프로젝트의 서비스 구조에 맞게 참조하거나 감싸서 사용합니다.

```csharp
using UnityEngine;

public sealed class GameBootstrap : MonoBehaviour
{
    [SerializeField]
    private HttpManager _httpManager;

    private void Awake()
    {
        var options = new HttpManagerOptions(
            baseUrl: "https://api.example.com",
            timeoutSeconds: 15,
            maxConcurrentRequests: 5,
            maxRetryAttempts: 3,
            retryDelaySeconds: 2f,
            bundleDelaySeconds: 5f);

        _httpManager.Initialize(
            options,
            typeof(LoginHandler).Assembly);
    }
}
```

`Initialize()`에 전달된 어셈블리에서 구체 클래스이고 `IRequestHandler`를 구현하며 `[API]`가 붙은 타입을 등록합니다.

### 2. 요청과 응답 정의

`JsonUtility`를 사용하므로 직렬화할 클래스에 `[Serializable]`을 지정하고 필드 기반 데이터 모델을 사용합니다.

```csharp
using System;

[Serializable]
public sealed class LoginRequest : IRequest
{
    public string userId;
}

[Serializable]
public sealed class LoginResponse : BaseResponse
{
    public string accessToken;
}
```

### 3. Handler 구현

```csharp
[API("auth/login")]
public sealed class LoginHandler
    : RequestHandler<LoginRequest, LoginResponse, string>
{
    protected override LoginRequest MakeReqMessage(string userId)
    {
        return new LoginRequest
        {
            userId = userId
        };
    }

    protected override void Process(
        RequestInfo requestInfo,
        LoginResponse response)
    {
        Debug.Log(response.accessToken);
    }

    public override void ErrorProcess(
        RequestInfo requestInfo,
        BaseResponse response,
        int error)
    {
        Debug.LogError($"Login failed: {error}");
    }
}
```

`[API]`가 붙은 Handler는 자동 등록 대상이며 public 기본 생성자가 필요합니다. `APIAttribute`가 `PreserveAttribute`를 상속하므로 Reflection으로 생성하는 Handler와 기본 생성자가 IL2CPP 스트리핑에서 보존됩니다.

### 4. 요청 전송

```csharp
var handler = _httpManager.GetHandler<LoginHandler>();

handler.EnqueueRequest(
    "player-id",
    (requestInfo, rawResponse) =>
    {
        Debug.Log(
            $"{requestInfo.Protocol}: {rawResponse}");
    });
```

콜백과 `Process()`는 응답 처리 중 Unity 메인 스레드에서 호출됩니다.

## 수동 Handler 등록

생성자 의존성이 있는 Handler에는 `[API]`를 붙이지 않고 `API`를 override합니다.

```csharp
public sealed class LoginHandler
    : RequestHandler<LoginRequest, LoginResponse>
{
    private readonly ISessionStorage _sessionStorage;

    public override string API => "auth/login";

    public LoginHandler(ISessionStorage sessionStorage)
    {
        _sessionStorage = sessionStorage;
    }

    protected override LoginRequest MakeReqMessage()
    {
        return new LoginRequest();
    }

    protected override void Process(
        RequestInfo requestInfo,
        LoginResponse response)
    {
        _sessionStorage.Save(response.accessToken);
    }
}
```

먼저 Manager를 초기화한 뒤 인스턴스를 등록합니다.

```csharp
_httpManager.Initialize(options);
_httpManager.RegisterHandler(
    new LoginHandler(sessionStorage));
```

| 등록 방식 | `[API]` | public 기본 생성자 | API 경로 |
| --- | ---: | ---: | --- |
| 어셈블리 자동 등록 | 필요 | 필요 | Attribute 값 |
| 인스턴스 수동 등록 | 불필요 | 불필요 | `API` override |

## HttpManagerOptions

| 옵션 | 기본값 | 설명 |
| --- | ---: | --- |
| `BaseUrl` | 필수 | HTTP 또는 HTTPS 절대 URL |
| `TimeoutSeconds` | 15 | UnityWebRequest timeout |
| `MaxConcurrentRequests` | 5 | 동시에 실행할 최대 요청 수 |
| `MaxRetryAttempts` | 3 | 최초 요청을 제외한 연결 오류 재시도 횟수 |
| `RetryDelaySeconds` | 2 | 재시도 대기 시간 |
| `BundleDelaySeconds` | 5 | 병합 요청을 모으는 시간 |

설정 객체는 생성 이후 변경할 수 없습니다. URL은 Base URL의 마지막 `/`와 API 경로의 첫 `/`를 정규화해 조합합니다.

## 오류 및 재시도 정책

| 결과 | 처리 |
| --- | --- |
| `ConnectionError` | 설정된 횟수만큼 재시도 |
| HTTP 4xx·5xx | `ErrorProcess()`에 HTTP 상태 코드 전달 |
| 응답의 `ErrCode != 0, 200` | `ErrorProcess()`에 ErrCode 전달 |
| `DataProcessingError` | 재시도 없이 `-1` 전달 |
| 비어 있는 성공 응답 | 재시도 없이 `-1` 전달 |
| HTTP 2xx | 성공 응답 파싱 |

HTTP 프로토콜 오류는 연결 장애로 재시도하지 않습니다. 재시도 중에도 같은 `RequestInfo`와 요청 본문을 유지하며 패킷 번호만 새로 발급합니다.

## 병합 요청

짧은 시간에 발생하는 동일 API 요청을 하나로 합치려면 요청 타입에서 `ICollapsableRequest`를 구현합니다.

```csharp
[Serializable]
public sealed class SaveProgressRequest :
    ICollapsableRequest
{
    public int stage;
    public int score;

    public bool Equals(ICollapsableRequest other)
    {
        return other is SaveProgressRequest;
    }

    public void Collapse(ICollapsableRequest request)
    {
        var other = (SaveProgressRequest)request;
        stage = Math.Max(stage, other.stage);
        score = Math.Max(score, other.score);
    }
}
```

- `Equals()`가 `true`이면 기존 메시지에 `Collapse()`하고 콜백을 누적합니다.
- `Equals()`가 `false`이면 같은 API의 기존 그룹을 즉시 전송하고 새 그룹을 시작합니다.
- 서로 다른 API의 병합 대기와 취소 상태는 독립적입니다.

> 공개 API 이름은 현재 호환성을 위해 `ICollapsableRequest` 철자를 유지합니다.

## 수명 주기

`Initialize()`를 다시 호출하면 기존 요청을 정리하고 새 설정으로 초기화합니다. `Dispose()` 또는 `OnDestroy()`에서는 다음 작업을 수행합니다.

- 활성 UnityWebRequest 중단
- 모든 요청 및 지연 코루틴 종료
- 대기·진행·재시도 중인 RequestInfo 풀 반환
- 병합 요청과 Handler 등록 정보 제거

Manager가 비활성화된 동안에는 코루틴을 시작할 수 없으므로 요청을 보내기 전에 활성 상태인지 확인해야 합니다.

## 샘플

Package Manager에서 **Basic Request** 샘플을 Import할 수 있습니다.

샘플에는 다음 내용이 포함됩니다.

- `HttpManagerOptions` 생성과 초기화
- Handler 어셈블리 등록
- 요청·응답 메시지
- 매개변수 기반 Handler
- 성공 콜백과 오류 처리

샘플 Base URL은 placeholder이므로 실제 서버 주소와 API 스키마로 교체해야 합니다.

## 현재 범위

현재 Runtime은 다음 범위를 대상으로 합니다.

- JSON POST
- `JsonUtility` 직렬화
- Handler 기반 응답 처리
- 연결 오류 재시도
- 단순 동시 요청 제한과 요청 병합

다음 기능은 사용하는 프로젝트에서 확장해야 합니다.

- GET, PUT, PATCH, DELETE
- 인증 Header와 공통 Header
- 파일 업로드 및 다운로드
- 다른 JSON 직렬화 라이브러리
- 지수 백오프와 상태 코드별 재시도 정책
- 요청별 취소와 진행률
- 캐시 및 오프라인 큐

## 프로젝트 구조

```text
HttpBase/
├── Runtime/
│   ├── Causeless3t.HttpBase.asmdef
│   ├── HttpManager.cs
│   ├── HttpManagerOptions.cs
│   ├── IRequestHandler.cs
│   └── RequestInfo.cs
├── Samples~/
│   └── BasicRequest/
├── CHANGELOG.md
├── LICENSE
├── README.md
└── package.json
```

## 라이선스

이 프로젝트는 [MIT License](LICENSE)를 따릅니다.
