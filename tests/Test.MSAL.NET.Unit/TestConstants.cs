using System.Collections.Generic;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;

namespace Test.ADAL.NET.Unit
{
    class TestConstants
    {
        public static readonly HashSet<string> DefaultScope = new HashSet<string>(new[] {"r1/scope1", "r1/scope2"});
        public static readonly HashSet<string> ScopeForAnotherResource = new HashSet<string>(new[] { "r2/scope1", "r2/scope2" });
        public static readonly string DefaultAuthorityHomeTenant = "https://login.microsoftonline.com/home";
        public static readonly string DefaultAuthorityGuestTenant = "https://login.microsoftonline.com/guest";
        public static readonly string DefaultClientId = "client_id";
        public static readonly TokenSubjectType DefaultTokenSubjectType= TokenSubjectType.User;
        public static readonly string DefaultUniqueId = "unique_id";
        public static readonly string DefaultDisplayableId = "displayable@id.com";
        public static readonly string DefaultRootId = "root_id";
        public static readonly string DefaultPolicy = "policy";

    }
}
