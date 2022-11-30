// <copyright file="HttpInListener.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using FastEndpoints.OpenTelemetry.Extensions;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Instrumentation;
using OpenTelemetry.Internal;
using OpenTelemetry.Trace;
#if !NETSTANDARD2_0
#endif

#if !NETSTANDARD2_0
#endif

namespace FastEndpoints.OpenTelemetry.Implementation
{
    internal class FastEndpointsListener : ListenerHandler
    {
        internal static readonly AssemblyName AssemblyName = typeof(FastEndpointsListener).Assembly.GetName();
        internal static readonly string ActivitySourceName = AssemblyName.Name;
        internal static readonly Version Version = AssemblyName.Version;
        internal static readonly ActivitySource ActivitySource = new(ActivitySourceName, Version.ToString());
        private const string DiagnosticSourceName = "FastEndpoints";

        private static readonly Func<HttpRequest, string, IEnumerable<string>> HttpRequestHeaderValuesGetter =
            (request, name) => request.Headers[name];

        private readonly PropertyFetcher<HttpContext> startContextFetcher = new("HttpContext");
        private readonly PropertyFetcher<EndpointDefinition> startEndpointDefinitionFetcher = new("EndpointDefinition");
        private readonly PropertyFetcher<HttpContext> stopContextFetcher = new("HttpContext");
        private readonly PropertyFetcher<EndpointDefinition> stopEndpointDefinitionFetcher = new("EndpointDefinition");
        private readonly PropertyFetcher<Exception> stopExceptionFetcher = new("Exception");
        private readonly PropertyFetcher<HttpContext> onValidationFailedContextFetcher = new("HttpContext");
        private readonly PropertyFetcher<EndpointDefinition> onValidationFailedEndpointDefinitionFetcher = new("EndpointDefinition");
        private readonly PropertyFetcher<List<ValidationFailure>> onValidationFailedValidationFailuresFetcher = new("ValidationFailures");
        private readonly FastEndpointsInstrumentationOptions options;

        public FastEndpointsListener(FastEndpointsInstrumentationOptions options)
            : base(DiagnosticSourceName)
        {
            Guard.ThrowIfNull(options);

            this.options = options;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
            Justification = "The objects should not be disposed.")]
        public override void OnStartActivity(Activity activity, object payload)
        {
            if (Sdk.SuppressInstrumentation)
            {
                return;
            }

            _ = this.startContextFetcher.TryFetch(payload, out HttpContext context);
            if (context == null)
            {
                FastEndpointsInstrumentationEventSource.Log.NullPayload(nameof(FastEndpointsListener),
                    nameof(this.OnStartActivity));
                return;
            }
            
            _ = this.startEndpointDefinitionFetcher.TryFetch(payload, out EndpointDefinition endpointDefinition);
            if (endpointDefinition == null)
            {
                FastEndpointsInstrumentationEventSource.Log.NullPayload(nameof(FastEndpointsListener),
                    nameof(this.OnStartActivity));
                return;
            }

            var request = context.Request;

            var activityOperationName = endpointDefinition.GetActivityName();
            Activity newOne = new Activity(activityOperationName);

            newOne.SetTag("IsCreatedByInstrumentation", bool.TrueString);

            // Starting the new activity make it the Activity.Current one.
            newOne.Start();

            activity = newOne;

            if (activity.IsAllDataRequested)
            {
                try
                {
                    if (this.options.Filter?.Invoke(context) == false)
                    {
                        FastEndpointsInstrumentationEventSource.Log.RequestIsFilteredOut(activity.OperationName);
                        activity.IsAllDataRequested = false;
                        activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
                        return;
                    }
                }
                catch (Exception ex)
                {
                    FastEndpointsInstrumentationEventSource.Log.RequestFilterException(ex);
                    activity.IsAllDataRequested = false;
                    activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
                    return;
                }

                ActivityInstrumentationHelper.SetActivitySourceProperty(activity, ActivitySource);
                ActivityInstrumentationHelper.SetKindProperty(activity, ActivityKind.Internal);

                activity.SetTag(SemanticConventions.AttributeFastEndpointsRoutes, endpointDefinition.Routes);
                activity.SetTag(SemanticConventions.AttributeFastEndpointsVerbs, endpointDefinition.Verbs);
                activity.SetTag(SemanticConventions.AttributeFastEndpointsSummary, endpointDefinition.EndpointSummary?.Summary);
                activity.SetTag(SemanticConventions.AttributeFastEndpointsDescription, endpointDefinition.EndpointSummary?.Description);
                activity.SetTag(SemanticConventions.AttributeFastEndpointsTags, endpointDefinition.EndpointTags);
                
                try
                {
                    this.options.Enrich?.Invoke(activity, "OnStartActivity", request);
                }
                catch (Exception ex)
                {
                    FastEndpointsInstrumentationEventSource.Log.EnrichmentException(ex);
                }
            }
        }

        public override void OnStopActivity(Activity activity, object payload)
        {
            if (activity.IsAllDataRequested)
            {
                _ = this.stopContextFetcher.TryFetch(payload, out HttpContext context);
                if (context == null)
                {
                    FastEndpointsInstrumentationEventSource.Log.NullPayload(nameof(FastEndpointsListener),
                        nameof(this.OnStopActivity));
                    return;
                }

                var response = context.Response;

                try
                {
                    this.options.Enrich?.Invoke(activity, "OnStopActivity", response);
                }
                catch (Exception ex)
                {
                    FastEndpointsInstrumentationEventSource.Log.EnrichmentException(ex);
                }
            }
            
            if (activity.TryCheckFirstTag("IsCreatedByInstrumentation", out var tagValue) &&
                ReferenceEquals(tagValue, bool.TrueString))
            {
                // If instrumentation started a new Activity, it must
                // be stopped here.
                activity.SetTag("IsCreatedByInstrumentation", null);
                activity.Stop();

                // Reset Activity.Current to AspNetCore
                Activity.Current = activity.Parent;
            }

            var textMapPropagator = Propagators.DefaultTextMapPropagator;
            if (textMapPropagator is not TraceContextPropagator)
            {
                Baggage.Current = default;
            }
        }

        public override void OnCustom(string name, Activity activity, object payload)
        {
            if (name == "FastEndpointsOnValidationFailed")
            {
                if (activity.IsAllDataRequested)
                {
                    // Taking reference on MVC will increase size of deployment for non-MVC apps.
                    _ = this.onValidationFailedValidationFailuresFetcher.TryFetch(payload, out var validationFailures);

                    if (validationFailures != null)
                    {
                        activity.SetTag(SemanticConventions.AttributeFastEndpointsValidationFailures, JsonSerializer.Serialize(validationFailures));
                    }
                }
            }
        }

        public override void OnException(Activity activity, object payload)
        {
            if (activity.IsAllDataRequested)
            {
                if (!this.stopExceptionFetcher.TryFetch(payload, out Exception exc) || exc == null)
                {
                    FastEndpointsInstrumentationEventSource.Log.NullPayload(nameof(FastEndpointsListener),
                        nameof(this.OnException));
                    return;
                }

                if (this.options.RecordException)
                {
                    activity.RecordException(exc);
                }

                activity.SetStatus(Status.Error.WithDescription(exc.Message));

                try
                {
                    this.options.Enrich?.Invoke(activity, "OnException", exc);
                }
                catch (Exception ex)
                {
                    FastEndpointsInstrumentationEventSource.Log.EnrichmentException(ex);
                }
            }
        }
    }
}