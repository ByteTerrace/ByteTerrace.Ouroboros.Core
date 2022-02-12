namespace ByteTerrace.Ouroboros.Http
{
    public readonly record struct HttpGetOperation<TResult>(
        Func<HttpResponseMessage, ValueTask<TResult>> Callback,
        HttpCompletionOption CompletionOption,
        string Uri
    )
    {
        public static implicit operator HttpOperation<TResult>(HttpGetOperation<TResult> operation) =>
            operation.ToHttpOperation();

        public static HttpGetOperation<TResult> New(
            Func<HttpResponseMessage, ValueTask<TResult>> callback,
            string uri,
            HttpCompletionOption completionOption = HttpCompletionOption.ResponseHeadersRead
        ) =>
            new(
                Callback: callback,
                CompletionOption: completionOption,
                Uri: uri
            );

        public HttpOperation<TResult> ToHttpOperation() =>
            HttpOperation<TResult>.New(
                callback: Callback,
                completionOption: CompletionOption,
                contentStream: default,
                method: HttpMethod.Get,
                uri: Uri
            );
    }
}
