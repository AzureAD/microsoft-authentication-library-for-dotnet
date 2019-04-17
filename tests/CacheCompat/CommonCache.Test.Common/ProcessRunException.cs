// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace CommonCache.Test.Common
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
