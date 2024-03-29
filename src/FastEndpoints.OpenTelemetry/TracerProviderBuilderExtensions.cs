// <copyright file="TracerProviderBuilderExtensions.cs" company="OpenTelemetry Authors">
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

using FastEndpoints.OpenTelemetry.Implementation;
using OpenTelemetry.Internal;
using OpenTelemetry.Trace;

namespace FastEndpoints.OpenTelemetry
{
    /// <summary>
    /// Extension methods to simplify registering of ASP.NET Core request instrumentation.
    /// </summary>
    public static class TracerProviderBuilderExtensions
    {
        /// <summary>
        /// Enables the incoming requests automatic data collection for ASP.NET Core.
        /// </summary>
        /// <param name="builder"><see cref="TracerProviderBuilder"/> being configured.</param>
        /// <param name="configureAspNetCoreInstrumentationOptions">ASP.NET Core Request configuration options.</param>
        /// <returns>The instance of <see cref="TracerProviderBuilder"/> to chain the calls.</returns>
        public static TracerProviderBuilder AddFastEndpointsInstrumentation(
            this TracerProviderBuilder builder,
            Action<FastEndpointsInstrumentationOptions> configureAspNetCoreInstrumentationOptions = null)
        {
            Guard.ThrowIfNull(builder);

            if (builder is IDeferredTracerProviderBuilder deferredTracerProviderBuilder)
            {
                return deferredTracerProviderBuilder.Configure((sp, builder) =>
                {
                    AddFastEndpointsInstrumentation(builder, sp.GetOptions<FastEndpointsInstrumentationOptions>(), configureAspNetCoreInstrumentationOptions);
                });
            }

            return AddFastEndpointsInstrumentation(builder, new FastEndpointsInstrumentationOptions(), configureAspNetCoreInstrumentationOptions);
        }

        internal static TracerProviderBuilder AddFastEndpointsInstrumentation(
            this TracerProviderBuilder builder,
            FastEndpointsInstrumentation instrumentation)
        {
            builder.AddSource(FastEndpointsListener.ActivitySourceName);
            // builder.AddLegacySource(FastEndpointsListener.ActivityOperationName); // for the activities created by AspNetCore
            return builder.AddInstrumentation(() => instrumentation);
        }

        private static TracerProviderBuilder AddFastEndpointsInstrumentation(
            TracerProviderBuilder builder,
            FastEndpointsInstrumentationOptions options,
            Action<FastEndpointsInstrumentationOptions> configure = null)
        {
            configure?.Invoke(options);
            return AddFastEndpointsInstrumentation(
                builder,
                new FastEndpointsInstrumentation(new FastEndpointsListener(options)));
        }
    }
}
