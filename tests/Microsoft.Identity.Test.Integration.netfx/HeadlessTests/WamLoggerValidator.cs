// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.IdentityModel.Abstractions;

/// <summary>
/// Performs validation on logs
/// </summary>
public class WamLoggerValidator : IIdentityLogger
{
    // string that is part of MSAL logs
    private const string MsalCppIdentifier = "[MSAL:000";

    /// <summary>
    /// Determines if any CPP log has been logged
    /// </summary>
    public virtual bool HasLogged
    { get; private set; }

    public virtual bool IsEnabled(EventLogLevel eventLogLevel) => true;

    /// <summary>
    /// Increases the logcount, if the log is from MsalCPP
    /// </summary>
    /// <param name="entry">Entry of log</param>
    public virtual void Log(LogEntry entry)
    {
        if (!HasLogged &&
            entry.Message.Contains(MsalCppIdentifier))
        {
            HasLogged= true;
        }
    }
}
