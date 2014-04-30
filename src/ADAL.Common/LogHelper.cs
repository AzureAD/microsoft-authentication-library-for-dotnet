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

// Content of this file is copied from this MSDN page: http://msdn.microsoft.com/en-us/library/01escwtf(v=vs.100).aspx

using System.Globalization;
using System.Text.RegularExpressions;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal static class LogHelper
    {
        internal static string PrepareLogMessage(CallState callState, string format, params object[] args)
        {
            return string.Format(CultureInfo.CurrentCulture, format, args) + (callState != null ? (". Correlation ID: " + callState.CorrelationId) : string.Empty);
        }
    }
}