//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System.Threading.Tasks;

namespace BlueCheese.App
{
    public interface IHttpService
    {
        Task<IHttpResponse> GetAsync(IHttpRequest request);
        Task<IHttpResponse> PostAsync(IHttpRequest request);
		void RegisterMiddleware<T>(T middleware) where T : IHttpMiddleware;
	}
}
