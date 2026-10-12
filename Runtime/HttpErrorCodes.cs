namespace Causeless3t.Network
{
    /// <summary>
    /// Error codes passed to <see cref="IRequestHandler.ErrorProcess"/> when there is no HTTP status code.
    /// HTTP 4xx and 5xx responses pass the HTTP status code itself.
    /// </summary>
    public static class HttpErrorCodes
    {
        /// <summary>The response could not be read or parsed, or a 2xx response had an unexpected empty body.</summary>
        public const int InvalidResponse = -1;

        /// <summary>The request failed with a connection error (including timeouts) and was not retried or ran out of retries.</summary>
        public const int ConnectionFailed = -2;

        /// <summary>The manager was disposed or re-initialized before the request completed.</summary>
        public const int Canceled = -3;
    }
}
