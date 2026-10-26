// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;

namespace Microsoft.Identity.Test.Performance
{
    public sealed class RetainedMemoryDiagnoser : IDiagnoser
    {
        private readonly Dictionary<BenchmarkCase, string> _resultPaths = new Dictionary<BenchmarkCase, string>();
        private readonly Dictionary<BenchmarkCase, double> _results = new Dictionary<BenchmarkCase, double>();

        public IEnumerable<string> Ids => new[] { nameof(RetainedMemoryDiagnoser) };
        public IEnumerable<IExporter> Exporters => Array.Empty<IExporter>();
        public IEnumerable<IAnalyser> Analysers => Array.Empty<IAnalyser>();

        public BenchmarkDotNet.Diagnosers.RunMode GetRunMode(BenchmarkCase benchmarkCase)
        {
            return BenchmarkDotNet.Diagnosers.RunMode.ExtraRun;
        }

        public void Handle(HostSignal signal, DiagnoserActionParameters parameters)
        {
            if (signal == HostSignal.BeforeAnythingElse)
            {
                string path = GetResultPath(parameters.Process);
                using (File.Open(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                }
                _resultPaths.Add(parameters.BenchmarkCase, path);
            }
            else if (signal == HostSignal.AfterAll)
            {
                string path = _resultPaths[parameters.BenchmarkCase];
                try
                {
                    string text = File.ReadAllText(path);
                    if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double bytes) ||
                        double.IsNaN(bytes) || double.IsInfinity(bytes) || bytes <= 0)
                    {
                        throw new InvalidOperationException("The retention diagnostic process did not produce a valid retained-memory measurement.");
                    }
                    _results.Add(parameters.BenchmarkCase, bytes);
                }
                finally
                {
                    File.Delete(path);
                    _resultPaths.Remove(parameters.BenchmarkCase);
                }
            }
            else if (signal == HostSignal.AfterProcessExit &&
                _resultPaths.TryGetValue(parameters.BenchmarkCase, out string pendingPath))
            {
                File.Delete(pendingPath);
                _resultPaths.Remove(parameters.BenchmarkCase);
            }
        }

        internal static string GetResultPath(Process process)
        {
            string identity = process.Id.ToString(CultureInfo.InvariantCulture) + "-" +
                process.StartTime.ToUniversalTime().Ticks.ToString(CultureInfo.InvariantCulture);
            return Path.Combine(Path.GetTempPath(), "msal-retention-" + identity + ".txt");
        }

        public IEnumerable<Metric> ProcessResults(DiagnoserResults results)
        {
            if (!_results.TryGetValue(results.BenchmarkCase, out double bytes))
            {
                throw new InvalidOperationException("The retention diagnostic run did not complete its retained-memory measurement.");
            }
            _results.Remove(results.BenchmarkCase);
            yield return new Metric(new RetainedBytesDescriptor(), bytes);
        }

        public void DisplayResults(ILogger logger)
        {
        }

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
        {
            return Array.Empty<ValidationError>();
        }

        private sealed class RetainedBytesDescriptor : IMetricDescriptor
        {
            public string Id => "RetainedBytesPerPartition";
            public string DisplayName => "Retained/partition";
            public string Legend => "Estimated live managed bytes per retained partition, including its storage slot; median of three full-GC heap deltas in an extra diagnostic run.";
            public string NumberFormat => "N2";
            public UnitType UnitType => UnitType.Size;
            public string Unit => "B";
            public bool TheGreaterTheBetter => false;
            public int PriorityInCategory => 0;
            public bool GetIsAvailable(Metric metric) => true;
        }
    }
}
