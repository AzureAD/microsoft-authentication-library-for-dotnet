using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace MSAL
{
    public class UserInfo
    {
        /// <summary>
        /// Gets identifier of the user authenticated during token acquisition. 
        /// </summary>
        [DataMember]
        public string UniqueId { get; internal set; }

        /// <summary>
        /// Gets a displayable value in UserPrincipalName (UPN) format. The value can be null.
        /// </summary>
        [DataMember]
        public string DisplayableId { get; internal set; }

        /// <summary>
        /// Gets given name of the user if provided by the service. If not, the value is null. 
        /// </summary>
        [DataMember]
        public string GivenName { get; internal set; }

        /// <summary>
        /// Gets family name of the user if provided by the service. If not, the value is null. 
        /// </summary>
        [DataMember]
        public string FamilyName { get; internal set; }

        /// <summary>
        /// Gets the time when the password expires. Default value is 0.
        /// </summary>
        [DataMember]
        public DateTimeOffset? PasswordExpiresOn { get; internal set; }

        /// <summary>
        /// Gets the url where the user can change the expiring password. The value can be null.
        /// </summary>
        [DataMember]
        public Uri PasswordChangeUrl { get; internal set; }

        /// <summary>
        /// Gets identity provider if returned by the service. If not, the value is null. 
        /// </summary>
        [DataMember]
        public string IdentityProvider { get; internal set; }

        public IDictionary<string, string> AllClaims { get; }
    }
}
