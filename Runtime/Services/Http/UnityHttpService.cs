//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using BlueCheese.Core.ServiceLocator;
using Core.Utils;
using System;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace BlueCheese.App.Services
{
    public class UnityHttpService : IHttpService
    {
        public struct Options : IOptions
        {
            public Uri BaseUri;
        }

        private readonly ILogger<UnityHttpService> _logger;
        private readonly ISerializationService _serializationService;

        private readonly Options _options = default;

        public UnityHttpService(Options options, ILogger<UnityHttpService> loggerService, ISerializationService serializationService)
        {
            _options = options;
            _logger = loggerService;
            _serializationService = serializationService;
        }

        public async Task<T> GetAsync<T>(string url)
        {
            if (!Uri.TryCreate(_options.BaseUri, url, out var uri) || !uri.IsWellFormedOriginalString())
            {
                throw new BadUrlException(uri != null ? uri.AbsoluteUri : url);
            }

            _logger.Log($"ApiService - GET: {uri}");
            var request = UnityWebRequest.Get(uri);
            await request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseJson = request.downloadHandler.text;
                try
                {
                    _logger.Log(responseJson);
                    T data = _serializationService.JsonDeserialize<T>(responseJson);
                    return data;
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
            else
            {
                throw new Exception($"Request failed with error: {request.error} ({request.url})");
            }
        }
    }

    public class BadUrlException : Exception
    {
        public string Url { get; }

        public BadUrlException(string url) : base($"Bad Url: {url}")
        {
            Url = url;
        }
    }
}
