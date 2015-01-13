//----------------------------------------------------------------------
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

using Windows.Foundation.Diagnostics;
namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    /// <summary>
    /// This class manages tracing in ADAL.
    /// </summary>
    public static class AdalTrace
    {
        static AdalTrace()
        {
            AdalLoggingChannel = new LoggingChannel("Microsoft.IdentityModel.Clients.ActiveDirectory");            
        }

        /// <summary>
        /// Gets the ADAL's Logging channel.
        /// </summary>
        public static LoggingChannel AdalLoggingChannel { get; private set; }
    }
}
