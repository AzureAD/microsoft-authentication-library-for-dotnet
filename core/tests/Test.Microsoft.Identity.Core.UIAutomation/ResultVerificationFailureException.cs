using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.Microsoft.Identity.Core.UIAutomation
{
    public class ResultVerificationFailureException : Exception
    {
        public VerificationError Error { get; private set; }

        public ResultVerificationFailureException(VerificationError error)
        {
            Error = error;
        }
    }

    public enum VerificationError
    {
        ResultNotFound,
        ResultIndicatesFailure
    }
}
