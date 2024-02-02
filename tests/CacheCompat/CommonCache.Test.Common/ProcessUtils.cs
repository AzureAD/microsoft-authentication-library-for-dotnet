// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommonCache.Test.Common;

namespace CommonCache.Test.Common
{
    /// <summary>
    ///     A collection of Process-based utility functions.
    /// </summary>
    public sealed class ProcessUtils : IProcessUtils
    {
        private static readonly object SyncRoot = new object();
        private static readonly HashSet<ProcessHelper> CurrentRunningProcesses = new HashSet<ProcessHelper>();

        public async Task<string> FindProgramAsync(string findArgs, CancellationToken cancellationToken)
        {
            if (File.Exists(findArgs))
            {
                return findArgs;
            }

            var executable = findArgs;
            try
            {
                Console.WriteLine($"Calling:  where {findArgs}");
                ProcessRunResults whereResults = await RunProcessAsync("where", findArgs, cancellationToken).ConfigureAwait(false);
                if (whereResults != null)
                {
                    Console.WriteLine($"Search result: {whereResults}");
                    if (!string.IsNullOrEmpty(whereResults.StandardOut))
                    {
                        var results = whereResults.StandardOut.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                        if (results.Length > 0)
                            executable = results[0].Trim();
                        else
                            executable = whereResults.StandardOut.Trim();
                    }
                }
            }
            catch (ProcessRunException ex)
            {
                Console.WriteLine(ex.ProcessStandardOutput);
                throw;
            }

            return executable;
        }

        public void KillAllChildProcesses()
        {
            lock (SyncRoot)
            {
                foreach (var processHelper in CurrentRunningProcesses)
                {
                    try
                    {
                        processHelper.Process.Kill();
                    }
                    catch (Win32Exception)
                    {
                    }
                    catch (InvalidOperationException)
                    {
                    }
                }

                CurrentRunningProcesses.Clear();
            }
        }

        /// <inheritdoc/>
        public ProcessRunResults RunProcess(string fileName, string arguments, Dictionary<string, string> environmentVars = null)
        {
            return RunProcess(fileName, arguments, environmentVars, null);
        }

        /// <inheritdoc/>
        public ProcessRunResults RunProcess(string fileName, string arguments, IEnumerable<int> successfulExitCodes)
        {
            return RunProcess(
                fileName,
                arguments,
                null,
                null,
                false,
                successfulExitCodes);
        }

        /// <inheritdoc/>
        public ProcessRunResults RunProcess(
            string fileName,
            string arguments,
            Dictionary<string, string> environmentVars,
            WaitHandle cancelWaitHandle,
            bool shouldIgnoreConsoleOutput = false)
        {
            return RunProcess(
                fileName,
                arguments,
                environmentVars,
                cancelWaitHandle,
                shouldIgnoreConsoleOutput,
                null);
        }

        /// <inheritdoc/>
        public ProcessRunResults RunProcess(
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
            string processKillDumpFile = null)
        {
            //_log.LogDebug("Running Process: fileName({0}) arguments({1})", fileName, arguments);

            // Check for cancel before we go through the work of launching the process.
            if (cancelWaitHandle != null && cancelWaitHandle.WaitOne(0))
            {
                throw new OperationCanceledException();
            }

            if (successfulExitCodes == null)
            {
                successfulExitCodes = new int[]
                {
                    0,
                };
            }

            using (var helper = new ProcessHelper(
                //_log,
                fileName,
                arguments,
                environmentVars,
                cancelWaitHandle,
                shouldIgnoreConsoleOutput,
                maxWorkingSetSizeMb,
                processWorkingDirectory,
                maxAllowedRuntimeInMinutes,
                maxVirtualMemorySizeMb,
                progress,
                processKillDumpFile))
            {
                AddChildProcess(helper);
                try
                {
                    helper.Run();

                    if (new HashSet<int>(successfulExitCodes).Contains(helper.ExitCode))
                    {
                        // exit code was successful
                        return new ProcessRunResults(helper.StandardOutput, helper.StandardError);
                    }

                    //_log.LogDebug(
                    //    "Running Process Failed: fileName({0}) arguments({1}) exitCode({2})",
                    //    fileName,
                    //    arguments,
                    //    helper.ExitCode);
                    //_log.LogDebug("StdOut: {0}", helper.StandardOutput);
                    //_log.LogDebug("StdErr: {0}", helper.StandardError);

                    throw new ProcessRunException(
                        fileName,
                        arguments,
                        helper.ExitCode,
                        helper.StandardOutput,
                        helper.StandardError);
                }
                finally
                {
                    RemoveChildProcess(helper);
                }
            }
        }

