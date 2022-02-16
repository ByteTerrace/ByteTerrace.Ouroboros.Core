using Microsoft.Extensions.DependencyInjection;

namespace ByteTerrace.Ouroboros.Database
{
    internal sealed class DbClientBuilder : IDbClientBuilder
    {
        public static DbClientBuilder New(
            string name,
            IServiceCollection services
        ) =>
            new(
                name: name,
                services: services
            );

        public string Name { get; }
        public IServiceCollection Services { get; }

        private DbClientBuilder(
            string name,
            IServiceCollection services
        ) {
            Name = name;
            Services = services;
        }
    }
}
