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
