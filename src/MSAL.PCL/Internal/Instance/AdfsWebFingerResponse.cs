//------------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted free of charge to any person obtaining a copy
// of this software and associated documentation files(the "Software") to deal
// in the Software without restriction including without limitation the rights
// to use copy modify merge publish distribute sublicense and / or sell
// copies of the Software and to permit persons to whom the Software is
// furnished to do so subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND EXPRESS OR
// IMPLIED INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM DAMAGES OR OTHER
// LIABILITY WHETHER IN AN ACTION OF CONTRACT TORT OR OTHERWISE ARISING FROM
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.Identity.Client.Internal.OAuth2;

namespace Microsoft.Identity.Client.Internal.Instance
{
    internal class AdfsWebFingerResponseClaim : OAuth2ResponseBaseClaim
    {
        public const string Subject = "subject";
        public const string Links = "links";
        public const string Rel = "rel";
        public const string Href = "href";
    }

    [DataContract(Name = AdfsWebFingerResponseClaim.Links)]
    internal class LinksList
    {
        [DataMember(Name = AdfsWebFingerResponseClaim.Rel, IsRequired = false)]
        public string Rel { get; set; }

        [DataMember(Name = AdfsWebFingerResponseClaim.Href, IsRequired = false)]
        public string Href { get; set; }
    }

    [DataContract]
    internal class AdfsWebFingerResponse : OAuth2ResponseBase
    {
        [DataMember(Name = AdfsWebFingerResponseClaim.Subject, IsRequired = false)]
        public string Subject { get; set; }

        [DataMember(Name = AdfsWebFingerResponseClaim.Links, IsRequired = false)]
        public List<LinksList> Links { get; set; }
    }
}