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
        public static readonly HashSet<string> DefaultScope = new HashSet<string>(new[] {"scope1", "scope2"});
        public static readonly string DefaultAuthorityCommon = "https://login.microsoftonline.com/common";
        public static readonly string DefaultAuthorityTenant = "https://login.microsoftonline.com/tenant";
        public static readonly string DefaultClientId = "client_id";
        public static readonly TokenSubjectType DefaultTokenSubjectType= TokenSubjectType.UserPlusClient;
        public static readonly string DefaultUniqueId = "unique_id";
        public static readonly string DefaultDisplayableId = "displayable@id.com";
        public static readonly string DefaultRootId = "root_id";
        public static readonly string DefaultPolicy = "policy";

    }
}
