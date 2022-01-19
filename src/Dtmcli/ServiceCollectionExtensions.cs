using Microsoft.Extensions.DependencyInjection;
using System;

namespace Dtmcli
{
    public static class ServiceCollectionExtensions
    {
        internal static IServiceCollection ServiceCollection;


        public static IServiceCollection AddDtmcli(this IServiceCollection services, Action<DtmOptions> setupAction)
        {
            if(setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }
            var options = new DtmOptions();
            setupAction(options);
            ServiceCollection = services;
            services.AddHttpClient("dtmClient", client =>
            {
                client.BaseAddress = new Uri(options.DtmUrl);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            })
            .AddTypedClient<IDtmClient, DtmClient>();

            services.AddSingleton<TccGlobalTransaction>();

            // 子事务屏障
            services.AddSingleton<IBranchBarrierFactory, DefaultBranchBarrierFactory>();
            return services;
        }
    }
}
