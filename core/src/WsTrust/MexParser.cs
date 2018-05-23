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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Identity.Core.Http;

namespace Microsoft.Identity.Core.WsTrust
{
    internal enum WsTrustVersion
    {
        WsTrust13,
        WsTrust2005
    }

    internal class WsTrustAddress
    {
        public Uri Uri { get; set; }

        public WsTrustVersion Version { get; set; }
    }

    internal class MexPolicy
    {
        public WsTrustVersion Version { get; set; }

        public string Id { get; set; }

        public UserAuthType AuthType { get; set; }

        public Uri Url { get; set; }
    }

    internal static class MexParser
    {
        private const string WsTrustSoapTransport = "http://schemas.xmlsoap.org/soap/http";

        /// <summary>
        /// Fetch federation metadata, parse it, and returns relevant WsTrustAddress.
        /// </summary>
        /// <returns>Returns WsTrustAddress, or returns null if the wanted policy is not found in mexDocument.</returns>
        /// <exception cref="Client.MsalException">If unable to access mex endpoint.</exception>
        /// <remarks>It could also potentially throw XmlException or http-relevant exceptions.</remarks>
        public static async Task<WsTrustAddress> FetchWsTrustAddressFromMexAsync(string federationMetadataUrl, UserAuthType userAuthType, RequestContext requestContext)
        {
            XDocument mexDocument = await FetchMexAsync(federationMetadataUrl, requestContext).ConfigureAwait(false);
            return ExtractWsTrustAddressFromMex(mexDocument, userAuthType, requestContext);
        }

        internal static async Task<XDocument> FetchMexAsync(string federationMetadataUrl, RequestContext requestContext)
        {
            var uri = new UriBuilder(federationMetadataUrl);
            var httpResponse = await HttpRequest.SendGetAsync(uri.Uri, null, requestContext).ConfigureAwait(false);
            if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) {
                throw new Client.MsalException(
                    MsalError.AccessingWsMetadataExchangeFailed,
                    string.Format(CultureInfo.CurrentCulture,
                        "Response status code does not indicate success: {0} ({1}).",
                        (int)httpResponse.StatusCode, httpResponse.StatusCode));
            }
            return XDocument.Parse(httpResponse.Body, LoadOptions.None);
        }

        /// <summary>
        /// Extract WsTrust Address from Mex Document
        /// </summary>
        /// <returns>Returns WsTrustAddress, or returns null if the wanted policy is not found in mexDocument.</returns>
        /// <exception cref="System.Xml.XmlException">If unable to parse mex document.</exception>
        internal static WsTrustAddress ExtractWsTrustAddressFromMex(XDocument mexDocument, UserAuthType userAuthType, RequestContext requestContext)
        {
            WsTrustAddress address = null;
            MexPolicy policy = null;

            Dictionary<string, MexPolicy> policies = ReadPolicies(mexDocument);
            Dictionary<string, MexPolicy> bindings = ReadPolicyBindings(mexDocument, policies);
            SetPolicyEndpointAddresses(mexDocument, bindings);
            Random random = new Random();
            //try ws-trust 1.3 first
            policy = policies.Values.Where(p => p.Url != null && p.AuthType == userAuthType && p.Version == WsTrustVersion.WsTrust13).OrderBy(p => random.Next()).FirstOrDefault() ??
                        policies.Values.Where(p => p.Url != null && p.AuthType == userAuthType).OrderBy(p => random.Next()).FirstOrDefault();

            if (policy != null)
            {
                address = new WsTrustAddress();
                address.Uri = policy.Url;
                address.Version = policy.Version;
            }
            return address;
        }

        internal static Dictionary<string, MexPolicy> ReadPolicies(XContainer mexDocument)
        {
            var policies = new Dictionary<string, MexPolicy>();
            IEnumerable<XElement> policyElements = mexDocument.Elements().First().Elements(XmlNamespace.Wsp + "Policy");
            foreach (XElement policy in policyElements)
            {
                XElement exactlyOnePolicy = policy.Elements(XmlNamespace.Wsp + "ExactlyOne").FirstOrDefault();
                if (exactlyOnePolicy == null)
                {
                    continue;
                }

                IEnumerable<XElement> all = exactlyOnePolicy.Descendants(XmlNamespace.Wsp + "All");
                foreach (XElement element in all)
                {
                    XNamespace securityPolicy = XmlNamespace.Sp;
                    XElement auth = element.Elements(XmlNamespace.Http + "NegotiateAuthentication").FirstOrDefault();
                    if (auth != null)
                    {
                        AddPolicy(policies, policy, UserAuthType.IntegratedAuth);
                    }

                    auth = element.Elements(securityPolicy + "SignedEncryptedSupportingTokens").FirstOrDefault();
                    if (auth == null && ((auth = element.Elements(XmlNamespace.Sp2005 + "SignedSupportingTokens").FirstOrDefault()) ==
                                         null))
                    {
                            continue;
                    }

                    securityPolicy = XmlNamespace.Sp2005;
                    XElement wspPolicy = auth.Elements(XmlNamespace.Wsp + "Policy").FirstOrDefault();
                    if (wspPolicy == null)
                    {
                        continue;
                    }

                    XElement usernameToken = wspPolicy.Elements(securityPolicy + "UsernameToken").FirstOrDefault();
                    if (usernameToken == null)
                    {
                        continue;
                    }

                    XElement wspPolicy2 = usernameToken.Elements(XmlNamespace.Wsp + "Policy").FirstOrDefault();
                    if (wspPolicy2 == null)
                    {
                        continue;
                    }

                    XElement wssUsernameToken10 = wspPolicy2.Elements(securityPolicy + "WssUsernameToken10").FirstOrDefault();
                    if (wssUsernameToken10 != null)
                    {
                        AddPolicy(policies, policy, UserAuthType.UsernamePassword);
                    }
                }
            }

            return policies;
        }

