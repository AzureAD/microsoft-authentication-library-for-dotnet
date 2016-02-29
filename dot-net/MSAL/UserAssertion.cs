using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSAL
{
    public class UserAssertion
    {
        // provide assertion type as a string or enum?
        // string allows extension without API update
        public UserAssertion(string assertion)
        {
            this.Assertion = assertion;
        }

        /// <summary>
        /// 
        /// </summary>
        public string Assertion { get; private set; }

    }
}
