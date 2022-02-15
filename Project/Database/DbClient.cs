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

        private DbCommandBuilder? m_commandBuilder;
        private DbConnection? m_connection;

        /// <inheritdoc />
        public DbCommandBuilder CommandBuilder {
            get {
                if (m_commandBuilder is null) {
                    ThrowHelper.ThrowObjectDisposedException(objectName: nameof(CommandBuilder));
                }

                return m_commandBuilder;
            }
            init {
                m_commandBuilder = value;
            }
        }
        /// <inheritdoc />
        public DbConnection Connection {
            get {
                if (m_connection is null) {
                    ThrowHelper.ThrowObjectDisposedException(objectName: nameof(Connection));
                }

                return m_connection;
            }
            init {
                m_connection = value;
            }
        }
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
        public DbClient() {
            CommandBuilder = NullDbCommandBuilder.Instance;
            Connection = NullDbConnection.Instance;
            IsDisposed = false;
            Logger = NullLogger.Instance;
            OwnsConnection = true;
            ProviderFactory = NullDbProviderFactory.Instance;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="DbClient"/> class.
        /// </summary>
        /// <param name="options">The options that will be used to configure the database client.</param>
        public DbClient(DbClientOptions options) {
            var connection = options.Connection;
            var logger = options.Logger;
            var ownsConnection = options.OwnsConnection;
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

            if (!providerFactory.CanCreateCommandBuilder) {
                ThrowHelper.ThrowNotSupportedException(message: "Unable to construct a command builder from the specified provider factory.");
            }

            m_commandBuilder = providerFactory.CreateCommandBuilder();
            m_connection = connection;

            IsDisposed = false;
            Logger = logger;
            OwnsConnection = ownsConnection;
            ProviderFactory = providerFactory;
        }

        /// <summary>
        /// Releases all resources used by this <see cref="DbClient"/> instance.
        /// </summary>
        /// <param name="isDisposing">Indicates whether the managed resources owned by this database client should be disposed.</param>
        protected virtual void Dispose(bool isDisposing) {
            if (!IsDisposed) {
                if (isDisposing) {
                    CommandBuilder.Dispose();

                    if (OwnsConnection) {
                        Connection.Dispose();
                    }
                }

                m_commandBuilder = null;
                m_connection = null;

                IsDisposed = true;
            }
        }
        /// <summary>
        /// Releases all resources used by this <see cref="DbClient"/> instance asynchronously.
        /// </summary>
        protected async virtual ValueTask DisposeAsyncCore() {
            if (!IsDisposed) {
                CommandBuilder.Dispose();

                if (OwnsConnection) {
                    await Connection
                        .DisposeAsync()
                        .ConfigureAwait(continueOnCapturedContext: false);
                }
            }
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
