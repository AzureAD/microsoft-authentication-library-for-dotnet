namespace Microsoft.Identity.Client.PlatformsCommon.Interfaces
{
    internal interface ITokenCacheBlobStorage
    {
        void OnAfterAccess(TokenCacheNotificationArgs args);
        void OnBeforeAccess(TokenCacheNotificationArgs args);
        void OnBeforeWrite(TokenCacheNotificationArgs args);
    }
}