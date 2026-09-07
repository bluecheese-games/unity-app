namespace BlueCheese.App
{
    public interface IHttpMiddleware
    {
        void HandleRequest(IHttpRequest request);
        void HandleResponse(IHttpResponse response);
    }
}
