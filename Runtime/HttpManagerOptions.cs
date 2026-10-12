using System;
using System.Collections.Generic;

namespace Causeless3t.Network
{
    public sealed class HttpManagerOptions
    {
        private static readonly IReadOnlyDictionary<string, string> NoHeaders =
            new Dictionary<string, string>();

        public string BaseUrl { get; }
        public int TimeoutSeconds { get; }
        public int MaxConcurrentRequests { get; }
        public int MaxRetryAttempts { get; }
        public float RetryDelaySeconds { get; }
        public float BundleDelaySeconds { get; }

        /// <summary>
        /// Headers applied to every request before per-request headers and
        /// <see cref="IRequestPolicy.ApplyHeaders"/>. Copied when the options are created.
        /// </summary>
        public IReadOnlyDictionary<string, string> DefaultHeaders { get; }

        /// <summary>
        /// Whether request bodies may be logged at all. Handlers can also opt out with
        /// <see cref="IRequestPolicy.LogRequestBody"/>. Headers are never logged.
        /// </summary>
        public bool LogRequestBodies { get; }

        public HttpManagerOptions(
            string baseUrl,
            int timeoutSeconds = 15,
            int maxConcurrentRequests = 5,
            int maxRetryAttempts = 3,
            float retryDelaySeconds = 2f,
            float bundleDelaySeconds = 5f,
            IReadOnlyDictionary<string, string> defaultHeaders = null,
            bool logRequestBodies = true)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new ArgumentException(
                    "Base URL cannot be empty.",
                    nameof(baseUrl));

            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
                throw new ArgumentException(
                    "Base URL must be an absolute URL.",
                    nameof(baseUrl));

            if (uri.Scheme != Uri.UriSchemeHttp &&
                uri.Scheme != Uri.UriSchemeHttps)
            {
                throw new ArgumentException(
                    "Base URL must use HTTP or HTTPS.",
                    nameof(baseUrl));
            }

            if (timeoutSeconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(timeoutSeconds));

            if (maxConcurrentRequests <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxConcurrentRequests));
            }

            if (maxRetryAttempts < 0)
                throw new ArgumentOutOfRangeException(nameof(maxRetryAttempts));

            if (retryDelaySeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(retryDelaySeconds));

            if (bundleDelaySeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(bundleDelaySeconds));
            }

            BaseUrl = baseUrl.TrimEnd('/');
            TimeoutSeconds = timeoutSeconds;
            MaxConcurrentRequests = maxConcurrentRequests;
            MaxRetryAttempts = maxRetryAttempts;
            RetryDelaySeconds = retryDelaySeconds;
            BundleDelaySeconds = bundleDelaySeconds;
            DefaultHeaders = CopyHeaders(defaultHeaders);
            LogRequestBodies = logRequestBodies;
        }

        /// <summary>True when the base URL uses HTTPS.</summary>
        public bool IsHttps =>
            BaseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

        public string BuildUrl(string api)
        {
            if (string.IsNullOrWhiteSpace(api))
                throw new ArgumentException(
                    "API path cannot be empty.",
                    nameof(api));

            return $"{BaseUrl}/{api.TrimStart('/')}";
        }

        private static IReadOnlyDictionary<string, string> CopyHeaders(
            IReadOnlyDictionary<string, string> headers)
        {
            if (headers == null || headers.Count == 0)
                return NoHeaders;

            var copy = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in headers)
            {
                RequestInfo.ValidateHeader(header.Key, header.Value);
                copy[header.Key] = header.Value;
            }

            return copy;
        }
    }
}
