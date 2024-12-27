using Dtmcli;
using DtmCommon;
using Dtmgrpc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Dtmworkflow
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDtmWorkflow(this IServiceCollection services, Action<DtmOptions> setupAction)
        {
            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            services.AddDtmcli(setupAction);
            services.AddDtmGrpc(setupAction);

            services.TryAddSingleton<IWorkflowFactory, WorkflowFactory>();
            services.TryAddSingleton<WorkflowGlobalTransaction>();

            return services;
        }

        public static IServiceCollection AddDtmWorkflow(this IServiceCollection services, IConfiguration configuration, string sectionName = "dtm")
        {
            services.AddDtmcli(configuration, sectionName);
            services.AddDtmGrpc(configuration, sectionName);

            services.TryAddSingleton<IWorkflowFactory, WorkflowFactory>();
            services.TryAddSingleton<WorkflowGlobalTransaction>();

            return services;
        }
    }
}
