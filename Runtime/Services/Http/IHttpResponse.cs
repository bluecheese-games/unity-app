//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System.Net;

namespace BlueCheese.App
{
    public interface IHttpResponse
    {
        string ErrorMessage { get; }
        bool IsSuccess { get; }
        string JsonData { get; }
        HttpStatusCode StatusCode { get; }
    }
}