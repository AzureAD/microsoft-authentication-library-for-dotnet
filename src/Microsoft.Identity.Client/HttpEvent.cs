﻿//----------------------------------------------------------------------
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


namespace Microsoft.Identity.Client
{
    internal class HttpEvent : EventBase
    {
        public const string ConstHttpPath = EventNamePrefix + "http_path";
        public const string ConstUserAgent = EventNamePrefix + "user_agent";
        public const string ConstQueryParameters = EventNamePrefix + "query_parameters";
        public const string ConstApiVersion = EventNamePrefix + "api_version";
        public const string ConstResponseCode = EventNamePrefix + "response_code";
        public const string ConstOauthErrorCode = EventNamePrefix + "oauth_error_code";

        public HttpEvent() : base(EventNamePrefix + "http_event") {}

        public string HttpPath
        {
            set => this[ConstHttpPath] = value;  // http path is case-sensitive
        }

        public string UserAgent
        {
            set => this[ConstUserAgent] = value;
        }

        public string QueryParams
        {
            set => this[ConstQueryParameters] = value;  // query parameters are case-sensitive
        }

        public string ApiVersion {
            set => this[ConstApiVersion] = value?.ToLower();
        }

        public int HttpResponseStatus
        {
            set => this[ConstResponseCode] = value.ToString();
        }

        public string OauthErrorCode
        {
            set => this[ConstOauthErrorCode] = value;
        }
    }
}
