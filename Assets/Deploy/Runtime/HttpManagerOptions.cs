using System;

namespace Causeless3t.Network
{
    public sealed class HttpManagerOptions
    {
        public string BaseUrl { get; }
        public int TimeoutSeconds { get; }
        public int MaxConcurrentRequests { get; }
        public int MaxRetryAttempts { get; }
        public float RetryDelaySeconds { get; }
        public float BundleDelaySeconds { get; }

        public HttpManagerOptions(
            string baseUrl,
            int timeoutSeconds = 15,
            int maxConcurrentRequests = 5,
            int maxRetryAttempts = 3,
            float retryDelaySeconds = 2f,
            float bundleDelaySeconds = 5f)
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
        }

        public string BuildUrl(string api)
        {
            if (string.IsNullOrWhiteSpace(api))
                throw new ArgumentException(
                    "API path cannot be empty.",
                    nameof(api));

            return $"{BaseUrl}/{api.TrimStart('/')}";
        }
    }
}
