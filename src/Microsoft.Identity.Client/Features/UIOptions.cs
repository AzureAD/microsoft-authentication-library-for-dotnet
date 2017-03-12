using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Features
{
    /// <summary>
    /// Indicates how AcquireToken should prompt the user.
    /// </summary>
    public partial struct UIOptions
    {
        /// <summary>
        /// AcquireToken will send prompt=select_account to authorize endpoint 
        /// and would show a list of users from which one can be selected for 
        /// authentication.
        /// </summary>
        public readonly static UIOptions Never = new UIOptions("attempt_none");
    }
}
