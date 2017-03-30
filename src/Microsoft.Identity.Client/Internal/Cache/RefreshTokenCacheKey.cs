using System.Text;

namespace Microsoft.Identity.Client.Internal.Cache
{
    class RefreshTokenCacheKey : TokenCacheKeyBase
    {

        public RefreshTokenCacheKey(string environment, string clientId, string userIdentifier) : base(clientId, userIdentifier)
        {
            Environment = environment;
        }

        public string Environment { get; }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(MsalHelpers.EncodeToBase64Url(Environment) + "$");
            stringBuilder.Append(MsalHelpers.EncodeToBase64Url(ClientId) + "$");
            stringBuilder.Append(MsalHelpers.EncodeToBase64Url(UserIdentifier) + "$");

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <returns>
        /// true if the specified object is equal to the current object; otherwise, false.
        /// </returns>
        /// <param name="obj">The object to compare with the current object. </param>
        /// <filterpriority>2</filterpriority>
        public override bool Equals(object obj)
        {
            RefreshTokenCacheKey other = obj as RefreshTokenCacheKey;
            return (other != null) && Equals(other);
        }

        /// <summary>
        /// Determines whether the specified AccessTokenCacheKey is equal to the current object.
        /// </summary>
        /// <returns>
        /// true if the specified AccessTokenCacheKey is equal to the current object; otherwise, false.
        /// </returns>
        /// <param name="other">The AccessTokenCacheKey to compare with the current object. </param>
        /// <filterpriority>2</filterpriority>
        public bool Equals(RefreshTokenCacheKey other)
        {
            return ReferenceEquals(this, other) ||
                   (other != null
                    && (other.Environment == Environment)
                    && Equals(ClientId, other.ClientId)
                    && UserIdentifier == other.UserIdentifier);
        }

        /// <summary>
        /// Returns the hash code for this AccessTokenCacheKey.
        /// </summary>
        /// <returns>
        /// A 32-bit signed integer hash code.
        /// </returns>
        public override int GetHashCode()
        {
            const string Delimiter = ":::";
            return (Environment + Delimiter
                    + ClientId.ToLowerInvariant() + Delimiter
                    + UserIdentifier).GetHashCode();
        }
    }
}
