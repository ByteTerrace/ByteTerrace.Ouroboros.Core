namespace ByteTerrace.Ouroboros.Database
{
    /// <summary>
    /// Represents the result of a database command.
    /// </summary>
    /// <param name="Parameters">The parameters returned by the stored procedure.</param>
    /// <param name="ResultCode">The result code returned by the stored procedure.</param>
    public readonly record struct DbResult(
        IDictionary<string, DbParameter>? Parameters,
        int ResultCode
    )
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DbResult"/> struct.
        /// </summary>
        /// <param name="parameters">The parameters returned by the stored procedure.</param>
        /// <param name="resultCode">The result code returned by the stored procedure.</param>
        public static DbResult New(
            int resultCode,
            IReadOnlyList<DbParameter>? parameters = default
        ) =>
            new(
                Parameters: parameters?.ToDictionary(p => p.Name),
                ResultCode: resultCode
            );

        /// <summary>
        /// Returns the value of the specified parameter.
        /// </summary>
        /// <typeparam name="TValue">The type to cast the value to.</typeparam>
        /// <param name="parameterName">The name of the parameter.</param>
        public TValue? GetValue<TValue>(string parameterName) =>
            ((TValue?)Parameters?[parameterName].Value);
    }
}
