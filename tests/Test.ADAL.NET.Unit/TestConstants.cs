using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Test.ADAL.NET.Unit
{
    class TestConstants
    {
        public static readonly HashSet<string> DefaultScope = new HashSet<string>(new[] {"r1/scope1", "r1/scope2"});
        public static readonly HashSet<string> ScopeForAnotherResource = new HashSet<string>(new[] { "r2/scope1", "r2/scope2" });
        public static readonly string DefaultAuthorityHomeTenant = "https://login.microsoftonline.com/home";
        public static readonly string DefaultAuthorityGuestTenant = "https://login.microsoftonline.com/guest";
        public static readonly string DefaultClientId = "client_id";
        public static readonly TokenSubjectType DefaultTokenSubjectType= TokenSubjectType.UserPlusClient;
        public static readonly string DefaultUniqueId = "unique_id";
        public static readonly string DefaultDisplayableId = "displayable@id.com";
        public static readonly string DefaultRootId = "root_id";
        public static readonly string DefaultPolicy = "policy";

    }
}
