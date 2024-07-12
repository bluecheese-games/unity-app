//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System;
using System.Collections.Generic;

namespace BlueCheese.App
{
    public interface IHttpRequest
    {
        Dictionary<string, string> Headers { get; }
        Dictionary<string, string> Parameters { get; }
        string Url { get; }

        HttpRequest AddHeader(string key, string value);
        HttpRequest AddParameter(string key, string value);
		bool TryGetUri(Uri baseUri, out Uri uri);
	}
}