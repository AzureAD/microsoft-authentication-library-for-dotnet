using System;

namespace Test.ADAL.NET.Unit
{
    public class TestConstants
    {
        public static readonly string DefaultResource = "resource1";
        public static readonly string AnotherResource = "resource2";
        public static readonly string DefaultAdfsAuthorityTenant = "https://login.contodo.com/adfs/";
        public static readonly string DefaultAuthorityHomeTenant = "https://login.microsoftonline.com/home/";
        public static readonly string SomeTenantId = "some-tenant-id";
        public static readonly string TenantSpecificAuthority = $"https://login.microsoftonline.com/{SomeTenantId}/";
        public static readonly string DefaultAuthorityGuestTenant = "https://login.microsoftonline.com/guest/";
        public static readonly string DefaultAuthorityCommonTenant = "https://login.microsoftonline.com/common/";
        public static readonly string DefaultClientId = "client_id";
        public static readonly string DefaultUniqueId = "unique_id";
        public static readonly string DefaultDisplayableId = "displayable@id.com";
        public static readonly Uri DefaultRedirectUri = new Uri("urn:ietf:wg:oauth:2.0:oob");
        public static readonly bool DefaultRestrictToSingleUser = false;
        public static readonly string DefaultClientSecret = "client_secret";
        public static readonly string DefaultPassword = "password";
        public static readonly bool DefaultExtendedLifeTimeEnabled = false;
        public static readonly bool PositiveExtendedLifeTimeEnabled = true;
        public static readonly string ErrorSubCode = "ErrorSubCode";
        public static readonly string CloudAudienceUrnMicrosoft = "urn:federation:MicrosoftOnline";
        public static readonly string CloudAudienceUrn = "urn:federation:Blackforest";
    }
}