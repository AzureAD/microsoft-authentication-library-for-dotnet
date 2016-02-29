using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MSAL
{
    /// <summary>
    /// 
    /// </summary>
    public class User
    {
        /// <summary>
        /// TODO - Better represent what sign out means for "User". how would sign out represent broker flows?
        /// TODO - 1) Deleting tokens for username+authority combo does not work as there a RT in cache that can be used to get cross tenant token silently.
        /// TODO - 2) Deleting ALL tokens for a given UPN would be wrong because userA in tenantX is not same as tenantY.
        /// TODO - We should consider putting signOut in application context because a user signs out of an application.
        /// TODO - Will calling sign out mutate the User object and clear all data from it? 
        /// </summary>
        public void SignOut()
        {
        }

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

        /// <summary>
        /// returns all the claims in a keypair.
        /// </summary>
        public IDictionary<string, string> AllClaims { get; }

    }
}