//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using BlueCheese.Core.ServiceLocator;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace BlueCheese.App
{
	public class HttpService : IHttpService
	{
		public struct Options : IOptions
		{
			public bool LogRequests;
			public Uri BaseUri;
			public IList<IHttpMiddleware> Middlewares;

			public Options UseMiddleware<T>(T middleware) where T : IHttpMiddleware
			{
				Middlewares ??= new List<IHttpMiddleware>();
				Middlewares.Add(middleware);
				return this;
			}
		}

		private readonly IHttpClient _httpClient;
		private readonly ILogger<HttpService> _logger;
		private readonly IJsonService _json;
		private readonly Options _options = default;
		private readonly IList<IHttpMiddleware> _middlewares = null;

		public HttpService(Options options, IHttpClient httpClient, ILogger<HttpService> loggerService, IJsonService json)
		{
			_options = options;
			_httpClient = httpClient;
			_logger = loggerService;
			_json = json;

			_middlewares = _options.Middlewares;
		}

		public async Task<HttpResponse<T>> GetAsync<T>(HttpRequest request)
		{
			if (!request.TryGetUri(_options.BaseUri, out var uri))
			{
				return HttpResponse<T>.Failure($"Bad Url: {request.Url}", HttpStatusCode.BadRequest);
			}

			HandleMiddlewaresRequest(request);

			if (_options.LogRequests)
			{
				_logger.Log($"GET: {uri}");
			}

			var result = await _httpClient.GetAsync(uri, request.Headers);

			var httpResponse = CreateHttpResponse<T>(result);

			HandleMiddlewaresResponse(httpResponse);

			return httpResponse;
		}

		public async Task<HttpResponse<T>> PostAsync<T>(HttpRequest request)
		{
			if (!request.TryGetUri(_options.BaseUri, out var uri))
			{
				return HttpResponse<T>.Failure($"Bad Url: {request.Url}", HttpStatusCode.BadRequest);
			}

			HandleMiddlewaresRequest(request);

			if (_options.LogRequests)
			{
				_logger.Log($"POST: {uri}");
			}

			var result = await _httpClient.PostAsync(uri, request.Headers, request.Parameters);
			var httpResponse = CreateHttpResponse<T>(result);

			HandleMiddlewaresResponse(httpResponse);

			return httpResponse;
		}

		private void HandleMiddlewaresRequest(HttpRequest request)
		{
			if (_middlewares == null || _middlewares.Count == 0)
			{
				return;
			}

			foreach (var middleware in _middlewares)
			{
				middleware.HandleRequest(request);
			}
		}

		private void HandleMiddlewaresResponse(IHttpResponse response)
		{
			if (_middlewares == null || _middlewares.Count == 0)
			{
				return;
			}

			foreach (var middleware in _middlewares)
			{
				middleware.HandleResponse(response);
			}
		}

		private static void BuildHeaders(HttpRequest request, UnityWebRequest webRequest)
		{
			foreach (var kvp in request.Headers)
			{
				webRequest.SetRequestHeader(kvp.Key, kvp.Value);
			}
		}

		private HttpResponse<T> CreateHttpResponse<T>(IHttpClient.Result result)
		{
			var statusCode = (HttpStatusCode)result.StatusCode;
			if (result.IsSuccess)
			{
				string responseJson = result.Content;

				try
				{
					_logger.Log(responseJson);
					T data = _json.Deserialize<T>(responseJson);
					return HttpResponse<T>.Success(data, responseJson, statusCode);
				}
				catch (Exception e)
				{
					if (_options.LogRequests)
					{
						_logger.LogError($"Failed to parse response:\n----------\n{responseJson}\n----------");
					}
					return HttpResponse<T>.Failure(e.Message, HttpStatusCode.UnprocessableEntity);
				}
			}
			else
			{
				if (_options.LogRequests)
				{
					_logger.LogError($"Request failed with error: {result.Content}");
				}
				return HttpResponse<T>.Failure(result.Content, statusCode);
			}
		}
	}
}
