using Microsoft.Identity.Core.Telemetry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.Microsoft.Identity.Core.Unit.Mocks
{
    internal class TestTelemetry : ITelemetry
    {
        public void StartEvent(string requestId, EventBase eventToStart)
        {
            
        }

        public void StopEvent(string requestId, EventBase eventToStop)
        {
            
        }

        public void Flush(string requestId)
        {

        }
    }
}
