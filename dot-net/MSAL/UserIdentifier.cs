using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSAL
{
    /// <summary>
    /// Indicates the type of <see cref=" UserIdentifier"/>
    /// </summary>
    public enum UserIdentifierType
    {
        /// <summary>
        /// When a <see cref=" UserIdentifier"/> of this type is passed in a token acquisition operation,
        /// the operation is guaranteed to return a token issued for the user with corresponding <see cref=" UserIdentifier.UniqueId"/> or fail.
        /// </summary>
        UniqueId,

        /// <summary>
        /// When a <see cref=" UserIdentifier"/> of this type is passed in a token acquisition operation,
        /// the operation restricts cache matches to the value provided and injects it as a hint in the authentication experience. However the end user could overwrite that value, resulting in a token issued to a different account than the one specified in the <see cref=" UserIdentifier"/> in input.
        /// </summary>
        OptionalDisplayableId,

        /// <summary>
        /// When a <see cref=" UserIdentifier"/> of this type is passed in a token acquisition operation,
        /// the operation is guaranteed to return a token issued for the user with corresponding <see cref=" UserIdentifier.DisplayableId"/> (UPN or email) or fail
        /// </summary>
        RequiredDisplayableId
    }

    public class UserIdentifier
    {
        public UserIdentifier(string id, UserIdentifierType type)
        {
        }

        /// <summary>
        /// Gets type of the <see cref="UserIdentifier"/>.
        /// </summary>
        public UserIdentifierType Type { get; private set; }

        /// <summary>
        /// Gets Id of the <see cref="UserIdentifier"/>.
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// Gets an static instance of <see cref="UserIdentifier"/> to represent any user.
        /// </summary>
        public static UserIdentifier AnyUser
        {
            get
            {
                return null; //
            }
        }
    }
}
