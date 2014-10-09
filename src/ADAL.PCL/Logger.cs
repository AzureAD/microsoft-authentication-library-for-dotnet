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

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal abstract class LoggerBase
    {
        internal abstract void Verbose(CallState callState, string format, params object[] args);

        internal abstract void Information(CallState callState, string format, params object[] args);

        internal abstract void Warning(CallState callState, string format, params object[] args);

        internal abstract void Error(CallState callState, string format, params object[] args);

        internal string PrepareLogMessage(CallState callState, string format, params object[] args)
        {
            return string.Format(CultureInfo.CurrentCulture, format, args) + (callState != null ? (". Correlation ID: " + callState.CorrelationId) : string.Empty);
        }

        internal void LogException(CallState callState, Exception ex)
        {
            ArgumentException argumentEx = ex as ArgumentException;
            if (argumentEx != null)
            {
                Information(callState, "ArgumentException was thrown for argument '{0}' with message '{1}'", argumentEx.ParamName, argumentEx.Message);
                return;
            }

            AdalServiceException adalServiceEx = ex as AdalServiceException;
            if (adalServiceEx != null)
            {
                Information(callState, "AdalServiceException was thrown with ErrorCode '{0}' and StatusCode '{1}' and innerException '{2}'", 
                    adalServiceEx.ErrorCode, adalServiceEx.StatusCode, (adalServiceEx.InnerException != null) ? adalServiceEx.Message : "No inner exception");
                return;
            }

            AdalException adalEx = ex as AdalException;
            if (adalEx != null)
            {
                Information(callState, "AdalException was thrown with ErrorCode '{0}'", adalEx.ErrorCode);
                return;
            }

            Information(callState, "Exception of type '{0}' was thrown with message '{1}'", ex.GetType().ToString(), ex.Message);
        }
    }
}