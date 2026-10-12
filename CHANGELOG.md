# 변경 이력

이 프로젝트의 주요 변경 사항을 이 문서에 기록합니다.

문서 형식은 [변경 이력 유지하기](https://keepachangelog.com/ko/1.0.0/)를 따르며,
버전은 [유의적 버전](https://semver.org/lang/ko/) 규칙을 따릅니다.

## [2.1.0] - 2026-10-12

### 추가

- `RequestMethod`(GET, POST, PUT, DELETE, PATCH)와 `[API(..., Method = ...)]`, `RequestHandler.Method` override를 추가했습니다. Method를 지정하지 않은 핸들러는 기존과 같이 POST입니다.
- 등록 키를 Method와 경로로 바꿔 같은 경로의 GET·PUT 핸들러를 함께 등록할 수 있게 했습니다. `GetHandler(api, method)`를 추가했습니다.
- 공통 헤더(`HttpManagerOptions.DefaultHeaders`), 요청 생성 시 헤더(`AddRequestHeaders`), 전송 직전 헤더(`ApplyHeaders`, 재시도마다 다시 적용)를 추가했습니다.
- `IRequestPolicy`(Method, `RetryOnConnectionError`, `LogRequestBody`, `IsReadyToSend`, `ApplyHeaders`, `AllowEmptyResponse`)를 추가했습니다. `RequestHandler`가 기본값으로 구현합니다.
- `IsReadyToSend()`가 false인 요청은 재시도 횟수를 쓰지 않고 대기열에 남습니다(토큰 갱신 중 대기 등).
- 요청별 호출자 상태 `RequestInfo.UserState`와 `EnqueueRequestWithState()`를 추가했습니다.
- 오류 처리에서 읽을 수 있는 `RequestInfo.ResponseCode`, `ResponseText`, `Error`, `GetResponseHeader()`를 추가했습니다.
- 요청 본문·응답 변환 확장 지점 `SerializeRequest()`, `DeserializeResponse()`를 추가했습니다.
- `HttpErrorCodes`(InvalidResponse -1, ConnectionFailed -2, Canceled -3)를 추가했습니다.
- `HttpManager.IsInitialized`, `Options`, `PendingRequestCount`를 추가했습니다.
- `HttpManagerOptions.LogRequestBodies`로 요청 본문 로그를 끌 수 있게 했습니다.
- 루프백 HTTP 서버를 쓰는 PlayMode 테스트(`Tests/Runtime`)를 추가했습니다.

### 변경

- 요청에 `Accept: application/json`, 본문이 있는 요청에 `Content-Type: application/json`을 보냅니다(2.0.0은 JSON 본문을 폼 Content-Type으로 보냈습니다).
- 204·205 응답은 본문 없는 성공으로 처리합니다. 그 밖의 빈 2xx 응답은 기존과 같이 -1입니다(`AllowEmptyResponse`로 허용 가능).
- 연결 오류 재시도를 모두 소진하면 `ErrorProcess(HttpErrorCodes.ConnectionFailed)`를 호출합니다(2.0.0은 호출하지 않았습니다).
- `Dispose()`·재초기화 때 남은 요청마다 `ErrorProcess(HttpErrorCodes.Canceled)`를 호출한 뒤 반환합니다.
- 응답 변환이나 `Process()`에서 예외가 나면 `ErrorProcess(-1)`을 호출합니다.
- 요청 본문 로그를 요청 생성 시점에서 전송 시점으로 옮기고 Method를 함께 표시합니다. 헤더는 로그에 남기지 않습니다.

### 수정

- 본문이 JSON이 아닌 응답(프록시의 HTML 오류 등)에서 `BaseResponse` 파싱 예외로 `ErrorProcess()`가 호출되지 않던 문제를 수정했습니다.
- 동시 요청 수 제한이 같은 프레임에 시작한 요청을 세지 않던 문제를 수정했습니다.

## [2.0.0] - 2026-09-16

### 변경

- 저장소를 루트 Unity Package Manager 패키지 구조로 변경했습니다.
- UnityCore의 싱글턴 의존성을 제거하고 독립적인 `MonoBehaviour`로 변경했습니다.
- UniTask를 Unity 코루틴으로 교체했습니다.
- 생성 후 변경할 수 없는 `HttpManagerOptions` 설정 객체를 추가했습니다.
- 핸들러 탐색 방식을 명시적인 어셈블리 등록 방식으로 변경했습니다.
- 매니저에 연결되는 핸들러 인스턴스 수동 등록 기능을 추가했습니다.
- 요청 메시지와 콜백 상태를 핸들러 인스턴스로부터 분리했습니다.
- 진행 중인 요청 추적 자료구조를 `Queue`에서 `HashSet`으로 변경했습니다.
- 요청 완료 처리와 오브젝트 풀 반환 절차를 한곳으로 통합했습니다.
- 연결 오류, HTTP 오류 응답, 응답 데이터 처리 오류를 구분하도록 변경했습니다.
- 매니저 수명 종료에 따른 취소와 확정적인 요청 정리 절차를 추가했습니다.
- API별로 병합 요청 상태와 지연 처리를 독립적으로 관리하도록 변경했습니다.
- `MaxRetryAttempts`가 최초 요청을 제외한 재시도 횟수를 의미하도록 명확히 했습니다.

### 수정

- 여러 요청의 콜백이 공유되거나 덮어써지는 문제를 수정했습니다.
- 응답 순서가 요청 순서와 다를 때 잘못된 요청이 제거되는 문제를 수정했습니다.
- 재시도 횟수를 모두 소진한 뒤 `RequestInfo`가 반환되지 않는 문제를 수정했습니다.
- HTTP 4xx 및 5xx 응답을 연결 오류로 간주하여 재시도하던 문제를 수정했습니다.
- 응답 핸들러가 없을 때 null 참조가 발생하는 문제를 수정했습니다.
- 한 API의 병합 요청이 다른 API의 지연 전송을 취소하는 문제를 수정했습니다.

### 제거

- UnityCore 의존성을 제거했습니다.
- UniTask 의존성을 제거했습니다.
- 저장소에 포함되어 있던 Unity 테스트 프로젝트 구조를 제거했습니다.

## [1.0.1] - 2025-02-12

### 변경

- `IRequestHandler`의 요청 처리 방식을 개선했습니다.
- `HttpManager`의 핸들러 처리 및 초기화 방식을 개선했습니다.
- 병합 요청 처리 기능을 추가했습니다.
- 사용자 정의 응답 콜백을 추가했습니다.

### 추가

- `BaseMessage`와 `SampleMessage` 예제를 추가했습니다.

## [1.0.0] - 2023-09-27

### 추가

- 최초 `RequestHandler` 기반 구현을 추가했습니다.
- 싱글턴 기반 `HttpManager`를 추가했습니다.
- `RequestInfo`를 추가했습니다.
