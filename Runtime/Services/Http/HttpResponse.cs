//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System.Net;

namespace BlueCheese.App
{
    public class HttpResponse<T> : IHttpResponse
    {
        public bool IsSuccess { get; private set; }
        public string JsonData { get; private set; }
        public T Data { get; private set; }
        public HttpStatusCode StatusCode { get; private set; }
        public string ErrorMessage { get; private set; }

        private HttpResponse() { }

        public static HttpResponse<T> Success(T data, string jsonData, HttpStatusCode statusCode = HttpStatusCode.OK) => new()
        {
            IsSuccess = true,
            Data = data,
            JsonData = jsonData,
            StatusCode = statusCode,
            ErrorMessage = null,
        };

        public static HttpResponse<T> Failure(string errorMessage, HttpStatusCode statusCode) => new()
        {
            IsSuccess = false,
            Data = default,
            JsonData = null,
            StatusCode = statusCode,
            ErrorMessage = errorMessage,
        };

        public static implicit operator T(HttpResponse<T> httpResponse) => httpResponse.Data;
        public static implicit operator string(HttpResponse<T> httpResponse) => httpResponse.JsonData;
    }
}
