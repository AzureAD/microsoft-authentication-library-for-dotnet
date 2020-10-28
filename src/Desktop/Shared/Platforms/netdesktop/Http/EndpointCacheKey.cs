#if DESKTOP

using System;
using System.Collections.Generic;

namespace Microsoft.Identity.Client.Platforms.net45.Http
{
    /// <summary>
    /// Based on https://github.com/NimaAra/Easy.Common/blob/master/Easy.Common/RestClient.cs
    /// </summary>
    internal struct EndpointCacheKey : IEquatable<EndpointCacheKey>
    {
        private readonly Uri _uri;

        public EndpointCacheKey(Uri uri) => _uri = uri;

        public bool Equals(EndpointCacheKey other)
        {
            return string.Equals(_uri.Host, other._uri.Host, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(_uri.Scheme, other._uri.Scheme, StringComparison.OrdinalIgnoreCase) &&
                _uri.Port == other._uri.Port;
        }

        public override bool Equals(object obj) => obj is EndpointCacheKey other && Equals(other);

        public override int GetHashCode()
        {
            // don't need the whole URI, only scheme, port and host are important
            var hashCode = -1793347351;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(_uri.Scheme);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(_uri.Host);
            hashCode = hashCode * -1521134295 + EqualityComparer<int>.Default.GetHashCode(_uri.Port);

            return hashCode;
        }

        public static bool operator ==(EndpointCacheKey left, EndpointCacheKey right) => left.Equals(right);

        public static bool operator !=(EndpointCacheKey left, EndpointCacheKey right) => !left.Equals(right);
    }
}
#endif
