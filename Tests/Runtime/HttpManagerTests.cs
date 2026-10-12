using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Causeless3t.Network.Tests
{
    /// <summary>
    /// PlayMode tests for HttpManager against a loopback HTTP server (<see cref="LocalTestServer"/>).
    /// HttpManager runs on Update and coroutines, so these tests need Play Mode.
    /// </summary>
    public sealed class HttpManagerTests
    {
        private const float TimeoutSeconds = 10f;

        private LocalTestServer _server;
        private GameObject _gameObject;
        private HttpManager _manager;

        [SetUp]
        public void SetUp()
        {
            _server = new LocalTestServer();
            _gameObject = new GameObject("HttpManagerTests");
            _manager = _gameObject.AddComponent<HttpManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_gameObject != null)
                UnityEngine.Object.DestroyImmediate(_gameObject);

            _server.Dispose();
        }

        private void Initialize(string baseUrl = null, int maxRetryAttempts = 0, float retryDelaySeconds = 0f)
        {
            _manager.Initialize(
                new HttpManagerOptions(
                    baseUrl ?? _server.BaseUrl,
                    timeoutSeconds: 5,
                    maxRetryAttempts: maxRetryAttempts,
                    retryDelaySeconds: retryDelaySeconds),
                typeof(LegacyPostHandler).Assembly);
        }

        private static IEnumerator WaitFor(Func<bool> condition)
        {
            var deadline = Time.realtimeSinceStartup + TimeoutSeconds;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > deadline)
                    Assert.Fail("Timed out waiting for the request.");

                yield return null;
            }
        }

        private static CallResult Send<T>(HttpManager manager, string value) where T : ResultHandler
        {
            var result = new CallResult();
            manager.GetHandler<T>().EnqueueRequestWithState(value, result);
            return result;
        }

        [UnityTest]
        public IEnumerator LegacyHandler_SendsJsonPost_AndParsesResponse()
        {
            _server.Route("POST", "/legacy/echo", r => TestResponse.Json(200, "{\"value\":\"ok\"}"));
            Initialize();

            string raw = null;
            var result = new CallResult();
            _manager.GetHandler<LegacyPostHandler>().EnqueueRequestWithState("hello", result, (_, recv) => raw = recv);
            yield return WaitFor(() => result.Done);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Response.value, Is.EqualTo("ok"));
            Assert.That(raw, Is.EqualTo("{\"value\":\"ok\"}"));

            var request = _server.Requests[0];
            Assert.That(request.Method, Is.EqualTo("POST"));
            Assert.That(request.Body, Is.EqualTo("{\"value\":\"hello\"}"));
            Assert.That(request.Header("Content-Type"), Does.StartWith("application/json"));
            Assert.That(request.Header("Accept"), Is.EqualTo("application/json"));
        }

        [UnityTest]
        public IEnumerator LegacyEnqueueRequest_WithoutState_StillWorks()
        {
            _server.Route("POST", "/legacy/echo", TestResponse.Json(200, "{\"value\":\"ok\"}"));
            Initialize();

            string raw = null;
            _manager.GetHandler<LegacyPostHandler>().EnqueueRequest("hello", (_, recv) => raw = recv);
            yield return WaitFor(() => raw != null);

            Assert.That(raw, Is.EqualTo("{\"value\":\"ok\"}"));
        }

        [UnityTest]
        public IEnumerator GetPutDelete_UseTheirMethods_AndOnlyPutSendsBody()
        {
            _server.Route("GET", "/items", TestResponse.Json(200, "{\"value\":\"get\"}"));
            _server.Route("PUT", "/items", TestResponse.Json(200, "{\"value\":\"put\"}"));
            _server.Route("DELETE", "/items", TestResponse.Json(200, "{\"value\":\"delete\"}"));
            Initialize();

            var get = Send<GetItemsHandler>(_manager, "a");
            var put = Send<PutItemsHandler>(_manager, "b");
            var delete = Send<DeleteItemsHandler>(_manager, "c");
            yield return WaitFor(() => get.Done && put.Done && delete.Done);

            Assert.That(get.Response.value, Is.EqualTo("get"));
            Assert.That(put.Response.value, Is.EqualTo("put"));
            Assert.That(delete.Response.value, Is.EqualTo("delete"));

            foreach (var request in _server.Requests)
            {
                if (request.Method == "PUT")
                {
                    Assert.That(request.Body, Is.EqualTo("{\"value\":\"b\"}"));
                    Assert.That(request.Header("Content-Type"), Does.StartWith("application/json"));
                }
                else
                {
                    Assert.That(request.Body, Is.Empty, request.Method);
                }
            }
        }

        [UnityTest]
        public IEnumerator SamePathWithDifferentMethods_AreRegisteredSeparately()
        {
            Initialize();
            yield return null;

            Assert.That(_manager.GetHandler("items", RequestMethod.Get), Is.InstanceOf<GetItemsHandler>());
            Assert.That(_manager.GetHandler("items", RequestMethod.Put), Is.InstanceOf<PutItemsHandler>());
            Assert.That(_manager.GetHandler("legacy/echo"), Is.InstanceOf<LegacyPostHandler>());
            Assert.Throws<InvalidOperationException>(() => _manager.RegisterHandler(new GetItemsHandler()));
        }

        [UnityTest]
        public IEnumerator ManualHandler_SendsSendTimeAuthorization_AndPerRequestHeaders()
        {
            _server.Route("GET", "/secure", TestResponse.Json(200, "{\"value\":\"ok\"}"));
            Initialize();

            var token = "first";
            var ready = true;
            _manager.RegisterHandler(new AuthorizedHandler(() => token, () => ready));

            var first = new CallResult();
            var second = new CallResult();
            var handler = _manager.GetHandler<AuthorizedHandler>();
            handler.EnqueueRequestWithState("one", first);
            handler.EnqueueRequestWithState("two", second);
            // The token changes after the requests were created and before they are sent.
            token = "second";
            yield return WaitFor(() => first.Done && second.Done);

            Assert.That(_server.Requests.Count, Is.EqualTo(2));
            var values = new System.Collections.Generic.HashSet<string>();
            foreach (var request in _server.Requests)
            {
                Assert.That(request.Header("Authorization"), Is.EqualTo("Bearer second"));
                values.Add(request.Header("X-Request-Value"));
            }

            Assert.That(values, Is.EquivalentTo(new[] { "one", "two" }));
        }

        [UnityTest]
        public IEnumerator NotReadyHandler_KeepsRequestWaiting_WithoutUsingRetries()
        {
            _server.Route("GET", "/secure", TestResponse.Json(200, "{\"value\":\"ok\"}"));
            Initialize(maxRetryAttempts: 0);

            var ready = false;
            _manager.RegisterHandler(new AuthorizedHandler(() => "token", () => ready));
            var result = new CallResult();
            _manager.GetHandler<AuthorizedHandler>().EnqueueRequestWithState("one", result);

            for (var i = 0; i < 10; i++)
                yield return null;

            Assert.That(result.Done, Is.False);
            Assert.That(_server.Requests.Count, Is.EqualTo(0));
            Assert.That(_manager.PendingRequestCount, Is.EqualTo(1));

            ready = true;
            yield return WaitFor(() => result.Done);
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.RetryCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator NoContent204_IsSuccess()
        {
            _server.Route("POST", "/empty", TestResponse.Json(204, string.Empty));
            Initialize();

            var result = Send<NoContentHandler>(_manager, "x");
            yield return WaitFor(() => result.Done);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.ResponseCode, Is.EqualTo(204));
            Assert.That(result.Response, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator EmptyOkBody_StaysAnInvalidResponse()
        {
            _server.Route("POST", "/empty", TestResponse.Json(200, string.Empty));
            Initialize();

            var result = Send<NoContentHandler>(_manager, "x");
            LogAssert.Expect(LogType.Error, "empty returned an empty response.");
            yield return WaitFor(() => result.Done);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Error, Is.EqualTo(HttpErrorCodes.InvalidResponse));
        }

        [UnityTest]
        public IEnumerator Status401And409_GoToErrorProcess_WithBody_AndAreNotRetried()
        {
            var status = 401;
            _server.Route("POST", "/status", _ => TestResponse.Json(status, $"{{\"status\":{status},\"code\":\"c{status}\"}}"));
            Initialize(maxRetryAttempts: 3);

            var unauthorized = Send<StatusHandler>(_manager, "x");
            yield return WaitFor(() => unauthorized.Done);
            status = 409;
            var conflict = Send<StatusHandler>(_manager, "y");
            yield return WaitFor(() => conflict.Done);

            Assert.That(unauthorized.Error, Is.EqualTo(401));
            Assert.That(unauthorized.ResponseText, Does.Contain("c401"));
            Assert.That(conflict.Error, Is.EqualTo(409));
            Assert.That(conflict.ResponseText, Does.Contain("c409"));
            Assert.That(_server.CountRequests("POST", "/status"), Is.EqualTo(2));
            Assert.That(unauthorized.CompletionCount, Is.EqualTo(1));
            Assert.That(conflict.CompletionCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator NonJsonErrorBody_GoesToErrorProcess()
        {
            _server.Route("POST", "/status", new TestResponse { Status = 502, Body = "<html>Bad Gateway</html>", ContentType = "text/html" });
            Initialize();

            var result = Send<StatusHandler>(_manager, "x");
            yield return WaitFor(() => result.Done);

            Assert.That(result.Error, Is.EqualTo(502));
            Assert.That(result.ResponseText, Does.Contain("Bad Gateway"));
        }

        [UnityTest]
        public IEnumerator ConnectionError_IsRetried_ThenReportedOnce()
        {
            Initialize(LocalTestServer.UnusedBaseUrl(), maxRetryAttempts: 2, retryDelaySeconds: 0f);
            _manager.RegisterHandler(new RetryHandler());

            LogAssert.ignoreFailingMessages = true;
            var result = Send<RetryHandler>(_manager, "x");
            yield return WaitFor(() => result.Done);
            LogAssert.ignoreFailingMessages = false;

            Assert.That(result.Error, Is.EqualTo(HttpErrorCodes.ConnectionFailed));
            Assert.That(result.RetryCount, Is.EqualTo(2));
            Assert.That(result.CompletionCount, Is.EqualTo(1));
            Assert.That(_manager.PendingRequestCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator ConnectionError_NotRetried_WhenHandlerOptsOut()
        {
            Initialize(LocalTestServer.UnusedBaseUrl(), maxRetryAttempts: 3, retryDelaySeconds: 0f);
            _manager.RegisterHandler(new NoRetryHandler());

            LogAssert.ignoreFailingMessages = true;
            var result = Send<NoRetryHandler>(_manager, "x");
            yield return WaitFor(() => result.Done);
            LogAssert.ignoreFailingMessages = false;

            Assert.That(result.Error, Is.EqualTo(HttpErrorCodes.ConnectionFailed));
            Assert.That(result.RetryCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator ResponsesOutOfOrder_ReachTheirOwnCallers()
        {
            _server.Route("POST", "/slow", r => TestResponse.Json(
                200,
                r.Body.Contains("first") ? "{\"value\":\"first\"}" : "{\"value\":\"second\"}",
                r.Body.Contains("first") ? 800 : 0));
            Initialize();

            var first = Send<SlowHandler>(_manager, "first");
            var second = Send<SlowHandler>(_manager, "second");
            yield return WaitFor(() => second.Done);
            Assert.That(first.Done, Is.False, "The slow request should still be running.");
            yield return WaitFor(() => first.Done);

            Assert.That(first.Response.value, Is.EqualTo("first"));
            Assert.That(second.Response.value, Is.EqualTo("second"));
        }

        [UnityTest]
        public IEnumerator Dispose_CancelsWaitingAndActiveRequests()
        {
            _server.Route("POST", "/slow", TestResponse.Json(200, "{\"value\":\"late\"}", 2000));
            Initialize();

            var active = Send<SlowHandler>(_manager, "a");
            yield return null;
            yield return null;
            var waiting = Send<SlowHandler>(_manager, "b");

            _manager.Dispose();

            Assert.That(active.Done && waiting.Done, Is.True);
            Assert.That(active.Error, Is.EqualTo(HttpErrorCodes.Canceled));
            Assert.That(waiting.Error, Is.EqualTo(HttpErrorCodes.Canceled));
            Assert.That(_manager.PendingRequestCount, Is.EqualTo(0));
            Assert.That(_manager.IsInitialized, Is.False);
            Assert.That(_manager.GetHandler<SlowHandler>(), Is.Null);
        }

        [UnityTest]
        public IEnumerator EnqueueBeforeInitialize_Throws()
        {
            yield return null;
            Assert.Throws<InvalidOperationException>(() => _manager.RegisterHandler(new RetryHandler()));
        }
    }
}
