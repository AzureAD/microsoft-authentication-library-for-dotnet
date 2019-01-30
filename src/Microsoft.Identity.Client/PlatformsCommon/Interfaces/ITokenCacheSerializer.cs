namespace Microsoft.Identity.Client.PlatformsCommon.Interfaces
{
    /// <summary>
    /// Provides a high level token cache serialization solution that is similar to the one offered to MSAL customers. 
    /// </summary>
    internal interface ITokenCacheBlobStorage
    {
        void OnAfterAccess(TokenCacheNotificationArgs args);
        void OnBeforeAccess(TokenCacheNotificationArgs args);
        void OnBeforeWrite(TokenCacheNotificationArgs args);
    }
}