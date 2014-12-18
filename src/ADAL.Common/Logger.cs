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

using System;
using System.Diagnostics;
using System.Globalization;
using System.Net;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal partial class Logger
    {
        internal static string PrepareLogMessage(CallState callState, string classOrComponent, string format, params object[] args)
        {
            string message = string.Format(CultureInfo.CurrentCulture, format, args);
            string correlationId = (callState != null) ? callState.CorrelationId.ToString() : string.Empty;
            return string.Format(CultureInfo.CurrentCulture, "{0}: {1} - {2}: {3}", DateTime.UtcNow, correlationId, classOrComponent, message);
        }
    }
}