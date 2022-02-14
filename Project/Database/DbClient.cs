using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Toolkit.Diagnostics;
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

        /// <inheritdoc />
        public DbCommandBuilder CommandBuilder { get; init; }
        /// <inheritdoc />
        public DbConnection Connection { get; init; }
        /// <inheritdoc />
        public bool IsDisposed { get; private set; }
        /// <inheritdoc />
        public ILogger Logger { get; init; }
        /// <inheritdoc />
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
                ThrowHelper.ThrowArgumentNullException(name: $"{nameof(options)}.{nameof(Connection)}");
            }

            if (providerFactory is null) {
                ThrowHelper.ThrowArgumentNullException(name: $"{nameof(options)}.{nameof(ProviderFactory)}");
            }

            if (logger is null) {
                logger = NullLogger.Instance;
            }

            CommandBuilder = (providerFactory.CreateCommandBuilder() ?? ThrowHelper.ThrowNotSupportedException<DbCommandBuilder>(message: "Unable to construct a command builder from the specified provider factory."));
            Connection = connection;
            IsDisposed = false;
            Logger = logger;
            OwnsConnection = options.OwnsConnection;
            ProviderFactory = providerFactory;
        }

        /// <summary>
        /// Releases all resources used by this <see cref="DbClient"/> instance.
        /// </summary>
        protected virtual void Dispose(bool isDisposing) {
            if (!IsDisposed) {
                if (isDisposing && OwnsConnection) {
                    CommandBuilder.Dispose();
                    Connection.Dispose();
                }

                IsDisposed = true;
            }
        }
        /// <summary>
        /// Releases all resources used by this <see cref="DbClient"/> instance.
        /// </summary>
        protected async virtual ValueTask DisposeAsyncCore() {
            if (!IsDisposed && OwnsConnection) {
                CommandBuilder.Dispose();
                await Connection
                    .DisposeAsync()
                    .ConfigureAwait(continueOnCapturedContext: false);
            }

            IsDisposed = true;
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
