using System;
using System.Collections.Generic;

namespace Causeless3t.Network.Tests
{
    [Serializable]
    public sealed class EchoRequest : IRequest
    {
        public string value;
    }

    [Serializable]
    public sealed class EchoResponse : BaseResponse
    {
        public string value;
    }

    /// <summary>Per-request result sink passed as <see cref="RequestInfo.UserState"/>.</summary>
    public sealed class CallResult
    {
        public bool Done;
        public bool Succeeded;
        public int Error;
        public long ResponseCode;
        public string ResponseText;
        public int RetryCount;
        public EchoResponse Response;
        public int CompletionCount;
    }

    /// <summary>Handler whose results go to the caller through <see cref="RequestInfo.UserState"/>.</summary>
    public abstract class ResultHandler : RequestHandler<EchoRequest, EchoResponse, string>
    {
        protected override EchoRequest MakeReqMessage(string param) => new() { value = param };

        protected override void Process(RequestInfo requestInfo, EchoResponse response)
        {
            if (requestInfo.UserState is not CallResult result)
                return;

            result.Done = true;
            result.Succeeded = true;
            result.Response = response;
            result.ResponseCode = requestInfo.ResponseCode;
            result.RetryCount = requestInfo.RetryCount;
            result.CompletionCount++;
        }

        public override void ErrorProcess(RequestInfo requestInfo, BaseResponse response, int error)
        {
            if (requestInfo.UserState is not CallResult result)
                return;

            result.Done = true;
            result.Succeeded = false;
            result.Error = error;
            result.ResponseCode = requestInfo.ResponseCode;
            result.ResponseText = requestInfo.ResponseText;
            result.RetryCount = requestInfo.RetryCount;
            result.CompletionCount++;
        }
    }

    /// <summary>No method declared: must stay POST (2.0.0 behavior).</summary>
    [API("legacy/echo")]
    public sealed class LegacyPostHandler : ResultHandler
    {
    }

    [API("items", Method = RequestMethod.Get)]
    public sealed class GetItemsHandler : ResultHandler
    {
    }

    [API("items", Method = RequestMethod.Put)]
    public sealed class PutItemsHandler : ResultHandler
    {
    }

    [API("items", Method = RequestMethod.Delete)]
    public sealed class DeleteItemsHandler : ResultHandler
    {
    }

    [API("empty")]
    public sealed class NoContentHandler : ResultHandler
    {
    }

    [API("status")]
    public sealed class StatusHandler : ResultHandler
    {
    }

    [API("slow")]
    public sealed class SlowHandler : ResultHandler
    {
    }

    /// <summary>
    /// Manually registered handler with constructor dependencies: per-request header from the parameter
    /// and an Authorization header read at send time.
    /// </summary>
    public sealed class AuthorizedHandler : ResultHandler
    {
        private readonly Func<string> _token;
        private readonly Func<bool> _ready;

        public AuthorizedHandler(Func<string> token, Func<bool> ready)
        {
            _token = token;
            _ready = ready;
        }

        public override string API => "secure";
        public override RequestMethod Method => RequestMethod.Get;

        protected override void AddRequestHeaders(EchoRequest request, ref Dictionary<string, string> headers)
        {
            headers ??= new Dictionary<string, string>();
            headers["X-Request-Value"] = request.value;
        }

        protected override void ApplyHeaders(RequestInfo info, IDictionary<string, string> headers)
        {
            headers["Authorization"] = "Bearer " + _token();
        }

        protected override bool IsReadyToSend(RequestInfo info) => _ready();
    }

    /// <summary>Connection errors are not retried (unsafe to send twice).</summary>
    public sealed class NoRetryHandler : ResultHandler
    {
        public override string API => "no-retry";
        public override bool RetryOnConnectionError => false;
        public override bool LogRequestBody => false;
    }

    public sealed class RetryHandler : ResultHandler
    {
        public override string API => "retry";
    }
}
