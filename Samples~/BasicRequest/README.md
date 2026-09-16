# Basic Request Sample

This sample demonstrates assembly-based Handler registration and a simple JSON request.

1. Import the sample from Package Manager.
2. Add `HttpManager` and `HttpRequest` to a GameObject.
3. Assign the `HttpManager` component and API base URL on `HttpRequest`.
4. Call `HttpRequest.Initialize()`.
5. Call `HttpRequest.TestAPI(int)` after initialization.

The sample URL is a placeholder. Replace it with a server that implements the `sample/test` endpoint and the sample request/response schema.
