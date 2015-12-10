using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace MSAL
{
    // As I think about adding properties to this class, it seems natural for this
    // class to be the UserInfo object that we return inside the AuthenticationResult.
    // If not the same then we need to either consolidate or differentiate User and UserInfo
    // clearly so that there is no confusion about the overlap.
    // It would feel weird to simply have only a signout method in the class.
    public class User
    {
        /// <summary>
        /// Default Constructor that will set login_hint=null
        /// </summary>
        public User()
        {
        }

        public User(string username, UserIdentifierType userIdentifierType)
        {
        }

        /// <summary>
        /// Gets the type of the Access Token returned. 
        /// </summary>
        [DataMember]
        public string AccessTokenType { get; private set; }

        /// <summary>
        /// Gets the Access Token requested.
        /// </summary>
        [DataMember]
        public string AccessToken { get; internal set; }

        /// <summary>
        /// Gets the point in time in which the Access Token returned in the AccessToken property ceases to be valid.
        /// This value is calculated based on current UTC time measured locally and the value expiresIn received from the service.
        /// </summary>
        [DataMember]
        public DateTimeOffset ExpiresOn { get; internal set; }

        /// <summary>
        /// Gets an identifier for the tenant the token was acquired from. This property will be null if tenant information is not returned by the service.
        /// </summary>
        [DataMember]
        public string TenantId { get; private set; }

        /// <summary>
        /// TODO - Better represent what sign out means for "User". how would sign out represent broker flows?
        /// TODO - 1) Deleting tokens for username+authority combo does not work as there a RT in cache that can be used to get cross tenant token silently.
        /// TODO - 2) Deleting ALL tokens for a given UPN would be wrong because userA in tenantX is not same as tenantY.
        /// TODO - We should consider putting signOut in application context because a user signs out of an application.
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