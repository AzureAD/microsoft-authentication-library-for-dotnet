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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Core;

namespace Test.Microsoft.Identity.Core.Unit
{
    internal class TestLogger : CoreLoggerBase
    {
        public TestLogger(Guid correlationId) : base(correlationId)
        {
            Default = this;
        }

        public TestLogger(Guid correlationId, string component) : base(correlationId)
        {
            Default = this;
        }

        public override void Error(string message)
        {
        }

        public override void ErrorPii(string message)
        {
        }

        public override void Warning(string message)
        {
        }

        public override void WarningPii(string message)
        {
        }

        public override void Info(string message)
        {
        }

        public override void InfoPii(string message)
        {
        }

        public override void Verbose(string message)
        {
        }

        public override void VerbosePii(string message)
        {
        }

        public override void Error(Exception ex)
        {
        }

        public override void ErrorPii(Exception ex)
        {
        }
    }
}
