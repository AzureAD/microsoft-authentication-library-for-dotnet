// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CacheExtension
{
    public class TraceStringListener : TraceListener
    {
        private const string TraceSourceName = "TestSource";

        public static (TraceSource, TraceStringListener) Create()
        {
            var logger = new TraceSource(TraceSourceName, SourceLevels.All);
            var listner = new TraceStringListener();

            logger.Listeners.Add(listner);

            return (logger, listner);
        }

        private readonly StringBuilder _log = new StringBuilder();

        public string CurrentLog => _log.ToString();

        public override void Write(string message)
        {
            _log.Append(FormatLogMessage(message));
        }

        public override void WriteLine(string message)
        {
            _log.AppendLine(FormatLogMessage(message));
        }

        public void AssertContains(string needle)
        {
            Assert.IsTrue(CurrentLog.Contains(needle));
        }

        private static string FormatLogMessage(string message)
        {
            return $"[TEST][{DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)}] {message}";
        }

        public void AssertContainsError(string needle)
        {
            AssertContains(TraceSourceName + " error");
            AssertContains(needle);
        }
    }
}
