# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.0] - 2026-09-16

### Changed

- Moved the repository to a root Unity Package Manager structure.
- Replaced the UnityCore Singleton dependency with an independent MonoBehaviour.
- Replaced UniTask with Unity coroutines.
- Added immutable HttpManagerOptions configuration.
- Changed Handler discovery to explicit assembly registration.
- Added manual Handler registration with Manager binding.
- Isolated request message and callback state from Handler instances.
- Changed in-progress request tracking from Queue to HashSet.
- Centralized request completion and pool release.
- Distinguished connection, HTTP protocol, and response processing errors.
- Added Manager lifetime cancellation and deterministic request cleanup.
- Isolated collapsible request state and delay handling per API.
- Updated retry semantics so MaxRetryAttempts excludes the initial request.

### Fixed

- Fixed callbacks being shared or overwritten between requests.
- Fixed incorrect request removal when responses complete out of order.
- Fixed RequestInfo not being released after retry exhaustion.
- Fixed HTTP 4xx and 5xx responses being retried as connection failures.
- Fixed null dereferencing when a response Handler was unavailable.
- Fixed one API canceling another API's collapsible request delay.

### Removed

- Removed the UnityCore dependency.
- Removed the UniTask dependency.
- Removed the embedded Unity test project structure.

## [1.0.1] - 2025-02-12

### Changed

- Updated IRequestHandler request processing.
- Updated HttpManager Handler processing and initialization.
- Added collapsible request processing.
- Added custom response callbacks.

### Added

- Added BaseMessage and SampleMessage examples.

## [1.0.0] - 2023-09-27

### Added

- Added the initial RequestHandler base implementation.
- Added the Singleton-based HttpManager.
- Added RequestInfo.
