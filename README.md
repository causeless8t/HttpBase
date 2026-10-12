# HttpBase

Unity 프로젝트에서 핸들러 기반 JSON HTTP 요청을 구성하기 위한 경량 UPM 패키지입니다.

`HttpManager`가 요청 대기열, 동시 실행 제한, 연결 오류 재시도, 요청 병합과 수명 주기를 관리합니다. 각 API의 요청 생성, 응답 해석 및 오류 처리는 `RequestHandler` 구현에서 정의합니다.

## 주요 기능

- `UnityWebRequest` 기반 JSON 요청(GET, POST, PUT, DELETE, PATCH)
- 공통 헤더, 요청별 헤더, 전송 직전 헤더(인증 토큰 등)
- 204·205 No Content 응답 처리
- 핸들러별 연결 오류 재시도 여부와 전송 대기(토큰 갱신 중 대기 등)
- 요청별 호출자 상태(`UserState`)로 결과 전달
- 핸들러 어셈블리 자동 등록 및 인스턴스 수동 등록
- 최대 동시 요청 수 제한
- 연결 오류 재시도
- API별 요청 병합과 지연 전송
- 요청별 메시지와 콜백 격리
- 응답 완료 순서와 무관한 진행 요청 추적
- `RequestInfo` 오브젝트 풀링
- 매니저 종료 시 대기·진행·재시도 요청 정리
- UnityCore, UniTask 등 외부 패키지 의존성 없음
- IL2CPP 핸들러 보존을 위한 `APIAttribute : PreserveAttribute`

## 요구 사항

- Unity 2022.3 이상

## 설치

Unity Package Manager에서 **Add package from git URL...** 메뉴를 선택하고 다음 주소를 입력합니다.

```text
https://github.com/causeless8t/HttpBase.git
```

특정 버전을 고정하려면 Git 태그를 사용합니다.

```text
https://github.com/causeless8t/HttpBase.git#2.1.0
```

또는 `Packages/manifest.json`에 추가합니다.

```json
{
  "dependencies": {
    "com.causeless3t.httpbase": "https://github.com/causeless8t/HttpBase.git#2.1.0"
  }
}
```

## 빠른 시작

### 1. HttpManager 배치

씬의 GameObject에 `HttpManager` 컴포넌트를 추가합니다. 패키지는 싱글턴을 강제하지 않으므로 프로젝트의 서비스 구조에 맞게 참조하거나 감싸서 사용합니다.

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

`Initialize()`에 전달한 어셈블리에서 구체 클래스이면서 `IRequestHandler`를 구현하고 `[API]`가 붙은 타입을 자동으로 등록합니다.

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

### 3. 핸들러 구현

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

`[API]`가 붙은 핸들러는 자동 등록 대상이며 공개 기본 생성자가 필요합니다. `APIAttribute`가 `PreserveAttribute`를 상속하므로 리플렉션으로 생성하는 핸들러와 기본 생성자가 IL2CPP 스트리핑 과정에서 보존됩니다.

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

콜백과 `Process()`는 응답 처리 중 Unity 주 스레드에서 호출됩니다.

## 수동 핸들러 등록

생성자 의존성이 있는 핸들러에는 `[API]`를 붙이지 않고 `API`를 override합니다.

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

먼저 매니저를 초기화한 뒤 인스턴스를 등록합니다.

```csharp
_httpManager.Initialize(options);
_httpManager.RegisterHandler(
    new LoginHandler(sessionStorage));
```

| 등록 방식 | `[API]` | public 기본 생성자 | API 경로 |
| --- | ---: | ---: | --- |
| 어셈블리 자동 등록 | 필요 | 필요 | 특성 값 |
| 인스턴스 수동 등록 | 불필요 | 불필요 | `API` override |

## HttpManagerOptions

| 옵션 | 기본값 | 설명 |
| --- | ---: | --- |
| `BaseUrl` | 필수 | HTTP 또는 HTTPS 절대 URL |
| `TimeoutSeconds` | 15 | UnityWebRequest 제한 시간 |
| `MaxConcurrentRequests` | 5 | 동시에 실행할 최대 요청 수 |
| `MaxRetryAttempts` | 3 | 최초 요청을 제외한 연결 오류 재시도 횟수 |
| `RetryDelaySeconds` | 2 | 재시도 대기 시간 |
| `BundleDelaySeconds` | 5 | 병합 요청을 모으는 시간 |
| `DefaultHeaders` | 없음 | 모든 요청에 붙는 공통 헤더(생성 시 복사) |
| `LogRequestBodies` | true | 요청 본문 로그 허용. 헤더는 로그에 남기지 않음 |

