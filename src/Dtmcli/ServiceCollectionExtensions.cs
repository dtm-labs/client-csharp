using DtmCommon;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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

            var op = new DtmOptions();
            setupAction(op);

            return AddDtmcliCore(services, op);
        }

        public static IServiceCollection AddDtmcli(this IServiceCollection services, IConfiguration configuration, string sectionName = "dtm")
        {
            services.Configure<DtmOptions>(configuration.GetSection(sectionName));

            var op = configuration.GetSection(sectionName).Get<DtmOptions>() ?? new DtmOptions();

            return AddDtmcliCore(services, op);
        }

        private static IServiceCollection AddDtmcliCore(IServiceCollection services, DtmOptions options)
        {
            AddHttpClient(services, options);
            AddDtmCore(services);

            return services;
        }

        private static void AddHttpClient(IServiceCollection services, DtmOptions options)
        {
            services.AddHttpClient(Constant.DtmClientHttpName, client =>
            {
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.Timeout = TimeSpan.FromMilliseconds(options.DtmTimeout);
            });
            services.AddHttpClient(Constant.BranchClientHttpName, client =>
            {
                client.Timeout = TimeSpan.FromMilliseconds(options.BranchTimeout);
            });
        }

        private static void AddDtmCore(IServiceCollection services)
        {
            // trans releate
            services.TryAddSingleton<IDtmTransFactory, DtmTransFactory>();
            services.TryAddSingleton<IDtmClient, DtmClient>();
            services.TryAddSingleton<TccGlobalTransaction>();

            DtmCommon.ServiceCollectionExtensions.AddDtmCommon(services);

            // barrier factory
            services.TryAddSingleton<IBranchBarrierFactory, DefaultBranchBarrierFactory>();
        }
    }
}
