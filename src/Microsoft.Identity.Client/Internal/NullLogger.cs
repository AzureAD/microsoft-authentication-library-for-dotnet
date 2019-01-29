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
        /// <inheritdoc />
        public Guid CorrelationId { get; } = Guid.Empty;

        /// <inheritdoc />
        public bool PiiLoggingEnabled { get; } = false;

        /// <inheritdoc />
        public void Error(string messageScrubbed)
        {
        }

        /// <inheritdoc />
        public void ErrorPii(string messageWithPii, string messageScrubbed)
        {
        }

        /// <inheritdoc />
        public void ErrorPii(Exception exWithPii)
        {
        }

        /// <inheritdoc />
        public void ErrorPiiWithPrefix(Exception exWithPii, string prefix)
        {
        }

        /// <inheritdoc />
        public void Warning(string messageScrubbed)
        {
        }

        /// <inheritdoc />
        public void WarningPii(string messageWithPii, string messageScrubbed)
        {
        }

        /// <inheritdoc />
        public void WarningPii(Exception exWithPii)
        {
        }

        /// <inheritdoc />
        public void WarningPiiWithPrefix(Exception exWithPii, string prefix)
        {
        }

        /// <inheritdoc />
        public void Info(string messageScrubbed)
        {
        }

        /// <inheritdoc />
        public void InfoPii(string messageWithPii, string messageScrubbed)
        {
        }

        /// <inheritdoc />
        public void InfoPii(Exception exWithPii)
        {
        }

        /// <inheritdoc />
        public void InfoPiiWithPrefix(Exception exWithPii, string prefix)
        {
        }

        /// <inheritdoc />
        public void Verbose(string messageScrubbed)
        {
        }

        /// <inheritdoc />
        public void VerbosePii(string messageWithPii, string messageScrubbed)
        {
        }
    }
}