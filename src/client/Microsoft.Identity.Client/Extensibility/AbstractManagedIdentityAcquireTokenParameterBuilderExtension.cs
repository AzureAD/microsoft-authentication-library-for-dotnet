// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.Identity.Client.Extensibility
{
    /// <summary>
    /// Extension methods for managed identity acquire-token requests
    /// (<see cref="AbstractManagedIdentityAcquireTokenParameterBuilder{T}"/>).
    /// </summary>
    public static class AbstractManagedIdentityAcquireTokenParameterBuilderExtension
    {
        /// <summary>
        /// Registers a delegate that adds additional tags (dimensions) to the OpenTelemetry metrics MSAL emits
        /// for this managed identity token acquisition. The delegate is invoked while MSAL records its metrics and
        /// receives the <see cref="ExecutionResult"/> of the acquisition (indicating success or failure, with the
        /// result or exception) together with a mutable list of tags. Tags appended to that list are attached to
        /// every metric recorded for the request, including metrics emitted during proactive background refresh.
        /// </summary>
        /// <typeparam name="T">The concrete managed identity builder type.</typeparam>
        /// <param name="builder">The builder to chain options to.</param>
        /// <param name="tagsEnricher">
        /// A delegate that receives the <see cref="ExecutionResult"/> and a mutable list of tags to enrich.
        /// The delegate runs on MSAL's metric-recording path, so it should be fast, non-blocking and must not throw.
        /// The supplied tag list must be populated synchronously; do not retain or mutate it after the delegate returns.
        /// </param>
        /// <returns>The builder to chain the .With methods.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="tagsEnricher"/> is null.</exception>
        /// <remarks>
        /// Keep both the number of added tags and — more importantly — their value cardinality low. High-cardinality
        /// tag values (such as correlation ids, timestamps, or user identifiers) can cause an unbounded number of
        /// metric time series in the downstream telemetry backend. The tags are applied to every metric MSAL records
        /// for the request, so a large number of tags also adds overhead on the metric-recording path.
        /// </remarks>
        public static AbstractManagedIdentityAcquireTokenParameterBuilder<T> WithOtelTagsEnricher<T>(
            this AbstractManagedIdentityAcquireTokenParameterBuilder<T> builder,
            Action<ExecutionResult, IList<KeyValuePair<string, object>>> tagsEnricher)
            where T : AbstractManagedIdentityAcquireTokenParameterBuilder<T>
        {
            if (tagsEnricher == null)
            {
                throw new ArgumentNullException(nameof(tagsEnricher));
            }

            builder.CommonParameters.OtelTagsEnricher = tagsEnricher;

            return builder;
        }
    }
}
