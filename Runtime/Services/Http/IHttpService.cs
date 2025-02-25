//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

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
