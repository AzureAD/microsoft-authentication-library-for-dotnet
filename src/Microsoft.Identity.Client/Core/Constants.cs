//------------------------------------------------------------------------------
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

using System.Globalization;
using System.Xml.Linq;

namespace Microsoft.Identity.Client.Core
{
    internal static class Constants
    {
        public const string MsAppScheme = "ms-app";
        public const int ExpirationMarginInMinutes = 5;
        public const int CodeVerifierLength = 128;
        public const int CodeVerifierByteSize = 32;

        public const string UapWEBRedirectUri = "https://sso"; // only ADAL supports WEB
        public const string DefaultRedirectUri = "urn:ietf:wg:oauth:2.0:oob";

        public const string DefaultRealm = "http://schemas.microsoft.com/rel/trusted-realm";

        public static string FormatEnterpriseRegistrationOnPremiseUri(string domain)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "https://enterpriseregistration.{0}/enrollmentserver/contract",
                domain);
        }

        public static string FormatEnterpriseRegistrationInternetUri(string domain)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "https://enterpriseregistration.windows.net/{0}/enrollmentserver/contract",
                domain);
        }

        public const string WellKnownOpenIdConfigurationPath = ".well-known/openid-configuration";
        public const string OpenIdConfigurationEndpoint = "v2.0/" + WellKnownOpenIdConfigurationPath;

        public static string FormatAdfsWebFingerUrl(string host, string resource)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "https://{0}/adfs/.well-known/webfinger?rel={1}&resource={2}",
                host,
                Constants.DefaultRealm,
                resource);
        }
    }


    internal static class XmlNamespace
    {
        public static readonly XNamespace Wsdl = "http://schemas.xmlsoap.org/wsdl/";
        public static readonly XNamespace Wsp = "http://schemas.xmlsoap.org/ws/2004/09/policy";
        public static readonly XNamespace Http = "http://schemas.microsoft.com/ws/06/2004/policy/http";
        public static readonly XNamespace Sp = "http://docs.oasis-open.org/ws-sx/ws-securitypolicy/200702";
        public static readonly XNamespace Sp2005 = "http://schemas.xmlsoap.org/ws/2005/07/securitypolicy";

        public static readonly XNamespace Wsu =
            "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";

        public static readonly XNamespace Soap12 = "http://schemas.xmlsoap.org/wsdl/soap12/";
        public static readonly XNamespace Wsa10 = "http://www.w3.org/2005/08/addressing";
        public static readonly XNamespace Trust = "http://docs.oasis-open.org/ws-sx/ws-trust/200512";
        public static readonly XNamespace Trust2005 = "http://schemas.xmlsoap.org/ws/2005/02/trust";
        public static readonly XNamespace Issue = "http://docs.oasis-open.org/ws-sx/ws-trust/200512/RST/Issue";
        public static readonly XNamespace Issue2005 = "http://schemas.xmlsoap.org/ws/2005/02/trust/RST/Issue";
        public static readonly XNamespace SoapEnvelope = "http://www.w3.org/2003/05/soap-envelope";
    }
}