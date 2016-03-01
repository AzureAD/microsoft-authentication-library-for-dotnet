﻿//----------------------------------------------------------------------
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
using Microsoft.Identity.Client.Interfaces;

namespace Microsoft.Identity.Client.Internal
{
    internal class HttpRequestWrapperException : Exception
    {
        public HttpRequestWrapperException(IHttpWebResponse webResponse, Exception innerException) 
            : base(string.Empty, innerException)
        {
            this.WebResponse = webResponse;
        }

        public IHttpWebResponse WebResponse { get; private set; }
    }
}
