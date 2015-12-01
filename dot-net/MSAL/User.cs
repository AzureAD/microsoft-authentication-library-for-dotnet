using System;
using System.Collections.Generic;
using System.Linq;
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
        public void SignOut()
        {
        }
    }
}