        private static Dictionary<string, MexPolicy> ReadPolicyBindings(XContainer mexDocument, IReadOnlyDictionary<string, MexPolicy> policies)
        {
            var bindings = new Dictionary<string, MexPolicy>();
            IEnumerable<XElement> bindingElements = mexDocument.Elements().First().Elements(XmlNamespace.Wsdl + "binding");
            foreach (XElement binding in bindingElements)
            {
                IEnumerable<XElement> policyReferences = binding.Elements(XmlNamespace.Wsp + "PolicyReference");
                foreach (XElement policyReference in policyReferences)
                {
                    XAttribute policyUri = policyReference.Attribute("URI");
                    if (policyUri == null || !policies.ContainsKey(policyUri.Value))
                    {
                        continue;
                    }

                    XAttribute bindingName = binding.Attribute("name");
                    if (bindingName == null)
                    {
                        continue;
                    }

                    XElement bindingOperation = binding.Elements(XmlNamespace.Wsdl + "operation").FirstOrDefault();
                    if (bindingOperation == null)
                    {
                        continue;
                    }

                    XElement soapOperation = bindingOperation.Elements(XmlNamespace.Soap12 + "operation").FirstOrDefault();
                    if (soapOperation == null)
                    {
                        continue;
                    }

                    XAttribute soapAction = soapOperation.Attribute("soapAction");
                    if (soapAction == null || (string.Compare(XmlNamespace.Issue.ToString(), soapAction.Value, StringComparison.OrdinalIgnoreCase) != 0
                        && string.Compare(XmlNamespace.Issue2005.ToString(), soapAction.Value, StringComparison.OrdinalIgnoreCase) != 0))
                    {
                        continue;
                    }

                    bool isWsTrust2005 =
                        string.Compare(XmlNamespace.Issue2005.ToString(), soapAction.Value,
                            StringComparison.OrdinalIgnoreCase) == 0;
                    policies[policyUri.Value].Version = isWsTrust2005 ? WsTrustVersion.WsTrust2005:WsTrustVersion.WsTrust13;


                    XElement soapBinding = binding.Elements(XmlNamespace.Soap12 + "binding").FirstOrDefault();
                    if (soapBinding == null)
                    {
                        continue;
                    }

                    XAttribute soapBindingTransport = soapBinding.Attribute("transport");
                    if (soapBindingTransport != null && string.Compare(WsTrustSoapTransport, soapBindingTransport.Value, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        bindings.Add(bindingName.Value, policies[policyUri.Value]);
                    }
                }
            }

            return bindings;
        }

        private static void SetPolicyEndpointAddresses(XContainer mexDocument, IReadOnlyDictionary<string, MexPolicy> bindings)
        {
            XElement serviceElement = mexDocument.Elements().First().Elements(XmlNamespace.Wsdl + "service").First();
            IEnumerable<XElement> portElements = serviceElement.Elements(XmlNamespace.Wsdl + "port");
            foreach (XElement port in portElements)
            {
                XAttribute portBinding = port.Attribute("binding");
                if (portBinding == null)
                {
                    continue;
                }

                string portBindingName = portBinding.Value;
                string[] portBindingNameSegments = portBindingName.Split(new[] { ':' }, 2);
                if (portBindingNameSegments.Length < 2 || !bindings.ContainsKey(portBindingNameSegments[1]))
                {
                    continue;
                }

                XElement endpointReference = port.Elements(XmlNamespace.Wsa10 + "EndpointReference").FirstOrDefault();
                if (endpointReference == null)
                {
                    continue;
                }

                XElement endpointAddress = endpointReference.Elements(XmlNamespace.Wsa10 + "Address").FirstOrDefault();
                if (endpointAddress != null && Uri.IsWellFormedUriString(endpointAddress.Value, UriKind.Absolute))
                {
                    bindings[portBindingNameSegments[1]].Url = new Uri(endpointAddress.Value);
                }
            }
        }

        private static void AddPolicy(IDictionary<string, MexPolicy> policies, XElement policy, UserAuthType policyAuthType)
        {
            XElement binding = policy.Descendants(XmlNamespace.Sp + "TransportBinding").FirstOrDefault()
                          ?? policy.Descendants(XmlNamespace.Sp2005 + "TransportBinding").FirstOrDefault();

            if (binding != null)
            {
                XAttribute id = policy.Attribute(XmlNamespace.Wsu + "Id");
                if (id != null)
                {
                    policies.Add("#" + id.Value, new MexPolicy { Id = id.Value, AuthType = policyAuthType });
                }
            }
        }
    }
}
