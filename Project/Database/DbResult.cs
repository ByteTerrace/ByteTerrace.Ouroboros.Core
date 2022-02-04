namespace ByteTerrace.Ouroboros.Core
{
    /// <summary>
    /// Represents the result of a database command.
    /// </summary>
    /// <param name="Parameters"></param>
    /// <param name="ResultCode"></param>
    public readonly record struct DbResult(
        IDictionary<string, DbParameter>? Parameters,
        int ResultCode
    )
    {
        /// <summary>
        /// Creates a new database result struct.
        /// </summary>
        /// <param name="resultCode"></param>
        /// <param name="parameters"></param>
        public static DbResult New(int resultCode, IList<DbParameter>? parameters = default) =>
            new(parameters?.ToDictionary(p => p.Name), resultCode);

        /// <summary>
        /// Returns the value of the specified parameter.
        /// </summary>
        /// <typeparam name="TValue">The type to cast the value to.</typeparam>
        /// <param name="parameterName">The name of the parameter.</param>
        public TValue? GetValue<TValue>(string parameterName) =>
            ((TValue?)Parameters?[parameterName].Value);
    }
}
