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
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
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

    internal class MexParser
    {
        private const string WsTrustSoapTransport = "http://schemas.xmlsoap.org/soap/http";

        public static async Task<WsTrustAddress> FetchWsTrustAddressFromMexAsync(string federationMetadataUrl, UserAuthType userAuthType, CallState callState)
        {
            XDocument mexDocument = await FetchMexAsync(federationMetadataUrl, callState).ConfigureAwait(false);
            return ExtractWsTrustAddressFromMex(mexDocument, userAuthType, callState);
        }

        internal static async Task<XDocument> FetchMexAsync(string federationMetadataUrl, CallState callState)
        {
            XDocument mexDocument;
            try
            {
                IHttpClient request = PlatformPlugin.HttpClientFactory.Create(federationMetadataUrl, callState);
                using (var response = await request.GetResponseAsync().ConfigureAwait(false))
                {
                    mexDocument = XDocument.Load(EncodingHelper.GenerateStreamFromString(response.ResponseString), LoadOptions.None);
                }
            }
            catch (HttpRequestWrapperException ex)
            {
                throw new AdalServiceException(AdalError.AccessingWsMetadataExchangeFailed, ex);
            }
            catch (XmlException ex)
            {
                throw new AdalException(AdalError.ParsingWsMetadataExchangeFailed, ex);
            }

            return mexDocument;
        }

        internal static WsTrustAddress ExtractWsTrustAddressFromMex(XDocument mexDocument, UserAuthType userAuthType, CallState callState)
        {
            WsTrustAddress address = null;
            MexPolicy policy = null;
            try
            {
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
                else if (userAuthType == UserAuthType.IntegratedAuth)
                {
                    throw new AdalException(AdalError.IntegratedAuthFailed, new AdalException(AdalError.WsTrustEndpointNotFoundInMetadataDocument));
                }
                else
                {
                    throw new AdalException(AdalError.WsTrustEndpointNotFoundInMetadataDocument);
                }
            }
            catch (XmlException ex)
            {
                throw new AdalException(AdalError.ParsingWsMetadataExchangeFailed, ex);
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
                    if (auth == null)
                    {
                        //switch to sp2005
                        securityPolicy = XmlNamespace.Sp2005;
                        if ((auth = element.Elements(securityPolicy + "SignedSupportingTokens").FirstOrDefault()) ==
                            null)
                        {
                            continue;
                        }
                    }

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
