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

namespace CommonCache.Test.Validator
{
    public class ProcessRunException : Exception
    {
        public ProcessRunException()
        {
        }

        public ProcessRunException(
            string fileName,
            string arguments,
            int processExitCode,
            string processStandardOutput,
            string processStandardError)
            : base($"Process {fileName} has exited with code {processExitCode}")
        {
            FileName = fileName;
            Arguments = arguments;
            ProcessExitCode = processExitCode;
            ProcessStandardOutput = processStandardOutput;
            ProcessStandardError = processStandardError;
        }

        public ProcessRunException(string message)
            : base(message)
        {
        }

        public ProcessRunException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public string FileName { get; }
        public string Arguments { get; }
        public int ProcessExitCode { get; }
        public string ProcessStandardOutput { get; }
        public string ProcessStandardError { get; }
    }
}