설정 객체는 생성 이후 변경할 수 없습니다. URL은 기본 URL의 마지막 `/`와 API 경로의 첫 `/`를 정규화해 조합합니다.

## HTTP Method

`[API]`의 `Method` 속성이나 `Method` override로 지정합니다. 지정하지 않은 핸들러는 2.0.0과 같이 POST입니다.

```csharp
[API("api/v1/user-data", Method = RequestMethod.Get)]
public sealed class GetUserDataHandler : RequestHandler<EmptyRequest, UserDataResponse>
{
    ...
}
```

- POST, PUT, PATCH는 요청 객체를 JSON 본문으로 보내고 GET, DELETE는 본문 없이 보냅니다.
- 등록 키는 Method와 경로입니다. 같은 경로의 GET·PUT 핸들러를 함께 등록할 수 있습니다. `GetHandler(api)`는 POST 핸들러를 우선하고, 없으면 그 경로의 유일한 핸들러를 돌려줍니다. 여러 개면 `GetHandler(api, method)`를 사용합니다.
- 본문 형식을 바꾸려면 `SerializeRequest()`, 응답 변환을 바꾸려면 `DeserializeResponse()`를 override합니다.

## 헤더

모든 요청에 `Accept: application/json`, 본문이 있는 요청에 `Content-Type: application/json`을 붙입니다. 이어서 다음 순서로 적용하며 뒤의 값이 앞의 값을 덮어씁니다.

| 순서 | 위치 | 적용 시점 | 용도 |
| --- | --- | --- | --- |
| 1 | `HttpManagerOptions.DefaultHeaders` | 전송 직전 | 앱 버전 등 공통 값 |
| 2 | `AddRequestHeaders(request, ref headers)` | 요청 생성 시 고정 | 요청마다 다른 값 |
| 3 | `ApplyHeaders(info, headers)` | 전송 직전(재시도마다 다시) | `Authorization: Bearer` 등 바뀔 수 있는 값 |

인증 헤더는 `ApplyHeaders()`에서 넣으므로 재시도 전송에도 그 시점의 토큰을 씁니다. 토큰을 갱신하는 동안 `IsReadyToSend()`가 false를 돌려주면 요청은 재시도 횟수를 쓰지 않고 대기열에 남습니다.

```csharp
public sealed class ProfileHandler : RequestHandler<EmptyRequest, ProfileResponse>
{
    private readonly ITokenSource _tokens;

    public ProfileHandler(ITokenSource tokens) => _tokens = tokens;

    public override string API => "api/v1/me";
    public override RequestMethod Method => RequestMethod.Get;

    protected override bool IsReadyToSend(RequestInfo info) => !_tokens.IsRefreshing;

    protected override void ApplyHeaders(RequestInfo info, IDictionary<string, string> headers)
    {
        headers["Authorization"] = "Bearer " + _tokens.AccessToken;
    }

    protected override EmptyRequest MakeReqMessage() => new();
    protected override void Process(RequestInfo info, ProfileResponse response) { }
}
```

## 요청별 결과 전달

핸들러 인스턴스에는 요청별 콜백을 두지 않습니다. 호출자의 결과 대기 객체는 `EnqueueRequestWithState(param, state)`로 넘기고 `Process()`·`ErrorProcess()`에서 `requestInfo.UserState`로 꺼냅니다. 오류 처리에서는 `requestInfo.ResponseCode`, `ResponseText`, `GetResponseHeader()`로 상태 코드, 오류 본문, `Retry-After` 등을 읽을 수 있습니다. 이 값은 요청이 풀로 반환되기 전까지만 유효합니다.

## 오류 및 재시도 정책

| 결과 | 처리 |
| --- | --- |
| `ConnectionError`(시간 초과 포함) | `RetryOnConnectionError`가 true면 설정된 횟수만큼 재시도, 소진되거나 false면 `ErrorProcess(HttpErrorCodes.ConnectionFailed)` |
| HTTP 4xx·5xx | 재시도 없이 `ErrorProcess()`에 HTTP 상태 코드 전달(401·409 포함) |
| 응답의 `ErrCode != 0, 200` | `ErrorProcess()`에 ErrCode 전달 |
| `DataProcessingError`, 응답 변환 예외 | 재시도 없이 `HttpErrorCodes.InvalidResponse`(-1) 전달 |
| 204·205 | 본문 없는 성공(`Process()`에 기본값 응답) |
| 그 밖의 빈 2xx | `AllowEmptyResponse`가 false(기본)면 -1, true면 성공 |
| 매니저 종료·재초기화 | 남은 요청마다 `HttpErrorCodes.Canceled` 전달 후 반환 |
| HTTP 2xx | 성공 응답 파싱 |

