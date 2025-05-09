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

            // AddHttpClient(services);

            return services;
        }

        public static IServiceCollection AddDtmWorkflow(this IServiceCollection services, IConfiguration configuration, string sectionName = "dtm")
        {
            services.AddDtmcli(configuration, sectionName);
            services.AddDtmGrpc(configuration, sectionName);

            services.TryAddSingleton<IWorkflowFactory, WorkflowFactory>();
            services.TryAddSingleton<WorkflowGlobalTransaction>();
                       
            // AddHttpClient(services);

            return services;
        }

        // private static void AddHttpClient(IServiceCollection services /*, DtmOptions options*/)
        // {
        //     services.AddHttpClient(Dtmcli.Constant.WorkflowBranchClientHttpName, client =>
        //     {
        //         // TODO DtmOptions
        //         // client.Timeout = TimeSpan.FromMilliseconds(options.BranchTimeout);                
        //     }).AddHttpMessageHandler<WorkflowHttpInterceptor>();
        //
        //     // TODO how to inject workflow instance?
        //     services.AddTransient<WorkflowHttpInterceptor>();
        // }
    }
}