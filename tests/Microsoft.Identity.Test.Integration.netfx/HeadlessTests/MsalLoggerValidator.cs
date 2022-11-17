// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.IdentityModel.Abstractions;

/// <summary>
/// Performs validation on logs
/// </summary>
public class MsalLoggerValidator : IIdentityLogger
{
    // string that i tpart of MSAL logs
    private const string MSALCPPIdentifier = "[MSAL:000";

    // count of MsalCPP logs
    private int MsalCPPLogCount { get; set; }

    /// <summary>
    /// Determines if any CPP log has been logged
    /// </summary>
    public virtual bool HasLogged => MsalCPPLogCount > 0;

    public virtual bool IsEnabled(EventLogLevel eventLogLevel) => true;

    /// <summary>
    /// Increases the logcount, if the log is from MsalCPP
    /// </summary>
    /// <param name="entry">Entry of log</param>
    public virtual void Log(LogEntry entry)
    {
        if (entry.Message.Contains(MSALCPPIdentifier))
        {
            MsalCPPLogCount++;
        }
    }
}
