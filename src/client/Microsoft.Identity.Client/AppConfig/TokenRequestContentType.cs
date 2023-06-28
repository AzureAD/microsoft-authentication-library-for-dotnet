// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.AppConfig
{
    /// <summary>
    /// The media content type for post requests made to the identity provider.
    /// </summary>
    public class TokenRequestContentType
    {
        internal string _value;

        internal TokenRequestContentType(string value) { _value = value; }

        /// <summary>
        /// JSON media content type
        /// </summary>
        public static TokenRequestContentType JSON { get { return new TokenRequestContentType("application/json"); } }

        internal string GetValue()
        {
            return _value;
        }
    }
}