        /// <inheritdoc/>
        public Task<ProcessRunResults> RunProcessAsync(string fileName, string arguments)
        {
            return Task.Run(() => RunProcess(fileName, arguments));
        }

        /// <inheritdoc/>
        public Task<ProcessRunResults> RunProcessAsync(string fileName, string arguments, IEnumerable<int> successfulExitCodes)
        {
            return Task.Run(() => RunProcess(fileName, arguments, successfulExitCodes));
        }

        /// <inheritdoc/>
        public Task<ProcessRunResults> RunProcessAsync(string fileName, string arguments, CancellationToken cancellationToken)
        {
            return Task.Run(() => RunProcess(fileName, arguments, null, cancellationToken.WaitHandle), cancellationToken);
        }

        /// <inheritdoc/>
        public Task<ProcessRunResults> RunProcessAsync(
            string fileName,
            string arguments,
            Dictionary<string, string> environmentVars,
            bool shouldIgnoreConsoleOutput,
            IEnumerable<int> successfulExitCodes,
            long maxWorkingSetSizeMb,
            CancellationToken cancellationToken,
            string processWorkingDirectory = null,
            int maxAllowedRuntimeInMinutes = 0,
            long maxVirtualMemorySizeMb = 0,
            IProgress<string> progress = null,
            string processKillDumpFile = null)
        {
            return Task.Run(
                () => RunProcess(
                    fileName,
                    arguments,
                    environmentVars,
                    cancellationToken.WaitHandle,
                    shouldIgnoreConsoleOutput,
                    successfulExitCodes,
                    maxWorkingSetSizeMb,
                    processWorkingDirectory,
                    maxAllowedRuntimeInMinutes,
                    maxVirtualMemorySizeMb,
                    progress,
                    processKillDumpFile),
                cancellationToken);
        }

        /// <inheritdoc/>
        public ProcessRunResults RunProcessFromKnownDirectory(string fileName, string arguments, string processWorkingDirectory)
        {
            return RunProcess(
                fileName,
                arguments,
                null,
                null,
                false,
                new List<int>
                {
                    0
                },
                0,
                processWorkingDirectory);
        }

        public ProcessRunResults RunProcessFromKnownDirectory(
            string fileName,
            string arguments,
            string processWorkingDirectory,
            Dictionary<string, string> environmentVars,
            WaitHandle cancelWaitHandle,
            bool shouldIgnoreConsoleOutput,
            IEnumerable<int> successfulExitCodes)
        {
            return RunProcess(
                fileName,
                arguments,
                environmentVars,
                cancelWaitHandle,
                shouldIgnoreConsoleOutput,
                successfulExitCodes,
                0,
                processWorkingDirectory);
        }

        public IProcessRunningInfo LaunchProcess(
            string fileName,
            string arguments,
            Dictionary<string, string> environmentVariables)
        {
            //_log.LogDebug("LaunchProcess: fileName({0}) arguments({1})", fileName, arguments);
            if (environmentVariables != null)
            {
                if (environmentVariables.Any())
                {
                    //_log.LogDebug("Environment Variables:");
                    //foreach (var kvp in environmentVariables)
                    //{
                    //    _log.LogDebug($"{kvp.Key} ==> {kvp.Value}");
                    //}
                }
            }

            var processStartInfo = new ProcessStartInfo
            {
                FileName = fileName.EncloseQuotes(),
                Arguments = arguments,
                CreateNoWindow = true,
                // WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false
            };

            if (environmentVariables != null)
            {
                foreach (KeyValuePair<string, string> environmentVariable in environmentVariables)
                {
                    processStartInfo.EnvironmentVariables.Add(environmentVariable.Key, environmentVariable.Value);
                }
            }

            var process = new Process
            {
                StartInfo = processStartInfo,
                EnableRaisingEvents = true
            };

            process.Start();
            return new ProcessRunningInfo(process, false);
        }

        private void KillProcessIfRunning(Process processToKill, bool waitForExit)
        {
            try
            {
                //_log.LogDebug("Killing process {0} - {1}", processToKill.ProcessName, processToKill.Id);
                processToKill.Kill();
                if (waitForExit)
                {
                    processToKill.WaitForExit();
                }
            }
            catch (InvalidOperationException)
            {
                // The process is not running.
                //_log.LogInformation("Process {0} - {1} is not running.", processToKill.ProcessName, processToKill.Id);
            }
            catch (Exception)
            {
                //_log.LogInformation(
                //    0,
                //    e,
                //    "Unable to kill process {0} - {1}",
                //    processToKill.ProcessName,
                //    processToKill.Id);
            }
        }

