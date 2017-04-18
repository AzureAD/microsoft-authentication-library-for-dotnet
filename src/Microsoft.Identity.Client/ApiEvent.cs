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


using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client
{
    internal class ApiEvent : EventBase
    {
        public const string ConstApiId = EventNamePrefix + "api_id";
        public const string ConstAuthority = EventNamePrefix + "authority";
        public const string ConstAuthorityType = EventNamePrefix + "authority_type";
        public const string ConstUiBehavior = EventNamePrefix + "ui_behavior";
        public const string ConstValidationStatus = EventNamePrefix + "validation_status";
        public const string ConstTenantId = EventNamePrefix + "tenant_id";
        public const string ConstUserId = EventNamePrefix + "user_id";
        public const string ConstWasSuccessful = EventNamePrefix + "was_successful";

        public ApiEvent() : base(EventNamePrefix + "api_event") {}

        public int ApiId
        {
            set => this[ConstApiId] = value.ToString();
        }

        public string Authority
        {
            set => this[ConstAuthority] = value?.ToLower();
        }

        public string AuthorityType
        {
            set => this[ConstAuthorityType] = value?.ToLower();
        }

        public string UiBehavior
        {
            set => this[ConstUiBehavior] = value?.ToLower();
        }

        public string ValidationStatus
        {
            set => this[ConstValidationStatus] = value?.ToLower();
        }

        public string TenantId
        {
            set => this[ConstTenantId] = (value != null) ? CryptographyHelper.CreateBase64UrlEncodedSha256Hash(value) : null;
        }

        public string UserId
        {
            set => this[ConstUserId] = value != null ? CryptographyHelper.CreateBase64UrlEncodedSha256Hash(value) : null;
        }

        public bool WasSuccessful
        {
            set => this[ConstWasSuccessful] = value.ToString().ToLower();
            get => this[ConstWasSuccessful] == true.ToString().ToLower();
        }
    }
}
