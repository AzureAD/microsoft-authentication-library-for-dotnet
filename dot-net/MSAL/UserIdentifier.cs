using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSAL
{
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
