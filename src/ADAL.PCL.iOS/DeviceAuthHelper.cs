using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Foundation;
using UIKit;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class DeviceAuthHelper : IDeviceAuthHelper
    {
        public bool CanHandleDeviceAuthChallenge { get { return false; } }
        public Task<string> CreateDeviceAuthChallengeResponse(IDictionary<string, string> challengeData)
        {
            throw new NotImplementedException();
        }
    }
}