//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using BlueCheese.Core.ServiceLocator;
using Core.Utils;
using System;
using System.Net;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace BlueCheese.App.Services
{
    public class UnityHttpService : IHttpService
    {
        public struct Options : IOptions
        {
            public bool LogRequests;
            public Uri BaseUri;
        }

        private readonly ILogger<UnityHttpService> _logger;
        private readonly IJsonService _json;
        private readonly Options _options = default;

        public UnityHttpService(Options options, ILogger<UnityHttpService> loggerService, IJsonService json)
        {
            _options = options;
            _logger = loggerService;
            _json = json;
        }

        public async Task<HttpResponse<T>> GetAsync<T>(HttpGetRequest request)
        {
            if (!request.TryGetUri(_options.BaseUri, out var uri))
            {
                return HttpResponse<T>.Failure($"Bad Url: {request}", HttpStatusCode.BadRequest);
            }

            if (_options.LogRequests)
            {
                _logger.Log($"GET: {uri}");
            }
            var webRequest = UnityWebRequest.Get(uri);
            BuildHeaders(request, webRequest);
            await webRequest.SendWebRequest();
            return HandleResponse<T>(webRequest);
        }

        public async Task<HttpResponse<T>> PostAsync<T>(HttpPostRequest request)
        {
            if (!request.TryGetUri(_options.BaseUri, out var uri))
            {
                return HttpResponse<T>.Failure($"Bad Url: {request}", HttpStatusCode.BadRequest);
            }

            if (_options.LogRequests)
            {
                _logger.Log($"POST: {uri}");
            }
            var webRequest = UnityWebRequest.Post(uri, request.Parameters);
            BuildHeaders(request, webRequest);
            await webRequest.SendWebRequest();
            return HandleResponse<T>(webRequest);
        }

        private static void BuildHeaders(HttpRequest request, UnityWebRequest webRequest)
        {
            foreach (var kvp in request.Headers)
            {
                webRequest.SetRequestHeader(kvp.Key, kvp.Value);
            }
        }

        private HttpResponse<T> HandleResponse<T>(UnityWebRequest webRequest)
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
