using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.Microsoft.Identity.Core.UIAutomation
{
    /// <summary>
    /// Exception thrown when AppFactory tries to initialize an unsupported platform
    /// </summary>
    public class PlatformNotSupportedException : Exception
    {
        private const string message = "Platform not supported.";

        public PlatformNotSupportedException() : base(message)
        {
        }
    }
}
