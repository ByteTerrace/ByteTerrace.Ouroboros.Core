using Microsoft.Extensions.DependencyInjection;

namespace ByteTerrace.Ouroboros.Database
{
    internal sealed class DefaultDbClientBuilder : IDbClientBuilder
    {
        public static DefaultDbClientBuilder New(
            string name,
            IServiceCollection services
        ) =>
            new(
                name: name,
                services: services
            );

        /// <inheritdoc />
        public string Name { get; }
        /// <inheritdoc />
        public IServiceCollection Services { get; }

        private DefaultDbClientBuilder(
            string name,
            IServiceCollection services
        ) {
            Name = name;
            Services = services;
        }
    }
}
