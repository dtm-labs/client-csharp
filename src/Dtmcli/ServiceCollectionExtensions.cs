using Microsoft.Extensions.DependencyInjection;
using System;

namespace Dtmcli
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDtmcli(this IServiceCollection services, Action<DtmOptions> setupAction)
        {
            if(setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            services.AddOptions();
            services.Configure(setupAction);

            //var options = new DtmOptions();
            //setupAction(options);

            //services.AddHttpClient("dtmClient", client =>
            //{
            //    client.BaseAddress = new Uri(options.DtmUrl);
            //    client.DefaultRequestHeaders.Add("Accept", "application/json");
            //})
            //.AddTypedClient<IDtmClient, DtmClient>();

            services.AddHttpClient(Constant.DtmClientHttpName, client =>
            {
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            });
            services.AddHttpClient(Constant.BranchClientHttpName);

            services.AddSingleton<IDtmClient, DtmClient>();
            services.AddSingleton<TccGlobalTransaction>();

            services.AddSingleton<IBranchBarrierFactory, DefaultBranchBarrierFactory>();
            return services;
        }
    }
}
