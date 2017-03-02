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
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client
{
    internal class Logger : ILogger
    {
        public void Error(string errorMessage)
        {
            Console.WriteLine(errorMessage); //Console.writeline writes to NSLog by default
        }

        public void Verbose(string verboseMessage)
        {
            Console.WriteLine(verboseMessage); //Console.writeline writes to NSLog by default
        }

        public void Information(string infoMessage)
        { 
            Console.WriteLine(infoMessage); //Console.writeline writes to NSLog by default
        }

        public void Warning(string warningMessage)
        {
            Console.WriteLine(warningMessage); //Console.writeline writes to NSLog by default
        }

        public void Error(Exception ex)
        {
            Error(ex.ToString());
        }

        public void Warning(Exception ex)
        {
            Warning(ex.ToString());
        }

        public void Information(Exception ex)
        {
            Information(ex.ToString());
        }

        public void Verbose(Exception ex)
        {
            Verbose(ex.ToString());
        }
    }
}