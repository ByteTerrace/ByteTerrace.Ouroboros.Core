using Microsoft.Extensions.DependencyInjection;

namespace ByteTerrace.Ouroboros.Http
{
    /// <summary>
    /// A collection of extension methods that directly or indirectly augment the <see cref="IServiceCollection"/> class.
    /// </summary>
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the <see cref="IHttpClientFactory"/> and related services to the collection and configures a binding between <see cref="IHttpClient"/> and the specified name.
        /// </summary>
        /// <param name="services">The collection of services that the client will be appended to.</param>
        /// <param name="configureClient">A delegate that configures the client after instantiation.</param>
        /// <param name="name">The name of the client.</param>
        public static IHttpClientBuilder AddGenericHttpClient(
            this IServiceCollection services,
            Action<HttpClient> configureClient,
            string name
        ) =>
            services.AddHttpClient<IHttpClient, GenericHttpClient>(
                configureClient: configureClient,
                name: name
            );
    }
}
