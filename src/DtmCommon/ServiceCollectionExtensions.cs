using Microsoft.Extensions.DependencyInjection;

namespace DtmCommon
{
    public static class ServiceCollectionExtensions
    {
        internal static IServiceCollection AddDtmCommon(IServiceCollection services)
        {
            // barrier database relate
            services.AddSingleton<IDbSpecial, MysqlDBSpecial>();
            services.AddSingleton<IDbSpecial, PostgresDBSpecial>();
            services.AddSingleton<IDbSpecial, SqlServerDBSpecial>();
            services.AddSingleton<DbSpecialDelegate>();
            services.AddSingleton<DbUtils>();

            return services;
        }
    }
}