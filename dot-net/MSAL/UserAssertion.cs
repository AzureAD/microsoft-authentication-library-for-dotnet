using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSAL
{
    public class UserAssertion
    {
        private string assertion;

        // provide assertion type as a string or enum?
        // string allows extension without API update, but enum saves the user 
        // from digging into API to find out the values to be passed.
        public UserAssertion(string assertion)
        {
            this.assertion = assertion;
        }
    }
}
