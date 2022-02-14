using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Data.Common;

namespace ByteTerrace.Ouroboros.Database
{
    /// <summary>
    /// Provides a minimal implementation of the <see cref="IDbClient"/> interface.
    /// </summary>
    public class DbClient : IDbClient
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DbClient"/> class.
        /// </summary>
        /// <param name="options">The options that will be used to configure the database client.</param>
        public static DbClient New(DbClientOptions options) =>
            new(options: options);

        private bool m_isDisposed;

        /// <inheritdoc />
        public DbCommandBuilder CommandBuilder { get; init; }
        /// <inheritdoc />
        public DbConnection Connection { get; init; }
        /// <inheritdoc />
        public ILogger Logger { get; init; }
        /// <summary>
        /// Indicates whether this client owns the underlying database connection.
        /// </summary>
        public bool OwnsConnection { get; init; }
        /// <inheritdoc />
        public DbProviderFactory ProviderFactory { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DbClient"/> class.
        /// </summary>
        /// <param name="options">The options that will be used to configure the database client.</param>
        protected DbClient(DbClientOptions options) {
            var connection = options.Connection;
            var logger = options.Logger;
            var providerFactory = options.ProviderFactory;

            if (connection is null) {
                throw new NullReferenceException();
            }

            if (providerFactory is null) {
                throw new NullReferenceException();
            }

            if (logger is null) {
                logger = NullLogger.Instance;
            }

            CommandBuilder = (providerFactory.CreateCommandBuilder() ?? throw new NullReferenceException(message: "Unable to construct a command builder from the specified provider factory."));
            Connection = connection;
            Logger = logger;
            OwnsConnection = options.OwnsConnection;
            ProviderFactory = providerFactory;
        }

        /// <summary>
        /// Releases all resources used by this <see cref="DbClient"/> instance.
        /// </summary>
        protected virtual void Dispose(bool isDisposing) {
            if (!m_isDisposed) {
                if (isDisposing && OwnsConnection) {
                    CommandBuilder.Dispose();
                    Connection.Dispose();
                }

                m_isDisposed = true;
            }
        }
        /// <summary>
        /// Releases all resources used by this <see cref="DbClient"/> instance.
        /// </summary>
        protected async virtual ValueTask DisposeAsyncCore() {
            if (!m_isDisposed && OwnsConnection) {
                CommandBuilder.Dispose();
                await Connection
                    .DisposeAsync()
                    .ConfigureAwait(continueOnCapturedContext: false);
            }

            m_isDisposed = true;
        }

        /// <inheritdoc />
        public void Dispose() {
            Dispose(isDisposing: true);
            GC.SuppressFinalize(obj: this);
        }
        /// <inheritdoc />
        public async ValueTask DisposeAsync() {
            await DisposeAsyncCore().ConfigureAwait(continueOnCapturedContext: false);
            Dispose(isDisposing: false);
            GC.SuppressFinalize(obj: this);
        }
        /// <summary>
        /// Convert this instance to the <see cref="IDbClient"/> interface.
        /// </summary>
        public IDbClient ToIDbClient() =>
            this;
    }
}