HTTP 오류 응답은 연결 장애로 간주하여 재시도하지 않습니다. 본문이 JSON이 아닌 오류 응답(프록시의 HTML 오류 등)도 예외 없이 `ErrorProcess()`로 전달합니다. 재시도 중에도 같은 `RequestInfo`와 요청 본문을 유지하며 패킷 번호만 새로 발급합니다.

서버가 같은 요청을 두 번 처리하면 안 되는 API(멱등성이 보장되지 않는 저장 등)는 `RetryOnConnectionError`를 false로 두고, 응답을 받지 못한 경우의 확인 방법을 서버 계약에 맞춰 호출하는 쪽에서 처리합니다. 401 후 토큰 갱신과 한 번의 재전송 같은 인증 정책도 사용하는 프로젝트에서 구현합니다.

비밀값을 보내는 핸들러는 `LogRequestBody`를 false로 두거나 `HttpManagerOptions.LogRequestBodies`를 false로 설정합니다.

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

`Initialize()`를 다시 호출하면 기존 요청을 정리하고 새 설정으로 초기화합니다. `Dispose()` 또는 `OnDestroy()`가 호출되면 다음 작업을 수행합니다.

- 실행 중인 UnityWebRequest 중단
- 모든 요청 및 지연 코루틴 종료
- 대기·진행·재시도 중인 요청에 `ErrorProcess(HttpErrorCodes.Canceled)` 전달 후 RequestInfo 풀 반환
- 병합 요청과 핸들러 등록 정보 제거

매니저가 비활성화된 동안에는 코루틴을 시작할 수 없으므로 요청을 보내기 전에 활성 상태인지 확인해야 합니다.

## 샘플

Package Manager에서 **Basic Request** 샘플을 가져올 수 있습니다.

샘플에는 다음 내용이 포함됩니다.

- `HttpManagerOptions` 생성과 초기화
- 핸들러 어셈블리 등록
- 요청·응답 메시지
- 매개변수 기반 핸들러
- 성공 콜백과 오류 처리

샘플 기본 URL은 예시 값이므로 실제 서버 주소와 API 스키마로 교체해야 합니다.

## 현재 범위

현재 런타임은 다음 범위를 대상으로 합니다.

- JSON 요청(GET, POST, PUT, DELETE, PATCH)
- `JsonUtility` 직렬화(핸들러에서 교체 가능)
- 핸들러 기반 응답 처리와 요청별 호출자 상태
- 공통·요청별·전송 직전 헤더
- 연결 오류 재시도(핸들러별 끄기)
- 단순 동시 요청 제한과 요청 병합

다음 기능은 사용하는 프로젝트에서 확장해야 합니다.

- 인증 흐름(토큰 발급·갱신·401 재전송)
- 파일 업로드 및 다운로드
- 다른 JSON 직렬화 도구
- 지수 백오프와 상태 코드별 재시도 정책
- 이미 보낸 요청의 개별 취소와 진행률
- 캐시 및 오프라인 큐

## 테스트

`Tests/Runtime`에 루프백 HTTP 서버를 쓰는 PlayMode 테스트가 있습니다. 사용하는 프로젝트의 `Packages/manifest.json`에 다음을 추가하고 Test Runner의 PlayMode에서 실행합니다.

```json
"testables": ["com.causeless3t.httpbase"]
```

## 프로젝트 구조

```text
HttpBase/
├── Runtime/
│   ├── Causeless3t.HttpBase.asmdef
│   ├── HttpErrorCodes.cs
│   ├── HttpManager.cs
│   ├── HttpManagerOptions.cs
│   ├── IRequestHandler.cs
│   ├── RequestInfo.cs
│   └── RequestMethod.cs
├── Samples~/
│   └── BasicRequest/
├── Tests/
│   └── Runtime/
├── CHANGELOG.md
├── LICENSE
├── README.md
└── package.json
```

## 라이선스

이 프로젝트는 [MIT 라이선스](LICENSE)를 따릅니다.
