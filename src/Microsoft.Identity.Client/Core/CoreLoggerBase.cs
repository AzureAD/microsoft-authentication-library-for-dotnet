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

namespace Microsoft.Identity.Client.Core
{
    internal interface ICoreLogger
    {
        Guid CorrelationId { get; set; }

        bool PiiLoggingEnabled { get; }

        void Error(string messageScrubbed);
        void ErrorPii(string messageWithPii, string messageScrubbed);
        void ErrorPii(Exception exWithPii);
        void ErrorPiiWithPrefix(Exception exWithPii, string prefix);

        void Warning(string messageScrubbed);
        void WarningPii(string messageWithPii, string messageScrubbed);
        void WarningPii(Exception exWithPii);
        void WarningPiiWithPrefix(Exception exWithPii, string prefix);

        void Info(string messageScrubbed);
        void InfoPii(string messageWithPii, string messageScrubbed);
        void InfoPii(Exception exWithPii);
        void InfoPiiWithPrefix(Exception exWithPii, string prefix);

        void Verbose(string messageScrubbed);
        void VerbosePii(string messageWithPii, string messageScrubbed);
    }

    internal abstract class CoreLoggerBase : ICoreLogger
    {
        public static CoreLoggerBase Default { get; set; }

        public Guid CorrelationId { get; set; }

        protected CoreLoggerBase()
        {
            CorrelationId = Guid.Empty;
        }

        protected CoreLoggerBase(Guid correlationId)
        {
            CorrelationId = correlationId;
        }

        public abstract bool PiiLoggingEnabled { get; }

        public abstract void Error(string messageScrubbed);
        public abstract void ErrorPii(string messageWithPii, string messageScrubbed);
        public abstract void ErrorPii(Exception exWithPii);
        public abstract void ErrorPiiWithPrefix(Exception exWithPii, string prefix);

        public abstract void Warning(string messageScrubbed);
        public abstract void WarningPii(string messageWithPii, string messageScrubbed);
        public abstract void WarningPii(Exception exWithPii);
        public abstract void WarningPiiWithPrefix(Exception exWithPii, string prefix);

        public abstract void Info(string messageScrubbed);
        public abstract void InfoPii(string messageWithPii, string messageScrubbed);
        public abstract void InfoPii(Exception exWithPii);
        public abstract void InfoPiiWithPrefix(Exception exWithPii, string prefix);

        public abstract void Verbose(string messageScrubbed);
        public abstract void VerbosePii(string messageWithPii, string messageScrubbed);
    }
}
