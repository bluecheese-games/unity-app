//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System.Collections.Generic;

namespace BlueCheese.App.Services
{
    public interface IHttpRequest
    {
        Dictionary<string, string> Headers { get; }
        Dictionary<string, string> Parameters { get; }
        string Url { get; }

        HttpRequest AddHeader(string key, string value);
        HttpRequest AddParameter(string key, string value);
    }
}