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
using System.Globalization;
using System.Net;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal partial class Logger
    {
        internal static string PrepareLogMessage(CallState callState, string format, params object[] args)
        {
            return string.Format(CultureInfo.CurrentCulture, format, args) + (callState != null ? (". Correlation ID: " + callState.CorrelationId) : string.Empty);
        }

        internal static void LogException(CallState callState, Exception ex)
        {
            Information(callState, "=== Token Acquisition finished with error:\n\t{0}", GetLogException(ex));
        }

        private static string GetLogException(Exception ex)
        {
            string message = string.Format("Exception Type: {0}\n\tMessage: {1}\n\t", ex.GetType().Name, ex.Message.Replace("\n", "\n\t\t"));
            ArgumentException argumentEx = ex as ArgumentException;
            AdalServiceException adalServiceEx = ex as AdalServiceException;
            AdalException adalEx = ex as AdalException;
            WebException webEx = ex as WebException;
            if (argumentEx != null)
            {
                message += string.Format("ParamName: {0}", argumentEx.ParamName);
            }
            else if (adalServiceEx != null)
            {
                message += string.Format("ErrorCode: {0}\n\tStatusCode: {1}", adalServiceEx.ErrorCode, adalServiceEx.StatusCode);
            }
            else if (adalEx != null)
            {
                message += string.Format("ErrorCode: {0}", adalEx.ErrorCode);
            }
            else if (webEx != null)
            {
                message += string.Format("Status: {0}\n\tSource: {1}", webEx.Status, webEx.Source);
            }

            if (ex.InnerException != null)
            {
                message += string.Format("\n\tInnerException:\n\t\t{0}", GetLogException(ex.InnerException).Replace("\n", "\n\t\t"));
            }

            return message + "\n\t";
        }
    }
}