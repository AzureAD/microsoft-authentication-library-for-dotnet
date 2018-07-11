using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.Microsoft.Identity.Core.UIAutomation.infrastructure
{
    public class UserQueryParameters
    {
        public FederationProvider? FederationProvider { get; set; }
        public bool? IsMamUser { get; set; }
        public bool? IsMfaUser { get; set; }
        public ISet<string> Licenses { get; set; }
        public bool? IsFederatedUser { get; set; }
        public UserType? IsUserType { get; set; }
        public bool? IsExternalUser { get; set; }
    }
}
