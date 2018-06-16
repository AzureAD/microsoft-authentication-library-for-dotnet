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
using System.Globalization;
using System.Runtime.Serialization;
using Microsoft.Identity.Core.Helpers;

namespace Microsoft.Identity.Core.Cache
{
    [DataContract]
    internal abstract class MsalCacheItemBase
    {
        [DataMember(Name = "unique_user_id", IsRequired = true)]
        public string UserIdentifier { get; internal set; }

        [DataMember(Name = "environment", IsRequired = true)]
        internal string Environment { get; set; }

        [DataMember(Name = "client_info")]
        internal string RawClientInfo { get; set; }

        public ClientInfo ClientInfo { get; set; }

        internal void InitClientInfo()
        {
            if (RawClientInfo != null)
            {
                ClientInfo = ClientInfo.CreateFromJson(RawClientInfo);
            }
        }

        internal void InitRawClientInfoDerivedProperties()
        {
            InitClientInfo();

            UserIdentifier = GetUserIdentifier();
        }

        string GetUserIdentifier()
        {
            if (ClientInfo == null)
            {
                return null;
            }
            return ClientInfo.ToUserIdentifier();
        }

        [OnDeserialized]
        void OnDeserialized(StreamingContext context)
        {
            InitClientInfo();
        }
    }
}