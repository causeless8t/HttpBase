namespace Causeless3t.Network
{
    /// <summary>
    /// HTTP Method. Handlers that do not declare a method use <see cref="Post"/> (2.0.0 behavior).
    /// </summary>
    public enum RequestMethod
    {
        Post = 0,
        Get,
        Put,
        Delete,
        Patch,
    }

    public static class RequestMethodExtensions
    {
        public static string ToHttpMethod(this RequestMethod method)
        {
            switch (method)
            {
                case RequestMethod.Get:
                    return "GET";
                case RequestMethod.Put:
                    return "PUT";
                case RequestMethod.Delete:
                    return "DELETE";
                case RequestMethod.Patch:
                    return "PATCH";
                default:
                    return "POST";
            }
        }

        /// <summary>
        /// True when the method sends the serialized request as a JSON body.
        /// GET and DELETE requests are sent without a body.
        /// </summary>
        public static bool HasRequestBody(this RequestMethod method)
        {
            return method == RequestMethod.Post ||
                   method == RequestMethod.Put ||
                   method == RequestMethod.Patch;
        }
    }
}
