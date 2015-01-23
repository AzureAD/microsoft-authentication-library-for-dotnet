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
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Xml;

namespace Test.ADAL.Common
{
    public static class StsLoginFlow
    {
        private static readonly string AddressToken = "$ADDRESS$";
        private static readonly string AuthPolicyToken = "$AUTH_POLICY$";
        private static readonly string Token = "$TOKEN$";
        private static readonly string UserIDToken = "$USER_ID$";
        private static readonly string PasswordToken = "$PASSWORD$";

        private static readonly string UsernameToken = @"<wsse:UsernameToken Id=""user""><wsse:Username>$USER_ID$</wsse:Username><wsse:Password>$PASSWORD$</wsse:Password></wsse:UsernameToken>";

        public static string TryGetSamlToken(string qualifiedLoginHostUrl, string username, string password, string siteDnsName)
        {
            string token = UsernameToken;
            token = token.Replace(UserIDToken, username).Replace(PasswordToken, password);

            return TryGetSamlToken(qualifiedLoginHostUrl, token, siteDnsName);
        }

        public static string TryGetSamlToken(string qualifiedLoginHostUrl, string token, string siteDnsName)
        {
            UriBuilder builder = new UriBuilder(qualifiedLoginHostUrl);
            builder.Path = "rst2.srf";
            string request = ReadTemplateFile();
            request = request.Replace(Token, token).Replace(AddressToken, siteDnsName).Replace(AuthPolicyToken, "LBI_FED_STS_CLEAR");
            string returnedToken = MakeRawSoapRequest(builder.Uri, request);
            return GetTokenFromResponse(returnedToken);
        }

        private static string ReadTemplateFile()
        {
            using (Stream stream = Assembly.GetExecutingAssembly()
                               .GetManifestResourceStream("Test.ADAL.NET.SamlLoginTemplate.xml"))
            using (StreamReader reader = new StreamReader(stream))
            {
                string result = reader.ReadToEnd();
                return result;
            }
        }

        private static string GetTokenFromResponse(string returnedToken)
        {
            XmlDocument document = new XmlDocument();
            document.PreserveWhitespace = false;
            document.LoadXml(returnedToken);

            XmlNamespaceManager nsXml = new XmlNamespaceManager(document.NameTable);
            nsXml.AddNamespace("S", "http://www.w3.org/2003/05/soap-envelope");
            nsXml.AddNamespace("psf", "http://schemas.microsoft.com/Passport/SoapServices/SOAPFault");
            nsXml.AddNamespace("wst", "http://schemas.xmlsoap.org/ws/2005/02/trust");

            XmlElement root = document.DocumentElement;
            XmlNode refNode = root.SelectSingleNode("//psf:reqstatus", nsXml);
            string value = refNode.InnerText;
            refNode = root.SelectSingleNode("//wst:RequestedSecurityToken", nsXml);
            value = refNode.InnerXml;

            return value;
        }

        private static string MakeRawSoapRequest(Uri url, String soapEnvelopeText)
        {
            var httpWebRequest = WebRequest.Create(url) as HttpWebRequest;
            httpWebRequest.Method = "POST";
            httpWebRequest.ContentType = "text/xml;utf-8";

            Byte[] bytesToWrite = Encoding.UTF8.GetBytes(soapEnvelopeText);
            httpWebRequest.ContentLength = bytesToWrite.Length;

            using (Stream requestStream = httpWebRequest.GetRequestStream())
            {
                requestStream.Write(bytesToWrite, 0, bytesToWrite.Length);
                requestStream.Flush();
                requestStream.Close();
            }
            var httpWebResponse = httpWebRequest.GetResponse() as HttpWebResponse;
            using (Stream responseStream = httpWebResponse.GetResponseStream())
            {
                using (var reader = new StreamReader(responseStream))
                {
                    string response = reader.ReadToEnd();
                    httpWebResponse.Close();
                    return response;
                }
            }
        }
    }
}