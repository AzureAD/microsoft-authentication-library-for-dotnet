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
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.Internal
{
    internal class NullLogger : ICoreLogger
    {
        public string ClientName { get; } = string.Empty;
        public string ClientVersion { get; } = string.Empty;

        public Guid CorrelationId { get; } = Guid.Empty;
        public bool PiiLoggingEnabled { get; } = false;

        public void Error(string messageScrubbed)
        {
        }

        public void ErrorPii(string messageWithPii, string messageScrubbed)
        {
        }

        public void ErrorPii(Exception exWithPii)
        {
        }

        public void ErrorPiiWithPrefix(Exception exWithPii, string prefix)
        {
        }

        public void Warning(string messageScrubbed)
        {
        }

        public void WarningPii(string messageWithPii, string messageScrubbed)
        {
        }

        public void WarningPii(Exception exWithPii)
        {
        }

        public void WarningPiiWithPrefix(Exception exWithPii, string prefix)
        {
        }

        public void Info(string messageScrubbed)
        {
        }

        public void InfoPii(string messageWithPii, string messageScrubbed)
        {
        }

        public void InfoPii(Exception exWithPii)
        {
        }

        public void InfoPiiWithPrefix(Exception exWithPii, string prefix)
        {
        }

        public void Verbose(string messageScrubbed)
        {
        }

        public void VerbosePii(string messageWithPii, string messageScrubbed)
        {
        }
    }
}
