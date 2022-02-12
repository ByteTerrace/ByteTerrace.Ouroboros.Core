namespace ByteTerrace.Ouroboros.Http
{
    public readonly record struct HttpPostOperation<TResult>(
        Func<HttpResponseMessage, ValueTask<TResult>> Callback,
        HttpCompletionOption CompletionOption,
        Stream ContentStream,
        bool IsContentOwner,
        string Uri
    )
    {
        public static implicit operator HttpOperation<TResult>(HttpPostOperation<TResult> operation) =>
            operation.ToHttpOperation();

        public static HttpPostOperation<TResult> New(
            Func<HttpResponseMessage, ValueTask<TResult>> callback,
            Stream contentStream,
            string uri,
            HttpCompletionOption completionOption = HttpCompletionOption.ResponseHeadersRead,
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
                method: HttpMethod.Post,
                uri: Uri
            );
    }
}
