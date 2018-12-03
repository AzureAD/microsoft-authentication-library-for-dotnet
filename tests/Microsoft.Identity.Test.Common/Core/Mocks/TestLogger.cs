//----------------------------------------------------------------------
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

using System;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Test.Common.Core.Mocks
{
    internal class TestLogger : CoreLoggerBase
    {
        public TestLogger()
            : this(Guid.Empty)
        {
        }

        public TestLogger(Guid correlationId) : base(correlationId)
        {
            Default = this;
        }

        public TestLogger(Guid correlationId, string component) : base(correlationId)
        {
            Default = this;
        }

        private bool _isPiiLoggingEnabled;
        public override bool PiiLoggingEnabled => _isPiiLoggingEnabled;
        public void SetPiiLoggingEnabled(bool isEnabled)
        {
            _isPiiLoggingEnabled = isEnabled;
        }

        public override void Error(string message)
        {
        }

        public override void Warning(string message)
        {
        }

        public override void WarningPii(string messageWithPii, string messageScrubbed)
        {
        }

        public override void WarningPii(Exception ex)
        {
        }

        public override void Info(string message)
        {
        }

        public override void InfoPii(string messageWithPii, string messageScrubbed)
        {
        }

        public override void Verbose(string message)
        {
        }

        public override void VerbosePii(string messageWithPii, string messageScrubbed)
        {
        }

        public override void ErrorPii(Exception ex)
        {
        }

        public override void ErrorPii(string messageWithPii, string messageScrubbed)
        {
        }

        public override void ErrorPiiWithPrefix(Exception exWithPii, string prefix)
        {
        }

        public override void WarningPiiWithPrefix(Exception exWithPii, string prefix)
        {
        }

        public override void InfoPii(Exception exWithPii)
        {
        }

        public override void InfoPiiWithPrefix(Exception exWithPii, string prefix)
        {
        }
    }
}
