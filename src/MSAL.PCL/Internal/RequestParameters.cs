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

using System.Collections.Generic;
using System.Text;

namespace Microsoft.Identity.Client.Internal
{
    internal interface IRequestParameters
    {
    }

    internal class DictionaryRequestParameters : Dictionary<string, string>, IRequestParameters
    {
        public DictionaryRequestParameters(HashSet<string> scope, ClientKey clientKey)
        {
            if (scope != null && scope.Count > 0)
            {
                this[OAuth2Parameter.Scope] = scope.AsSingleString();
            }

            clientKey.AddToParameters(this);
        }

        public string ExtraQueryParameter { get; set; }

        public override string ToString()
        {
            StringBuilder messageBuilder = new StringBuilder();

            foreach (KeyValuePair<string, string> kvp in this)
            {
                EncodingHelper.AddKeyValueString(messageBuilder, EncodingHelper.UrlEncode(kvp.Key),
                    EncodingHelper.UrlEncode(kvp.Value));
            }

            if (this.ExtraQueryParameter != null)
            {
                messageBuilder.Append('&' + this.ExtraQueryParameter);
            }

            return messageBuilder.ToString();
        }
    }

    internal class StringRequestParameters : IRequestParameters
    {
        private readonly StringBuilder parameter;

        public StringRequestParameters(StringBuilder stringBuilderParameter)
        {
            this.parameter = stringBuilderParameter;
        }

        public override string ToString()
        {
            return this.parameter.ToString();
        }
    }
}