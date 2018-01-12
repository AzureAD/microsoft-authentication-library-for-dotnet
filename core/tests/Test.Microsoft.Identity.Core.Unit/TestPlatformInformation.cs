using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Core;

namespace Test.Microsoft.Identity.Core.Unit
{
    class TestPlatformInformation : CorePlatformInformationBase
    {
        static TestPlatformInformation()
        {
            Instance = new TestPlatformInformation();
        }

        public override string GetProductName()
        {
            return null;
        }

        public override string GetEnvironmentVariable(string variable)
        {
            return null;
        }

        public override string GetProcessorArchitecture()
        {
            return null;
        }

        public override string GetOperatingSystem()
        {
            return null;
        }

        public override string GetDeviceModel()
        {
            return null;
        }

        public override string GetAssemblyFileVersionAttribute()
        {
            return null;
        }

        public override Task<bool> IsUserLocalAsync(RequestContext requestContext)
        {
            throw new NotImplementedException();
        }
    }
}
