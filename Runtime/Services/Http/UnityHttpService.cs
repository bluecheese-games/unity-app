//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using BlueCheese.Core.ServiceLocator;
using Core.Utils;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace BlueCheese.App
{
    public class UnityHttpService : IHttpService
    {
        public struct Options : IOptions
        {
            public bool LogRequests;
            public Uri BaseUri;
            public IList<IHttpMiddleware> Middlewares;

            public Options UseMiddleware<T>() where T : IHttpMiddleware
            {
                Middlewares ??= new List<IHttpMiddleware>();
                Middlewares.Add(Services.Instantiate<T>());
                return this;
            }
        }

        private readonly ILogger<UnityHttpService> _logger;
        private readonly IJsonService _json;
        private readonly Options _options = default;
        private readonly IList<IHttpMiddleware> _middlewares = null;

        public UnityHttpService(Options options, ILogger<UnityHttpService> loggerService, IJsonService json)
        {
            _options = options;
            _logger = loggerService;
            _json = json;

            _middlewares = _options.Middlewares;
        }

        public async Task<HttpResponse<T>> GetAsync<T>(HttpRequest request)
        {
            if (!request.TryGetUri(_options.BaseUri, out var uri))
            {
                return HttpResponse<T>.Failure($"Bad Url: {request}", HttpStatusCode.BadRequest);
            }

            HandleMiddlewaresRequest(request);

            if (_options.LogRequests)
            {
                _logger.Log($"GET: {uri}");
            }

            var webRequest = UnityWebRequest.Get(uri);
            BuildHeaders(request, webRequest);

            await webRequest.SendWebRequest();

            var response = CreateResponse<T>(webRequest);

            HandleMiddlewaresResponse(response);

            return response;
        }

        public async Task<HttpResponse<T>> PostAsync<T>(HttpRequest request)
        {
            if (!request.TryGetUri(_options.BaseUri, out var uri))
            {
                return HttpResponse<T>.Failure($"Bad Url: {request}", HttpStatusCode.BadRequest);
            }

            HandleMiddlewaresRequest(request);

            if (_options.LogRequests)
            {
                _logger.Log($"POST: {uri}");
            }
            var webRequest = UnityWebRequest.Post(uri, request.Parameters);
            BuildHeaders(request, webRequest);
            await webRequest.SendWebRequest();

            var response = CreateResponse<T>(webRequest);

            HandleMiddlewaresResponse(response);

            return response;
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

        private HttpResponse<T> CreateResponse<T>(UnityWebRequest webRequest)
        {
            var statusCode = (HttpStatusCode)webRequest.responseCode;
            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string responseJson = webRequest.downloadHandler.text;
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
                    _logger.LogError($"Request failed with error: {webRequest.error} ({webRequest.url})");
                }
                return HttpResponse<T>.Failure(webRequest.error, statusCode);
            }
        }
    }
}
