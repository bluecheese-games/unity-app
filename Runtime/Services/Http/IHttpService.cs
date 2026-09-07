using Cysharp.Threading.Tasks;

namespace BlueCheese.App
{
    public interface IHttpService
    {
		UniTask<IHttpResponse> GetAsync(IHttpRequest request);
		UniTask<IHttpResponse> PostAsync(IHttpRequest request);
		void RegisterMiddleware<T>(T middleware) where T : IHttpMiddleware;
	}
}
