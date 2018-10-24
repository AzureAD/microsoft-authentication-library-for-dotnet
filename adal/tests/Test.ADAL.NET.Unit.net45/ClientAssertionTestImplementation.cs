//----------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using Microsoft.Identity.Core;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Platform;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Test.ADAL.NET.Common;

namespace Test.ADAL.NET.Unit
{
    /// <summary>
    /// Test implementation of IClientAssertionCertificate.
    /// </summary>
    [DeploymentItem("Resources\\valid_cert.pfx")]
    public class ClientAssertionTestImplementation : IClientAssertionCertificate
    {
        public string ClientId { get { return AdalTestConstants.DefaultClientId; } }

        public string Thumbprint { get { return AdalTestConstants.DefaultThumbprint; } }

        public byte[] Sign(string message)
        {
            return SigningHelper.SignWithCertificate(message, this.Certificate);
        }

        public X509Certificate2 Certificate { get; }

        public ClientAssertionTestImplementation()
        {
            this.Certificate = new X509Certificate2(
                Microsoft.Identity.Core.Unit.ResourceHelper.GetTestResourceRelativePath("valid_cert.pfx"), 
                AdalTestConstants.DefaultPassword);
        }
    }
}