        private void AddChildProcess(ProcessHelper processHelper)
        {
            lock (SyncRoot)
            {
                CurrentRunningProcesses.Add(processHelper);
            }
        }

        private void RemoveChildProcess(ProcessHelper processHelper)
        {
            lock (SyncRoot)
            {
                if (CurrentRunningProcesses.Contains(processHelper))
                {
                    CurrentRunningProcesses.Remove(processHelper);
                }
            }
        }

        private class ProcessHelper : IDisposable
        {
            //private readonly ILogger _log;
            private readonly StringBuilder _sbStdErr;
            private readonly StringBuilder _sbStdOut;
            private readonly object _syncObject;
            private bool _alreadyStarted;

            public ProcessHelper(
                //ILogger logger,
                string fileName,
                string arguments,
                Dictionary<string, string> environmentVars,
                WaitHandle cancelWaitHandle,
                bool shouldIgnoreConsoleOutput = false,
                long maxWorkingSetSizeMb = 0,
                string processWorkingDirectory = null,
                int maxAllowedRuntimeInMinutes = 0,
                long maxVirtualMemorySizeMb = 0,
                IProgress<string> progress = null,
                string processKillDumpFile = null)
            {
                //_log = logger;
                _sbStdOut = new StringBuilder();
                _sbStdErr = new StringBuilder();
                _syncObject = new object();

                FileName = fileName;
                Arguments = arguments;
                CancelWaitHandle = cancelWaitHandle;
                ShouldIgnoreConsoleOutput = shouldIgnoreConsoleOutput;
                MaxWorkingSetMb = maxWorkingSetSizeMb;
                MaxAllowedRuntimeInMinutes = maxAllowedRuntimeInMinutes;
                MaxVirtualMemorySizeMb = maxVirtualMemorySizeMb;
                Progress = progress;
                ProcessKillDumpFile = processKillDumpFile;

                Process = new Process
                {
                    StartInfo =
                    {
                        FileName = fileName.EncloseQuotes(),
                        Arguments = arguments,
                        CreateNoWindow = true,
                        // WindowStyle = ProcessWindowStyle.Hidden,
                        RedirectStandardOutput = !ShouldIgnoreConsoleOutput,
                        RedirectStandardError = !ShouldIgnoreConsoleOutput,
                        UseShellExecute = false
                    }
                };

                if (!string.IsNullOrEmpty(processWorkingDirectory))
                {
                    Process.StartInfo.WorkingDirectory = processWorkingDirectory;
                }

                // environment vars
                if (environmentVars != null)
                {
                    foreach (string key in environmentVars.Keys)
                    {
                        Process.StartInfo.EnvironmentVariables[key] = environmentVars[key];
                    }
                }

                if (!ShouldIgnoreConsoleOutput)
                {
                    Process.OutputDataReceived += StandardOutputHandler;
                    Process.ErrorDataReceived += StandardErrorHandler;
                }
            }

