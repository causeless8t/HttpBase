using System;
using System.Collections.Generic;

namespace Causeless3t.Network
{
    public sealed class RequestInfo
    {
        public enum eRequestState
        {
            Ready = 0,
            InProgress,
            Retrying,
        }

        private static readonly IReadOnlyDictionary<string, string> EmptyHeaders =
            new Dictionary<string, string>();

        private static readonly char[] InvalidHeaderNameCharacters = { '\r', '\n', ':' };
        private static readonly char[] InvalidHeaderValueCharacters = { '\r', '\n' };

        private Dictionary<string, string> _headers;
        private Dictionary<string, string> _responseHeaders;

        public eRequestState State { get; set; }
        internal IRequestHandler Handler { get; private set; }
        public string Protocol { get; private set; }
        public int PacketNumber { get; private set; }
        public string Body { get; private set; }
        public int RetryCount { get; private set; }

        public Action<RequestInfo, string> CustomCallback { get; private set; }

        /// <summary>HTTP Method fixed when the request was created. POST unless the handler declares another method.</summary>
        public RequestMethod Method { get; private set; }

        /// <summary>
        /// Per-request headers fixed when the request was created. Headers that must be
        /// computed at send time (for example Authorization) are applied by
        /// <see cref="IRequestPolicy.ApplyHeaders"/> instead.
        /// </summary>
        public IReadOnlyDictionary<string, string> Headers =>
            _headers != null ? _headers : EmptyHeaders;

        /// <summary>Caller state passed with the request (for example a completion source). Never serialized or logged.</summary>
        public object UserState { get; private set; }

        /// <summary>HTTP status code of the last response. 0 when no response was received.</summary>
        public long ResponseCode { get; private set; }

        /// <summary>Body of the last response. Empty when no body was received.</summary>
        public string ResponseText { get; private set; } = string.Empty;

        /// <summary>Transport or processing error message of the last attempt. Empty when none.</summary>
        public string Error { get; private set; } = string.Empty;

        public void SetInfo(
            IRequestHandler handler,
            string protocol,
            int packetNum,
            string body,
            Action<RequestInfo, string> callback = null)
        {
            Handler = handler ?? throw new ArgumentNullException(nameof(handler));
            State = eRequestState.Ready;
            Protocol = protocol;
            PacketNumber = packetNum;
            Body = body;
            RetryCount = 0;
            CustomCallback = callback;
            Method = RequestMethod.Post;
            _headers?.Clear();
            UserState = null;
            ClearResponse();
        }

        /// <summary>Sets the method and per-request headers. Handlers call this while creating the request.</summary>
        public void SetRequestOptions(
            RequestMethod method,
            IEnumerable<KeyValuePair<string, string>> headers)
        {
            Method = method;
            _headers?.Clear();

            if (headers == null)
                return;

            foreach (var header in headers)
            {
                ValidateHeader(header.Key, header.Value);
                _headers ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                _headers[header.Key] = header.Value;
            }
        }

        internal void SetUserState(object userState)
        {
            UserState = userState;
        }

        internal void SetResponse(
            long responseCode,
            string responseText,
            Dictionary<string, string> responseHeaders,
            string error)
        {
            ResponseCode = responseCode;
            ResponseText = responseText ?? string.Empty;
            Error = error ?? string.Empty;
            _responseHeaders?.Clear();

            if (responseHeaders == null)
                return;

            _responseHeaders ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in responseHeaders)
                _responseHeaders[header.Key] = header.Value;
        }

        /// <summary>Returns a header of the last response (case-insensitive), or null.</summary>
        public string GetResponseHeader(string name)
        {
            if (_responseHeaders == null || string.IsNullOrEmpty(name))
                return null;

            return _responseHeaders.TryGetValue(name, out var value) ? value : null;
        }

        public void Reset()
        {
            Handler = null;
            State = eRequestState.Ready;
            Protocol = null;
            PacketNumber = 0;
            Body = null;
            RetryCount = 0;
            CustomCallback = null;
            Method = RequestMethod.Post;
            _headers?.Clear();
            UserState = null;
            ClearResponse();
        }

        public bool TryPrepareRetry(
            int packetNum,
            int maxRetryAttempts)
        {
            if (maxRetryAttempts < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxRetryAttempts));
            }

            if (RetryCount >= maxRetryAttempts)
                return false;

            RetryCount++;
            PacketNumber = packetNum;
            State = eRequestState.Retrying;
            ClearResponse();
            return true;
        }

        internal static void ValidateHeader(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Header name cannot be empty.", nameof(name));

            if (value == null)
                throw new ArgumentNullException(nameof(value), $"Header '{name}' has a null value.");

            if (name.IndexOfAny(InvalidHeaderNameCharacters) >= 0 ||
                value.IndexOfAny(InvalidHeaderValueCharacters) >= 0)
            {
                throw new ArgumentException($"Header '{name}' contains invalid characters.");
            }
        }

        private void ClearResponse()
        {
            ResponseCode = 0;
            ResponseText = string.Empty;
            Error = string.Empty;
            _responseHeaders?.Clear();
        }
    }
}
