//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using BlueCheese.Core.ServiceLocator;
using System.Net;

namespace BlueCheese.App
{
    public class HttpResponse : IHttpResponse
    {
        public bool IsSuccess { get; private set; }
        public string JsonData { get; private set; }
        public HttpStatusCode StatusCode { get; private set; }
        public string ErrorMessage { get; private set; }

        private HttpResponse() { }

        public T GetData<T>() => Services.Get<IJsonService>().Deserialize<T>(JsonData);

        public static IHttpResponse Success(string jsonData, HttpStatusCode statusCode = HttpStatusCode.OK) => new HttpResponse()
        {
            IsSuccess = true,
            JsonData = jsonData,
            StatusCode = statusCode,
            ErrorMessage = null,
        };

        public static IHttpResponse Failure(string errorMessage, HttpStatusCode statusCode) => new HttpResponse()
        {
            IsSuccess = false,
            JsonData = null,
            StatusCode = statusCode,
            ErrorMessage = errorMessage,
        };

        public static IHttpResponse FromResult(IHttpClient.Result result) => new HttpResponse()
		{
			IsSuccess = result.IsSuccess,
			JsonData = result.IsSuccess ? result.Content : null,
			StatusCode = (HttpStatusCode)result.StatusCode,
			ErrorMessage = result.IsSuccess ? null : result.Content,
		};
    }
}
