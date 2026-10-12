using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Causeless3t.Network.Tests
{
    /// <summary>Request seen by <see cref="LocalTestServer"/>.</summary>
    public sealed class RecordedRequest
    {
        public string Method;
        public string Path;
        public string Body;
        public Dictionary<string, string> Headers = new(StringComparer.OrdinalIgnoreCase);

        public string Header(string name) => Headers.TryGetValue(name, out var value) ? value : null;
    }

    /// <summary>Response a route returns.</summary>
    public sealed class TestResponse
    {
        public int Status = 200;
        public string Body = "{}";
        public int DelayMilliseconds;
        public string ContentType = "application/json";

        public static TestResponse Json(int status, string body, int delayMilliseconds = 0) =>
            new() { Status = status, Body = body, DelayMilliseconds = delayMilliseconds };
    }

    /// <summary>
    /// Minimal HTTP server on the loopback interface for HttpManager tests. Routes are "METHOD /path".
    /// Requests are handled on thread-pool threads so delayed responses can finish out of order.
    /// </summary>
    public sealed class LocalTestServer : IDisposable
    {
        private readonly HttpListener _listener = new();
        private readonly ConcurrentDictionary<string, Func<RecordedRequest, TestResponse>> _routes = new();
        private readonly ConcurrentQueue<RecordedRequest> _requests = new();
        private readonly Thread _thread;
        private volatile bool _running = true;

        public LocalTestServer()
        {
            Port = FindFreePort();
            BaseUrl = $"http://127.0.0.1:{Port}";
            _listener.Prefixes.Add(BaseUrl + "/");
            _listener.Start();
            _thread = new Thread(Loop) { IsBackground = true, Name = "HttpBase test server" };
            _thread.Start();
        }

        public int Port { get; }
        public string BaseUrl { get; }

        public IReadOnlyList<RecordedRequest> Requests => _requests.ToArray();

        public void Route(string method, string path, Func<RecordedRequest, TestResponse> responder)
        {
            _routes[$"{method} {path}"] = responder;
        }

        public void Route(string method, string path, TestResponse response)
        {
            Route(method, path, _ => response);
        }

        public int CountRequests(string method, string path)
        {
            var count = 0;
            foreach (var request in _requests)
            {
                if (request.Method == method && request.Path == path)
                    count++;
            }

            return count;
        }

        /// <summary>A loopback URL where nothing listens (connection refused).</summary>
        public static string UnusedBaseUrl() => $"http://127.0.0.1:{FindFreePort()}";

        private static int FindFreePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private void Loop()
        {
            while (_running)
            {
                HttpListenerContext context;
                try
                {
                    context = _listener.GetContext();
                }
                catch
                {
                    return;
                }

                ThreadPool.QueueUserWorkItem(_ => Handle(context));
            }
        }

        private void Handle(HttpListenerContext context)
        {
            try
            {
                var request = context.Request;
                var recorded = new RecordedRequest
                {
                    Method = request.HttpMethod,
                    Path = request.Url.AbsolutePath,
                };

                foreach (string key in request.Headers.AllKeys)
                    recorded.Headers[key] = request.Headers[key];

                using (var reader = new StreamReader(request.InputStream, Encoding.UTF8))
                    recorded.Body = reader.ReadToEnd();

                _requests.Enqueue(recorded);

                var response = _routes.TryGetValue($"{recorded.Method} {recorded.Path}", out var responder)
                    ? responder(recorded)
                    : TestResponse.Json(404, "{\"code\":\"not_found\"}");

                if (response.DelayMilliseconds > 0)
                    Thread.Sleep(response.DelayMilliseconds);

                context.Response.StatusCode = response.Status;

                if (response.Status == 204 || string.IsNullOrEmpty(response.Body))
                {
                    context.Response.ContentLength64 = 0;
                }
                else
                {
                    var bytes = Encoding.UTF8.GetBytes(response.Body);
                    context.Response.ContentType = response.ContentType;
                    context.Response.ContentLength64 = bytes.Length;
                    context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                }

                context.Response.OutputStream.Close();
            }
            catch
            {
                // The client may have aborted the request.
            }
        }

        public void Dispose()
        {
            _running = false;

            try
            {
                _listener.Stop();
                _listener.Close();
            }
            catch
            {
                // Already closed.
            }
        }
    }
}
