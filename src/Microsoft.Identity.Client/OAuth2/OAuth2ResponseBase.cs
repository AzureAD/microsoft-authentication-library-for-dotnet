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

using System.Runtime.Serialization;

namespace Microsoft.Identity.Client.OAuth2
{
    internal class OAuth2ResponseBaseClaim
    {
        public const string Claims = "claims";
        public const string Error = "error";
        public const string SubError = "suberror";
        public const string ErrorDescription = "error_description";
        public const string ErrorCodes = "error_codes";
        public const string CorrelationId = "correlation_id";
    }

    [DataContract]
    internal class OAuth2ResponseBase
    {
        [DataMember(Name = OAuth2ResponseBaseClaim.Error, IsRequired = false)]
        public string Error { get; set; }

        [DataMember(Name = OAuth2ResponseBaseClaim.SubError, IsRequired = false)]
        public string SubError { get; set; }

        [DataMember(Name = OAuth2ResponseBaseClaim.ErrorDescription, IsRequired = false)]
        public string ErrorDescription { get; set; }

        /// <summary>
        /// Do not expose these in the MsalException because Evo does not guarantee that the error
        /// codes remain the same.
        /// </summary>
        [DataMember(Name = OAuth2ResponseBaseClaim.ErrorCodes, IsRequired = false)]
        public string[] ErrorCodes { get; set; }

        [DataMember(Name = OAuth2ResponseBaseClaim.CorrelationId, IsRequired = false)]
        public string CorrelationId { get; set; }

        [DataMember(Name = OAuth2ResponseBaseClaim.Claims, IsRequired = false)]
        public string Claims { get; set; }
    }
}