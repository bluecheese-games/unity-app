//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using BlueCheese.Core.DI;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using Cysharp.Threading.Tasks;

namespace BlueCheese.App
{
	public class HttpService : IHttpService
	{
		public class Settings
		{
			public bool LogRequests;
			public Uri BaseUri;
			public List<IHttpMiddleware> Middlewares;

			public Settings UseMiddleware<T>(T middleware) where T : IHttpMiddleware
			{
				Middlewares ??= new List<IHttpMiddleware>();
				Middlewares.Add(middleware);
				return this;
			}
		}

		private readonly IHttpClient _httpClient;
		private readonly ILogger<HttpService> _logger;
		private readonly Settings _settings = default;
		private readonly List<IHttpMiddleware> _middlewares = new();

		public HttpService(IHttpClient httpClient, ILogger<HttpService> loggerService, IOptions<Settings> settings)
		{
			_settings = settings.Value;
			_httpClient = httpClient;
			_logger = loggerService;

			if (_settings.Middlewares != null)
			{
				_middlewares.AddRange(_settings.Middlewares);
			}
		}

		public async UniTask<IHttpResponse> GetAsync(IHttpRequest request) => await ProcessRequestAsync(request, HttpMethod.Get);

		public async UniTask<IHttpResponse> PostAsync(IHttpRequest request) => await ProcessRequestAsync(request, HttpMethod.Post);

		private async UniTask<IHttpResponse> ProcessRequestAsync(IHttpRequest request, HttpMethod method)
		{
			if (!request.TryGetUri(_settings.BaseUri, out var uri))
			{
				return HttpResponse.Failure($"Bad Url: {request.Url}", HttpStatusCode.BadRequest);
			}

			_middlewares?.ForEach(middleware => middleware.HandleRequest(request));

			if (_settings.LogRequests)
			{
				_logger.LogInfo($"{method}: {uri}");
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

			if (_settings.LogRequests && !result.IsSuccess)
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
