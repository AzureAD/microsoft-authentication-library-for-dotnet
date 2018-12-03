// ------------------------------------------------------------------------------
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
// ------------------------------------------------------------------------------

using System;
using Microsoft.Identity.Client.CacheV2.Impl.Utils;
using Microsoft.Identity.Core.Helpers;
using Microsoft.Identity.Json.Linq;

namespace Microsoft.Identity.Client.CacheV2.Impl
{
    internal class Jwt
    {
        public Jwt(string raw)
        {
            Raw = raw;
            if (string.IsNullOrWhiteSpace(Raw))
            {
                // warning: constructed jwt from empty string
                return;
            }

            string[] sections = Raw.Split('.');
            if (sections.Length != 3)
            {
                throw new ArgumentException("failed jwt decode: wrong number of sections", nameof(Raw));
            }

            Payload = Base64UrlHelpers.DecodeToString(sections[1]);
            Json = JObject.Parse(Payload);
            IsSigned = !string.IsNullOrEmpty(sections[2]);
        }

        protected JObject Json { get; }
        public string Raw { get; }
        public string Payload { get; }
        public bool IsSigned { get; }
        public bool IsEmpty => Json.IsEmpty();
    }
}