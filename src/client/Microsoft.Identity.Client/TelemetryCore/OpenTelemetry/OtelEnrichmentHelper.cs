// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Extensibility;

namespace Microsoft.Identity.Client.TelemetryCore.OpenTelemetry
{
    internal static class OtelEnrichmentHelper
    {
        /// <summary>
        /// Invokes the caller-supplied OTel tags enricher exactly once per acquisition and returns the
        /// materialized set of extra tags. The same fixed set is then merged into every instrument's
        /// canonical base tags, so the delegate runs once (not once per metric) and a throwing enricher
        /// logs at most one warning per acquisition.
        /// </summary>
        /// <param name="tagsEnricher">The caller-supplied enricher delegate, or <c>null</c> when none is configured.</param>
        /// <param name="executionResultFactory">
        /// Factory for the <see cref="ExecutionResult"/> passed to the enricher. Invoked only when an enricher
        /// is configured, so the result is not allocated on the no-enricher hot path.
        /// </param>
        /// <param name="logger">Logger used to emit the single warning if the enricher throws.</param>
        /// <returns>
        /// The materialized extra tags, or <c>null</c> when no enricher is configured or the enricher threw
        /// (in which case any partial mutations are discarded and only the canonical base tags are recorded).
        /// </returns>
        public static IReadOnlyList<KeyValuePair<string, object>> MaterializeExtraTags(
            Action<ExecutionResult, IList<KeyValuePair<string, object>>> tagsEnricher,
            Func<ExecutionResult> executionResultFactory,
            ILoggerAdapter logger)
        {
            if (tagsEnricher == null)
            {
                return null;
            }

            var extraTags = new List<KeyValuePair<string, object>>();
            try
            {
                tagsEnricher(executionResultFactory(), extraTags);
            }
            catch (Exception ex)
            {
                // A caller-supplied enricher must never break telemetry recording or the auth flow.
                // On failure, log a single warning and discard any partial mutations.
                logger?.WarningPii(
                    $"[OpenTelemetry] The OTel tags enricher threw an exception and was ignored. {ex}",
                    "[OpenTelemetry] The OTel tags enricher threw an exception and was ignored.");
                return null;
            }

            // Return a snapshot, not the list handed to the enricher. A misbehaving enricher that retained the
            // reference (e.g. via a captured field or a fire-and-forget task) cannot then mutate the set we
            // enumerate while recording metrics, which would otherwise risk a "Collection was modified" throw
            // on the auth path. Empty -> null so the no-tags case allocates nothing (BuildTagList treats both alike).
            return extraTags.Count == 0 ? null : extraTags.ToArray();
        }
    }
}
