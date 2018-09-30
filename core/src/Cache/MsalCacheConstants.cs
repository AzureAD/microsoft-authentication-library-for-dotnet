using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Core.Cache
{
    internal class MsalCacheConstants
    {
        public const string ScopesDelimiter = " ";
        public const string CacheKeyDelimiter = "-";

        public const string IdToken = "idtoken";
        public const string AccessToken = "accesstoken";
        public const string RefreshToken = "refreshtoken";

    }
}
