﻿using System;
using System.Collections.Generic;
using System.Text;
using Hangfire.HttpJob.Agent.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Hangfire.HttpJob.Agent
{
    public static class JobAgentServiceCollectionExtensions
    {
        public static IServiceCollection AddHangfireHttpJobAgent(this IServiceCollection serviceCollection, Action<JobAgentServiceConfigurer> configure)
        {
            serviceCollection.AddOptions();
            serviceCollection.TryAddSingleton<IConfigureOptions<JobAgentOptions>, ConfigureJobAgentOptions>();
            var configurer = new JobAgentServiceConfigurer(serviceCollection);
            configure?.Invoke(configurer);
            serviceCollection.TryAddSingleton<JobAgentMiddleware>();
            return serviceCollection;

        }

        public static IServiceCollection AddHangfireHttpJobAgent(this IServiceCollection serviceCollection)
        {
            return serviceCollection.AddHangfireHttpJobAgent(null);
        }

    }
}