using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Core;

namespace Test.Microsoft.Identity.Core.Unit
{
    internal class TestLogger : CoreLoggerBase
    {
        public TestLogger(Guid correlationId) : base(correlationId)
        {
            Default = this;
        }

        public TestLogger(Guid correlationId, string component) : base(correlationId)
        {
            Default = this;
        }

        public override void Error(string message)
        {
        }

        public override void ErrorPii(string message)
        {
        }

        public override void Warning(string message)
        {
        }

        public override void WarningPii(string message)
        {
        }

        public override void Info(string message)
        {
        }

        public override void InfoPii(string message)
        {
        }

        public override void Verbose(string message)
        {
            throw new NotImplementedException();
        }

        public override void VerbosePii(string message)
        {
        }

        public override void Error(Exception ex)
        {
        }

        public override void ErrorPii(Exception ex)
        {
        }
    }
}
