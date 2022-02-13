using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Dtmcli
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDtmcli(this IServiceCollection services, Action<DtmOptions> setupAction)
        {
            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            services.AddOptions();
            services.Configure(setupAction);

            return AddDtmcliCore(services);
        }

        public static IServiceCollection AddDtmcli(this IServiceCollection services, IConfiguration configuration, string sectionName = "dtm")
        {
            services.Configure<DtmOptions>(configuration.GetSection(sectionName));

            return AddDtmcliCore(services);
        }

        private static IServiceCollection AddDtmcliCore(IServiceCollection services)
        {
            AddHttpClient(services);
            AddDtmCore(services);

            return services;
        }

        private static void AddHttpClient(IServiceCollection services)
        {
            services.AddHttpClient(Constant.DtmClientHttpName, client =>
            {
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            });
            services.AddHttpClient(Constant.BranchClientHttpName);
        }

        private static void AddDtmCore(IServiceCollection services)
        {
            services.AddSingleton<IDtmClient, DtmClient>();
            services.AddSingleton<TccGlobalTransaction>();

            services.AddSingleton<IBranchBarrierFactory, DefaultBranchBarrierFactory>();
        }
    }
}