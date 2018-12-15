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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CommonCache.Test.Validator
{
    public interface IProcessUtils
    {
        void KillAllChildProcesses();
        ProcessRunResults RunProcess(string fileName, string arguments, Dictionary<string, string> environmentVars = null);
        ProcessRunResults RunProcess(string fileName, string arguments, IEnumerable<int> successfulExitCodes);

        /// <summary>
        ///     Executes a console-based process given a filename and arguments, waits for it to exit,
        ///     and returns the stdout output of the program once it has completed.
        /// </summary>
        /// <param name="fileName">
        ///     The filename (or full path) of the program to execute.  It will be enclosed in quotes by this
        ///     function.
        /// </param>
        /// <param name="arguments">The arguments to pass to the program.</param>
        /// <param name="environmentVars">Environment variables to set (optional).</param>
        /// <param name="cancelWaitHandle">WaitHandle to signal if process should be terminated/cancelled.</param>
        /// <param name="shouldIgnoreConsoleOutput">If true, ignore StandardOutput and StandardError capture.</param>
        /// <exception cref="InvalidOperationException">Thrown if the Process.ExitCode is not 0.</exception>
        /// <returns>A ProcessRunResults object containing information about the process execution.</returns>
        ProcessRunResults RunProcess(
            string fileName,
            string arguments,
            Dictionary<string, string> environmentVars,
            WaitHandle cancelWaitHandle,
            bool shouldIgnoreConsoleOutput = false);

        /// <summary>
        ///     Executes a console-based process given a filename and arguments, waits for it to exit,
        ///     and returns the stdout output of the program once it has completed.
        /// </summary>
        /// <param name="fileName">
        ///     The filename (or full path) of the program to execute.  It will be enclosed in quotes by this
        ///     function.
        /// </param>
        /// <param name="arguments">The arguments to pass to the program.</param>
        /// <param name="environmentVars">Environment variables to set (optional).</param>
        /// <param name="cancelWaitHandle">WaitHandle to signal if process should be terminated/cancelled.</param>
        /// <param name="shouldIgnoreConsoleOutput">If true, ignore StandardOutput and StandardError capture.</param>
        /// <param name="successfulExitCodes">List of exit codes that mean a successful exit of the program</param>
        /// <param name="maxWorkingSetSizeMb">Maximum working set size of the program before it is killed.</param>
        /// <param name="processWorkingDirectory">Working directory for the process</param>
        /// <param name="maxAllowedRuntimeInMinutes">Maximum run time allowed for the program before it is killed.</param>
        /// <param name="maxVirtualMemorySizeMb">Maximum virtual memory allowed for the program before it is killed.</param>
        /// <param name="progress">Mechanism for reporting console output as the process runs.</param>
        /// <param name="processKillDumpFile">
        ///     Path to where a full process MiniDump should be written before the program is killed for any reason. The
        ///     default is not to create a dump file when killing the program.
        /// </param>
        /// <exception cref="InvalidOperationException">Thrown if the Process.ExitCode is not 0.</exception>
        /// <returns>A ProcessRunResults object containing information about the process execution.</returns>
        ProcessRunResults RunProcess(
            string fileName,
            string arguments,
            Dictionary<string, string> environmentVars,
            WaitHandle cancelWaitHandle,
            bool shouldIgnoreConsoleOutput,
            IEnumerable<int> successfulExitCodes,
            long maxWorkingSetSizeMb = 0,
            string processWorkingDirectory = null,
            int maxAllowedRuntimeInMinutes = 0,
            long maxVirtualMemorySizeMb = 0,
            IProgress<string> progress = null,
            string processKillDumpFile = null);

        /// <summary>
        ///     Executes a console-based process given a filename and arguments, waits for it to exit,
        ///     and returns the stdout output of the program once it has completed.
        /// </summary>
        /// <param name="fileName">
        ///     The filename (or full path) of the program to execute.  It will be enclosed in quotes by this
        ///     function.
        /// </param>
        /// <param name="arguments">The arguments to pass to the program.</param>
        /// <exception cref="InvalidOperationException">Thrown if the Process.ExitCode is not 0.</exception>
        /// <returns>A ProcessRunResults object containing information about the process execution.</returns>
        Task<ProcessRunResults> RunProcessAsync(string fileName, string arguments);

        /// <summary>
        ///     Executes a console-based process given a filename and arguments, waits for it to exit,
        ///     and returns the stdout output of the program once it has completed.
        /// </summary>
        /// <param name="fileName">
        ///     The filename (or full path) of the program to execute.  It will be enclosed in quotes by this
        ///     function.
        /// </param>
        /// <param name="arguments">The arguments to pass to the program.</param>
        /// <param name="successfulExitCodes">List of exit codes that mean a successful exit of the program</param>
        /// <exception cref="InvalidOperationException">
        ///     Thrown if the Process.ExitCode is not one of the
        ///     <paramref name="successfulExitCodes" />.
        /// </exception>
        /// <returns>A ProcessRunResults object containing information about the process execution.</returns>
        Task<ProcessRunResults> RunProcessAsync(string fileName, string arguments, IEnumerable<int> successfulExitCodes);

        /// <summary>
        ///     Executes a console-based process given a filename and arguments, waits for it to exit,
        ///     and returns the stdout output of the program once it has completed.
        /// </summary>
        /// <param name="fileName">
        ///     The filename (or full path) of the program to execute.  It will be enclosed in quotes by this
        ///     function.
        /// </param>
        /// <param name="arguments">The arguments to pass to the program.</param>
        /// <param name="cancellationToken">Token used to signal if process should be terminated/cancelled.</param>
        /// <exception cref="InvalidOperationException">Thrown if the Process.ExitCode is not 0.</exception>
        /// <returns>A ProcessRunResults object containing information about the process execution.</returns>
        Task<ProcessRunResults> RunProcessAsync(string fileName, string arguments, CancellationToken cancellationToken);

        /// <summary>
        ///     Executes a console-based process given a filename and arguments, waits for it to exit,
        ///     and returns the stdout output of the program once it has completed.
        /// </summary>
        /// <param name="fileName">
        ///     The filename (or full path) of the program to execute.  It will be enclosed in quotes by this
        ///     function.
        /// </param>
        /// <param name="arguments">The arguments to pass to the program.</param>
        /// <param name="environmentVars">Environment variables to set (optional).</param>
        /// <param name="cancellationToken">Token used to signal if process should be terminated/cancelled.</param>
        /// <param name="shouldIgnoreConsoleOutput">If true, ignore StandardOutput and StandardError capture.</param>
        /// <param name="successfulExitCodes">List of exit codes that mean a successful exit of the program</param>
        /// <param name="maxWorkingSetSizeMb">Maximum working set size of the program before it is killed.</param>
        /// <param name="processWorkingDirectory">Working directory for the process</param>
        /// <param name="maxAllowedRuntimeInMinutes">Maximum run time allowed for the program before it is killed.</param>
        /// <param name="maxVirtualMemorySizeMb">Maximum virtual memory allowed for the program before it is killed.</param>
        /// <param name="progress">Mechanism for reporting console output as the process runs.</param>
        /// <param name="processKillDumpFile">
        ///     Path to where a full process MiniDump should be written before the program is killed for any reason. The
        ///     default is not to create a dump file when killing the program.
        /// </param>
        /// <exception cref="InvalidOperationException">
        ///     Thrown if the Process.ExitCode is not one of the
        ///     <paramref name="successfulExitCodes" />.
        /// </exception>
        /// <returns>A ProcessRunResults object containing information about the process execution.</returns>
        Task<ProcessRunResults> RunProcessAsync(
            string fileName,
            string arguments,
            Dictionary<string, string> environmentVars,
            CancellationToken cancellationToken,
            bool shouldIgnoreConsoleOutput,
            IEnumerable<int> successfulExitCodes,
            long maxWorkingSetSizeMb,
            string processWorkingDirectory = null,
            int maxAllowedRuntimeInMinutes = 0,
            long maxVirtualMemorySizeMb = 0,
            IProgress<string> progress = null,
            string processKillDumpFile = null);

        /// <summary>
        ///     Executes a console-based process given a filename and arguments, waits for it to exit,
        ///     and returns the stdout output of the program once it has completed.
        /// </summary>
        /// <param name="fileName">
        ///     The filename (or full path) of the program to execute.  It will be enclosed in quotes by this
        ///     function.
        /// </param>
        /// <param name="arguments">The arguments to pass to the program.</param>
        /// <param name="processWorkingDirectory">WorkingDirectory from which proces should be run.</param>
        /// <exception cref="InvalidOperationException">Thrown if the Process.ExitCode is not 0.</exception>
        /// <returns>A ProcessRunResults object containing information about the process execution.</returns>
        ProcessRunResults RunProcessFromKnownDirectory(string fileName, string arguments, string processWorkingDirectory);

        ProcessRunResults RunProcessFromKnownDirectory(
            string fileName,
            string arguments,
            string processWorkingDirectory,
            Dictionary<string, string> environmentVars,
            WaitHandle cancelWaitHandle,
            bool shouldIgnoreConsoleOutput,
            IEnumerable<int> successfulExitCodes);

        IProcessRunningInfo LaunchProcess(string fileName, string arguments, Dictionary<string, string> environmentVariables);
    }
}