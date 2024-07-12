//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using BlueCheese.Core.ServiceLocator;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace BlueCheese.App
{
	public class HttpService : IHttpService
	{
		public struct Options : IOptions
		{
			public bool LogRequests;
			public Uri BaseUri;
			public List<IHttpMiddleware> Middlewares;

			public Options UseMiddleware<T>(T middleware) where T : IHttpMiddleware
			{
				Middlewares ??= new List<IHttpMiddleware>();
				Middlewares.Add(middleware);
				return this;
			}
		}

		private readonly IHttpClient _httpClient;
		private readonly ILogger<HttpService> _logger;
		private readonly Options _options = default;
		private readonly List<IHttpMiddleware> _middlewares = new();

		public HttpService(Options options, IHttpClient httpClient, ILogger<HttpService> loggerService)
		{
			_options = options;
			_httpClient = httpClient;
			_logger = loggerService;

			if (_options.Middlewares != null)
			{
				_middlewares.AddRange(_options.Middlewares);
			}
		}

		public async Task<IHttpResponse> GetAsync(IHttpRequest request) => await ProcessRequestAsync(request, HttpMethod.Get);

		public async Task<IHttpResponse> PostAsync(IHttpRequest request) => await ProcessRequestAsync(request, HttpMethod.Post);

		private async Task<IHttpResponse> ProcessRequestAsync(IHttpRequest request, HttpMethod method)
		{
			if (!request.TryGetUri(_options.BaseUri, out var uri))
			{
				return HttpResponse.Failure($"Bad Url: {request.Url}", HttpStatusCode.BadRequest);
			}

			_middlewares?.ForEach(middleware => middleware.HandleRequest(request));

			if (_options.LogRequests)
			{
				_logger.Log($"{method}: {uri}");
			}

			IHttpClient.Result result;
			if (method == HttpMethod.Get)
			{
				result = await _httpClient.GetAsync(uri, request.Headers);
			}
			else if (method == HttpMethod.Post)
			{
				result = await _httpClient.PostAsync(uri, request.Headers, request.Parameters);
			}
			else
			{
				throw new NotSupportedException($"Unsupported HTTP method: {method}");
			}

			var httpResponse = HttpResponse.FromResult(result);

			_middlewares?.ForEach(middleware => middleware.HandleResponse(httpResponse));

			if (_options.LogRequests && !result.IsSuccess)
			{
				_logger.LogError($"Request failed with error: {result.Content}");
			}

			return httpResponse;
		}

		public void RegisterMiddleware<T>(T middleware) where T : IHttpMiddleware
		{
			_middlewares.Add(middleware);
		}
	}
}
