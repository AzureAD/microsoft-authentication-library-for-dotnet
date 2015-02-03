//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class MexPolicy
    {
        public string Id { get; set; }   

        public UserAuthType AuthType { get; set; }

        public Uri Url { get; set; }
    }

    internal class MexParser
    {
        private const string WsTrustSoapTransport = "http://schemas.xmlsoap.org/soap/http";

        public static async Task<Uri> FetchWsTrustAddressFromMexAsync(string federationMetadataUrl, UserAuthType userAuthType, CallState callState)
        {
            XDocument mexDocument = await FetchMexAsync(federationMetadataUrl, callState);
            return ExtractWsTrustAddressFromMex(mexDocument, userAuthType, callState);
        }

        internal static async Task<XDocument> FetchMexAsync(string federationMetadataUrl, CallState callState)
        {
            XDocument mexDocument;
            try
            {
                IHttpClient request = PlatformPlugin.HttpClientFactory.Create(federationMetadataUrl, callState);
                using (var response = await request.GetResponseAsync())
                {
                    mexDocument = XDocument.Load(response.ResponseStream, LoadOptions.None);
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

        internal static Uri ExtractWsTrustAddressFromMex(XDocument mexDocument, UserAuthType userAuthType, CallState callState)
        {
            Uri url;

            try
            {
                Dictionary<string, MexPolicy> policies = ReadPolicies(mexDocument);
                Dictionary<string, MexPolicy> bindings = ReadPolicyBindings(mexDocument, policies);
                SetPolicyEndpointAddresses(mexDocument, bindings);
                Random random = new Random();
                MexPolicy policy = policies.Values.Where(p => p.Url != null && p.AuthType == userAuthType).OrderBy(p => random.Next()).FirstOrDefault();
                if (policy != null)
                {
                    url = policy.Url;
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

            return url;
        }

        private static Dictionary<string, MexPolicy> ReadPolicies(XContainer mexDocument)
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
                    XElement auth = element.Elements(XmlNamespace.Http + "NegotiateAuthentication").FirstOrDefault();
                    if (auth != null)
                    {
                        AddPolicy(policies, policy, UserAuthType.IntegratedAuth);
                    }

                    auth = element.Elements(XmlNamespace.Sp + "SignedEncryptedSupportingTokens").FirstOrDefault();
                    if (auth == null)
                    {
                        continue;
                    }

                    XElement wspPolicy = auth.Elements(XmlNamespace.Wsp + "Policy").FirstOrDefault();
                    if (wspPolicy == null)
                    {
                        continue;
                    }

                    XElement usernameToken = wspPolicy.Elements(XmlNamespace.Sp + "UsernameToken").FirstOrDefault();
                    if (usernameToken == null)
                    {
                        continue;
                    }

                    XElement wspPolicy2 = usernameToken.Elements(XmlNamespace.Wsp + "Policy").FirstOrDefault();
                    if (wspPolicy2 == null)
                    {
                        continue;
                    }

                    XElement wssUsernameToken10 = wspPolicy2.Elements(XmlNamespace.Sp + "WssUsernameToken10").FirstOrDefault();
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
                    if (soapAction == null || string.Compare(XmlNamespace.Issue.ToString(), soapAction.Value, StringComparison.OrdinalIgnoreCase) != 0)
                    {
                        continue;
                    }

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