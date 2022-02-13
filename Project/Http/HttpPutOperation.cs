namespace ByteTerrace.Ouroboros.Http
{
    public readonly record struct HttpPutOperation<TResult>(
        Func<HttpResponseMessage, ValueTask<TResult>> Callback,
        HttpCompletionOption CompletionOption,
        Stream ContentStream,
        bool IsContentOwner,
        string Uri
    )
    {
        public static implicit operator HttpOperation<TResult>(HttpPutOperation<TResult> operation) =>
            operation.ToHttpOperation();

        public static HttpPutOperation<TResult> New(
            Func<HttpResponseMessage, ValueTask<TResult>> callback,
            Stream contentStream,
            string uri,
            HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead,
            bool isContentOwner = true
        ) =>
            new(
                Callback: callback,
                CompletionOption: completionOption,
                ContentStream: contentStream,
                IsContentOwner: isContentOwner,
                Uri: uri
            );

        public HttpOperation<TResult> ToHttpOperation() =>
            HttpOperation<TResult>.New(
                callback: Callback,
                completionOption: CompletionOption,
                contentStream: ContentStream,
                isContentOwner: IsContentOwner,
                method: HttpMethod.Put,
                uri: Uri
            );
    }
}
