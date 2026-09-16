# CLAUDE.md

이 문서는 HttpBase 저장소에서 작업하는 AI 코딩 도구를 위한 프로젝트 지침이다. 변경 전 Runtime, Samples, package.json, README와 CHANGELOG를 읽고 과거 설계안보다 현재 코드를 우선한다.

## 프로젝트 목적

HttpBase는 Unity 프로젝트에서 Handler 기반 JSON HTTP 요청을 구성하는 경량 UPM 패키지다.

Runtime의 책임:

- HttpManager 설정과 수명 주기
- 요청 대기열과 동시 실행 제한
- UnityWebRequest 생성과 종료
- 연결 오류 재시도
- HTTP 및 응답 오류 분류
- API별 요청 병합
- Handler 등록과 조회
- RequestInfo 풀 반환 보장

사용자 프로젝트의 책임:

- API 요청·응답 모델
- 인증과 공통 Header
- 서버별 오류 정책
- GET·PUT·PATCH·DELETE 등 추가 Method
- 프로젝트 서비스 또는 Singleton 구성
- UI 및 도메인 로직

범용 HTTP 프레임워크로 확장하지 말고 현재 책임 안에서 가장 작은 변경을 선택한다.

## 기술 기준

- 최소 Unity 버전: 2022.3
- Runtime 네임스페이스: `Causeless3t.Network`
- Runtime 어셈블리: `Causeless3t.HttpBase`
- Unity 기본 API 외 외부 패키지 의존성 없음
- 비동기 실행 방식: Unity Coroutine
- HTTP 구현: UnityWebRequest
- JSON 구현: JsonUtility
- 패키지 버전: Semantic Versioning

UnityCore, UniTask 또는 다른 Singleton·비동기 패키지 의존성을 다시 추가하지 않는다.

## 저장소 구조

```text
HttpBase/
├── Runtime/
├── Samples~/
│   └── BasicRequest/
├── CHANGELOG.md
├── LICENSE
├── README.md
└── package.json
```

저장소 자체가 UPM 패키지다. `Assets`, `Packages`, `ProjectSettings`를 다시 만들거나 설치 경로에 `?path=Assets/Deploy`를 사용하지 않는다.

## 핵심 계약

### HttpManager

`HttpManager`는 Singleton이 아닌 독립적인 `MonoBehaviour`다.

- Singleton 여부는 사용자 프로젝트가 결정한다.
- 하나의 프로젝트에서 서로 다른 Base URL을 가진 Manager 여러 개를 사용할 수 있어야 한다.
- `Initialize()` 전에 요청이나 Handler 등록을 허용하지 않는다.
- 재초기화는 이전 요청과 등록 상태를 먼저 정리한다.
- `OnDestroy()`는 `Dispose()`를 호출한다.
- 비활성 GameObject에서는 Coroutine을 시작할 수 있음을 가정하지 않는다.

### HttpManagerOptions

설정은 생성 이후 변경할 수 없는 일반 C# 객체다.

- Base URL은 유효한 HTTP 또는 HTTPS 절대 URL이어야 한다.
- timeout과 동시 요청 수는 0보다 커야 한다.
- 재시도 횟수와 대기 시간은 0 이상이어야 한다.
- `MaxRetryAttempts`는 최초 시도를 제외한 추가 시도 횟수다.
- ScriptableObject, 환경변수, Remote Config 등 설정 공급 방식은 사용자 프로젝트가 결정한다.

### Handler 등록

자동 등록:

- 호출자가 `Initialize(options, typeof(Handler).Assembly)`처럼 대상 Assembly를 전달한다.
- 구체 클래스, `IRequestHandler` 구현, `[API]` 지정 타입만 등록한다.
- public 기본 생성자가 필요하다.
- `APIAttribute`는 `PreserveAttribute`를 상속해 IL2CPP 스트리핑을 방지한다.

수동 등록:

- 생성자 의존성이 있는 Handler는 `[API]`를 붙이지 않는다.
- `API` 프로퍼티를 override한다.
- `RegisterHandler(instance)`로 등록한다.

API 경로 중복이나 빈 경로를 조용히 무시하지 않는다.

### Handler 상태

`RequestHandler` 인스턴스에 요청별 메시지나 콜백을 저장하지 않는다.

다음 값은 요청마다 `RequestInfo`에 고정되어야 한다.

- Handler
- API 경로
- 패킷 번호
- JSON Body
- Callback
- 재시도 횟수

같은 Handler에서 요청이 연속으로 발생해도 메시지와 콜백이 서로 덮어쓰이지 않아야 한다.

### Coroutine과 종료

진행 상태는 다음 컬렉션으로 구분한다.

- 대기: `_requestWaitingQueue`
- 진행: `_inProgressRequests`
- 재시도 대기: `_retryingRequests`
- 활성 네트워크 요청: `_activeWebRequests`
- 병합 대기: `_pendingCollapsibleRequests`

`Dispose()` 순서:

1. 초기화 상태 해제
2. 활성 UnityWebRequest Abort
3. 모든 Coroutine 중단
4. 남은 UnityWebRequest Dispose
5. 대기·진행·재시도 RequestInfo 완료 처리
6. Handler와 설정 제거

Coroutine을 추가할 때 중단될 경우 보유 중인 RequestInfo와 UnityWebRequest가 유실되지 않는지 반드시 확인한다.

### RequestInfo 풀링

