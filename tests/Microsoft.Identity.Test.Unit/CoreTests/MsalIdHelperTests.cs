// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet.Versioning;

namespace Microsoft.Identity.Test.Unit.CoreTests
{
    [TestClass]
    public class MsalIdHelperTests
    {
        [TestMethod]
        public void XClientOSTest()
        {
            // Act
            IDictionary<string, string> parameters = MsalIdHelper.GetMsalIdParameters(null);

            // Assert
            Assert.IsTrue(parameters.ContainsKey(MsalIdParameter.OS));
            Assert.IsTrue(parameters.ContainsKey(MsalIdParameter.Product)); // sku
            Assert.IsTrue(parameters.ContainsKey(MsalIdParameter.Version)); // version
            Assert.IsFalse(parameters.ContainsKey(MsalIdParameter.DeviceModel)); // device - we don't send this on .NET and .NET fwk

            string os = parameters[MsalIdParameter.OS];

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // This matches the ESTS code https://msazure.visualstudio.com/One/_git/ESTS-Main?path=/src/Product/Microsoft.AzureAD.ESTS/Sts/ClientInfo.cs&version=GBmaster&_a=contents
                Assert.IsTrue(os.Contains("Windows"));
                Regex AdalOsVersionRegex = new Regex(@"[\d]+[.\d]*", RegexOptions.Compiled);
                Match match = AdalOsVersionRegex.Match(os);
                Assert.IsTrue(match.Success);
                
                NuGetVersion.TryParse(match.Value, out var semanticVersion);

                Assert.IsTrue(semanticVersion.Major >= 10);

                
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Assert.AreEqual(AbstractPlatformProxy.MacOsDescriptionForSTS, os);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Assert.AreEqual(AbstractPlatformProxy.LinuxOSDescriptionForSTS, os);
            }
            else
            {
                Assert.Fail("Unknown OS");
            }

        }
    }
}
