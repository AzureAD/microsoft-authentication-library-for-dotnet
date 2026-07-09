// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.KeyAttestation.Attestation
{
    internal static class AttestationLogger
    {
        /// <summary>
        /// Attestation Logger
        /// </summary>
        internal static readonly AttestationClientLib.LogFunc ConsoleLogger = (ctx, tag, lvl, func, line, msg) =>
        {
            try
            {
                var lineText = FormatNativeLog(tag, lvl, func, line, msg);

                // Default: Trace (respects listeners; safe for all app types)
                Trace.WriteLine(lineText);
            }
            catch
            {
            }
        };

        /// <summary>
        /// Builds a native log callback (<see cref="AttestationClientLib.LogFunc"/>) that forwards
        /// <c>AttestationClientLib.dll</c> log lines into the MSAL <see cref="ILoggerAdapter"/> so that
        /// native MAA diagnostics (e.g. policy-evaluation failures) appear in MSAL logs instead of only
        /// going to <see cref="Trace"/>. When <paramref name="logger"/> is null, falls back to the
        /// <see cref="ConsoleLogger"/> (Trace) so behavior is preserved for callers without a logger.
        /// </summary>
        /// <remarks>
        /// The returned delegate must never throw: a managed exception escaping back into the native
        /// caller would corrupt the interop boundary. All work is wrapped in a try/catch that swallows.
        /// Callers must keep a strong reference to the returned delegate for as long as the native library
        /// holds the function pointer (i.e. until <c>UninitAttestationLib</c>), otherwise the GC may collect
        /// it and the native callback will crash.
        /// </remarks>
        internal static AttestationClientLib.LogFunc CreateLoggerBridge(ILoggerAdapter logger)
        {
            if (logger is null)
            {
                return ConsoleLogger;
            }

            return (ctx, tag, lvl, func, line, msg) =>
            {
                try
                {
                    LogLevel mapped = MapLevel(lvl);
                    if (!logger.IsLoggingEnabled(mapped))
                    {
                        return;
                    }

                    string lineText = FormatNativeLog(tag, lvl, func, line, msg);

                    // Native Error/Warning/Info lines (MAA policy-evaluation failures, endpoint
                    // URLs, TPM cert issuer) are operational diagnostics, not user PII, so they are
                    // logged as the scrubbed message and stay visible in standard MSAL logs. The
                    // most verbose native output (Debug -> Verbose, and any unknown level) can carry
                    // richer payload fragments, so it is routed through the PII slot and only
                    // surfaces when the caller opts into PII logging.
                    if (mapped == LogLevel.Verbose)
                    {
                        logger.Log(mapped, lineText, string.Empty);
                    }
                    else
                    {
                        logger.Log(mapped, string.Empty, lineText);
                    }
                }
                catch
                {
                    // A logging callback must never throw back into native code.
                }
            };
        }

        /// <summary>
        /// Maps a native <see cref="AttestationClientLib.LogLevel"/> to the MSAL <see cref="LogLevel"/>.
        /// Unknown values map to <see cref="LogLevel.Verbose"/>.
        /// </summary>
        internal static LogLevel MapLevel(AttestationClientLib.LogLevel lvl) => lvl switch
        {
            AttestationClientLib.LogLevel.Error => LogLevel.Error,
            AttestationClientLib.LogLevel.Warn => LogLevel.Warning,
            AttestationClientLib.LogLevel.Info => LogLevel.Info,
            AttestationClientLib.LogLevel.Debug => LogLevel.Verbose,
            _ => LogLevel.Verbose,
        };

        /// <summary>
        /// Formats a single native log line into a human-readable string, robust to null/IntPtr inputs.
        /// </summary>
        internal static string FormatNativeLog(string tag, AttestationClientLib.LogLevel lvl, string func, int line, string msg)
        {
            string sTag = ToText(tag);
            string sFunc = ToText(func);
            string sMsg = ToText(msg);

            return $"[MtlsPop][AttestationClientLib][{lvl}] {sTag} {sFunc}:{line}  {sMsg}";
        }

        // Converts either string or IntPtr (char*) to text. Works with any LogFunc variant.
        private static string ToText(object value)
        {
            if (value is IntPtr p && p != IntPtr.Zero)
            {
                try
                { return Marshal.PtrToStringAnsi(p) ?? string.Empty; }
                catch { return string.Empty; }
            }
            return value?.ToString() ?? string.Empty;
        }
    }
}
