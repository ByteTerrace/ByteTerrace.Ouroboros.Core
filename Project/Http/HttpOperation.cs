namespace ByteTerrace.Ouroboros.Http
{
    public readonly record struct HttpOperation<TResult>(
        Func<HttpResponseMessage, ValueTask<TResult>> Callback,
        HttpCompletionOption CompletionOption,
        Stream? ContentStream,
        bool IsContentOwner,
        HttpMethod Method,
        string Uri
    )
    {
        public static HttpOperation<TResult> New(
            Func<HttpResponseMessage, ValueTask<TResult>> callback,
            Stream? contentStream,
            HttpMethod method,
            string uri,
            HttpCompletionOption completionOption = HttpCompletionOption.ResponseHeadersRead,
            bool isContentOwner = true
        ) =>
            new(
                Callback: callback,
                ContentStream: contentStream,
                CompletionOption: completionOption,
                IsContentOwner: isContentOwner,
                Method: method,
                Uri: uri
            );
    }
}