`RequestInfo`는 Handler Generic 타입별 ObjectPool에서 관리한다.

- 모든 최종 성공·실패·취소 경로는 `CompleteRequest()`에 도달해야 한다.
- 재시도 예정인 요청은 풀로 반환하지 않는다.
- 재시도 소진 시 반드시 반환한다.
- 반환 전 `Reset()`으로 Handler, Body와 Callback 참조를 제거한다.
- 완료 순서는 요청 시작 순서와 다를 수 있으므로 Queue의 선두를 임의로 제거하지 않는다.
- 풀로 반환한 객체를 이후 코드에서 참조하거나 수정하지 않는다.

### HTTP 결과

- `ConnectionError`만 자동 재시도한다.
- HTTP 4xx와 5xx는 `ErrorProcess()`에 상태 코드를 전달한다.
- 전체 2xx 범위를 성공으로 취급한다.
- `DataProcessingError`와 빈 성공 응답은 내부 오류 코드 `-1`로 처리한다.
- 응답의 `BaseResponse.ErrCode`가 0 또는 200이 아니면 `ErrorProcess()`를 호출한다.
- HTTP 프로토콜 오류를 연결 장애처럼 재시도하지 않는다.

오류 정책을 변경하면 README와 테스트 사례도 함께 변경한다.

### 요청 병합

공개 API의 기존 이름은 `ICollapsableRequest`다. 별도 major version 결정 없이 철자를 변경하지 않는다.

- 병합 상태와 지연 Coroutine은 API별로 독립적이어야 한다.
- `Equals()`가 true일 때만 `Collapse()`하고 Callback을 누적한다.
- 같은 API에서 Equals가 false이면 기존 그룹을 즉시 큐에 넣고 새 그룹을 시작한다.
- 한 API의 병합 변경이 다른 API의 지연 Coroutine을 중단해서는 안 된다.
- Dispose 시 모든 병합 Coroutine과 참조를 제거한다.

## 코드 변경 규칙

- 현재 규모에서 Core, Transport, Serialization 등 과도한 계층을 추가하지 않는다.
- 요청 없이 외부 패키지 의존성을 추가하지 않는다.
- Runtime에 프로젝트 전용 Singleton이나 인증 서비스를 직접 참조하지 않는다.
- public/protected API 변경은 호환성과 major version 영향을 검토한다.
- Unity가 추적하는 Runtime 파일을 추가·이동할 때 `.meta`를 함께 관리한다.
- Samples~의 package.json path와 실제 디렉터리를 항상 일치시킨다.
- 사용자가 보는 변경은 CHANGELOG에 기록한다.
- C# 문자열은 큰따옴표를 사용한다. 백틱을 C# 코드에 쓰지 않는다.
- 주석과 README는 검증된 동작만 설명한다.
- 자동화된 테스트가 없다면 컴파일 또는 런타임 검증 완료라고 주장하지 않는다.

## 현재 제한 사항

- 요청 Method는 JSON POST로 고정되어 있다.
- 공통 Header와 인증 주입 API가 없다.
- JsonUtility의 필드 기반 직렬화 제약을 가진다.
- 빈 2xx 응답을 실패로 취급하므로 204 응답 API에는 적합하지 않다.
- 요청별 취소 API가 없다.
- 자동화된 Runtime 테스트가 아직 없다.
- Debug 로그가 Runtime에 직접 포함되어 있다.

이 제한을 해결할 때 서로 관련 없는 기능을 한 변경에 묶지 않는다.

## 검증 체크리스트

코드 변경 후 가능한 범위에서 다음을 확인한다.

- Git URL로 루트 UPM 설치
- Unity 2022.3 컴파일
- Basic Request 샘플 Import와 컴파일
- 자동 Handler 어셈블리 등록
- 수동 Handler 인스턴스 등록
- 중복 및 빈 API 경로 예외
- IL2CPP 빌드에서 자동 Handler 보존
- JSON 요청 Body 생성
- 최대 동시 요청 수 준수
- 요청과 다른 순서로 응답 완료
- HTTP 2xx 성공 처리
- HTTP 4xx·5xx ErrorProcess 전달
- ConnectionError 재시도 횟수
- 재시도 소진 후 RequestInfo 반환
- 서로 다른 API 병합 대기의 독립성
- 동일 API 요청 Collapse와 Callback 누적
- Initialize 재호출 시 기존 요청 정리
- Dispose 및 OnDestroy 시 대기·진행·재시도 요청 반환
- 전체 Runtime에서 UnityCore, UniTask, Cysharp 참조 없음
- 전체 C# 파일에서 백틱 문자 없음

## 금지 사항

- HttpManager에 전역 static Instance를 추가하지 않는다.
- Runtime을 특정 프로젝트의 Singleton 기반 클래스에서 상속하지 않는다.
- UniTask 의존성을 다시 추가하지 않는다.
- Handler에 요청별 Message 또는 Callback 필드를 추가하지 않는다.
- 진행 요청을 완료 순서와 무관한 Queue.Dequeue로 제거하지 않는다.
- HTTP 4xx·5xx를 ConnectionError처럼 자동 재시도하지 않는다.
- 병합 요청에 모든 API가 공유하는 단일 취소 상태를 사용하지 않는다.
- RequestInfo를 반환하지 않는 종료 경로를 추가하지 않는다.
- 루트 UPM 구조를 Unity 테스트 프로젝트 구조로 되돌리지 않는다.
