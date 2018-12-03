using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.MSAL.NET.Unit.net45.CacheV2Tests
{
    public static class MsalCacheV2TestConstants
    {
        public static readonly HashSet<string> Scope = new HashSet<string>(new[] { "r1/scope1", "r1/scope2" });

        public const string ProductionPrefNetworkEnvironment = "login.microsoftonline.com";

        public const string Uid = "my-uid";
        public const string Utid = "my-utid";

        public const string ClientId = "client_id";

        public const string AuthorityTestTenant = "https://" + ProductionPrefNetworkEnvironment + "/" + Utid + "/";
        
        public const long ValidExpiresIn = 3600;
        public const long ValidExtendedExpiresIn = 7200;
    }
}
