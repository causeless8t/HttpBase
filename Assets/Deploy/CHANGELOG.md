# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.1] - 2025-02-12
### Modified
- IRequestHandler
  - Request를 다루는 Handler의 기본형
- HttpManager
  - 핸들러 처리의 수정
  - 중첩 메시지 처리 추가
  - 초기화 로직 수정
  - 커스텀 콜백 처리 추가
- RequestInfo
  - 커스텀 콜백 자료형 추가
### Added
- BaseMessage
  - Request/Response 메시지의 기본형
- SampleMessage
  - BaseMessage를 상속하는 샘플메시지

## [1.0.0] - 2023-09-27
### Added
- BaseRequestHandler
  - Request를 다루는 Handler의 기본형
- HttpManager
  - Request를 처리를 담당하는 Singleton 클래스
- RequestInfo
  - Request 하나의 정보를 담은 클래스