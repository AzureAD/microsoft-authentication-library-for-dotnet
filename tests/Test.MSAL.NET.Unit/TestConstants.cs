using System.Collections.Generic;
using Microsoft.Identity.Client;

namespace Test.MSAL.NET.Unit
{
    class TestConstants
    {
        public static readonly HashSet<string> DefaultScope = new HashSet<string>(new[] {"r1/scope1", "r1/scope2"});
        public static readonly HashSet<string> ScopeForAnotherResource = new HashSet<string>(new[] { "r2/scope1", "r2/scope2" });
        public static readonly string DefaultAuthorityHomeTenant = "https://login.microsoftonline.com/home/";
        public static readonly string DefaultAuthorityGuestTenant = "https://login.microsoftonline.com/guest/";
        public static readonly string DefaultAuthorityCommonTenant = "https://login.microsoftonline.com/common/";
        public static readonly string DefaultClientId = "client_id";
        public static readonly string DefaultUniqueId = "unique_id";
        public static readonly string DefaultDisplayableId = "displayable@id.com";
        public static readonly string DefaultHomeObjectId = "home_oid";
        public static readonly string DefaultPolicy = "policy";
        public static readonly string DefaultRedirectUri = "urn:ietf:wg:oauth:2.0:oob";
        public static readonly bool DefaultRestrictToSingleUser = false;
        public static readonly string DefaultClientSecret = "client_secret";

        public static readonly User DefaultUser = new User
        {
            UniqueId = DefaultUniqueId,
            DisplayableId = DefaultDisplayableId,
            HomeObjectId = DefaultHomeObjectId
        };
    }
}