            public string FileName { get; }
            public string Arguments { get; }
            public WaitHandle CancelWaitHandle { get; }
            public Process Process { get; private set; }
            public int ExitCode { get; private set; }
            public bool ShouldIgnoreConsoleOutput { get; }
            public string StandardOutput { get; private set; }
            public string StandardError { get; private set; }
            public long MaxWorkingSetMb { get; }
            public int MaxAllowedRuntimeInMinutes { get; }
            public long MaxVirtualMemorySizeMb { get; }
            public IProgress<string> Progress { get; }
            public string ProcessKillDumpFile { get; }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            public void Run()
            {
                if (_alreadyStarted)
                {
                    throw new InvalidOperationException("Can't run again.");
                }

                var sw = Stopwatch.StartNew();
                _alreadyStarted = true;

                Process.Start();
                if (!ShouldIgnoreConsoleOutput)
                {
                    Process.BeginOutputReadLine();
                    Process.BeginErrorReadLine();
                }

                while (!Process.WaitForExit(1000))
                {
                    if (MaxWorkingSetMb > 0)
                    {
                        long peakWorkingSet = 0;
                        try
                        {
                            if (!Process.HasExited)
                            {
                                Process.Refresh();
                                peakWorkingSet = Process.PeakWorkingSet64;
                            }
                        }
                        catch (InvalidOperationException)
                        {
                        }

                        if (peakWorkingSet > MaxWorkingSetMb * 1024 * 1024)
                        {
                            //_log.LogInformation("Attempting to kill process since it exceeded it's maximum working set size");
                            try
                            {
                                if (!Process.HasExited)
                                {
                                    Process.Kill();

                                    // Wait for up to a minute so the process can release it's resources
                                    Process.WaitForExit(60000);
                                }
                            }
                            catch (Win32Exception)
                            {
                            }
                            catch (InvalidOperationException)
                            {
                            }

                            throw new InvalidOperationException(
                                $"Process {Process.ProcessName} exceeded working set limit. Process WS:{Process.WorkingSet64}. Limit:{Process.MaxWorkingSet}");
                        }
                    }

                    if (MaxAllowedRuntimeInMinutes > 0)
                    {
                        if (sw.Elapsed.TotalMinutes > MaxAllowedRuntimeInMinutes)
                        {
                            //_log.LogInformation(
                            //    "Attempting to kill process since it exceeded it's max allowed run time ({0} minutes)",
                            //    MaxAllowedRuntimeInMinutes);
                            try
                            {
                                if (!Process.HasExited)
                                {
                                    Process.Kill();

                                    // Wait for up to a minute so the process can release it's resources
                                    Process.WaitForExit(60000);
                                }
                            }
                            catch (Win32Exception)
                            {
                            }
                            catch (InvalidOperationException)
                            {
                            }

                            throw new InvalidOperationException(
                                $"Process exceeded max allowed run time ({MaxAllowedRuntimeInMinutes} minutes)");
                        }
                    }

                    if (MaxVirtualMemorySizeMb > 0)
                    {
                        long peakVirtualMemorySize = 0;
                        try
                        {
                            if (!Process.HasExited)
                            {
                                Process.Refresh();
                                peakVirtualMemorySize = Process.PeakVirtualMemorySize64;
                            }
                        }
                        catch (InvalidOperationException)
                        {
                        }

                        if (peakVirtualMemorySize > MaxVirtualMemorySizeMb * 1024 * 1024)
                        {
                            //_log.LogInformation(
                            //    "Attempting to kill process since it exceeded the configured Virtual memory limit");
                            try
                            {
                                if (!Process.HasExited)
                                {
                                    Process.Kill();

                                    // Wait for up to a minute so the process can release it's resources
                                    Process.WaitForExit(60000);
                                }
                            }
                            catch (Win32Exception)
                            {
                            }
                            catch (InvalidOperationException)
                            {
                            }

                            throw new InvalidOperationException(
                                $"Process {FileName} exceeded Virtual memory limit. Process VM Size:{peakVirtualMemorySize / (1024 * 1024)} MB. Limit:{MaxVirtualMemorySizeMb} MB");
                        }
                    }

                    if (CancelWaitHandle != null && CancelWaitHandle.WaitOne(0))
                    {
                        //_log.LogInformation("Attempting to kill process since cancelWaitHandle is set");
                        try
                        {
                            if (!Process.HasExited)
                            {
                                Process.Kill();
                            }
                        }
                        catch (Win32Exception)
                        {
                        }
                        catch (InvalidOperationException)
                        {
                        }

                        throw new OperationCanceledException();
                    }
                }

                // Note that earlier, we called Process.WaitForExit(1000).
                // https://msdn.microsoft.com/en-us/library/fb4aw7b8(v=vs.110).aspx
                // According to msdn documentation, that overload has a caveat:
                //   When standard output has been redirected to asynchronous event handlers,
                //   it is possible that output processing will not have completed when this method returns.
                //   To ensure that asynchronous event handling has been completed,
                //   call the WaitForExit() overload that takes no parameter after receiving a true
                //   from this overload. To help ensure that the Exited event is handled correctly
                //   in Windows Forms applications, set the SynchronizingObject property.
                // Hence, we call the overload with no parameter here.
                Process.WaitForExit();

                lock (_syncObject)
                {
                    StandardOutput = _sbStdOut.ToString();
                    StandardError = _sbStdErr.ToString();
                }

                ExitCode = Process.ExitCode;
            }

            private void StandardOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
            {
                lock (_syncObject)
                {
                    _sbStdOut.AppendLine(outLine.Data);

                    Progress?.Report(outLine.Data);
                }
            }

            private void StandardErrorHandler(object sendingProcess, DataReceivedEventArgs outLine)
            {
                lock (_syncObject)
                {
                    _sbStdErr.AppendLine(outLine.Data);

                    Progress?.Report(outLine.Data);
                }
            }

            ~ProcessHelper()
            {
                Dispose(false);
            }

            protected void Dispose(bool disposing)
            {
                if (disposing)
                {
                    Process?.Dispose();
                    Process = null;
                }
            }
        }
    }
}
