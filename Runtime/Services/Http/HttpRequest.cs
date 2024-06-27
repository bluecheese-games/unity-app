//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System;
using System.Collections.Generic;
using System.Text;

namespace BlueCheese.App.Services
{
    public abstract class HttpRequest
    {
        public string Url { get; protected set; }
        public Dictionary<string, string> Headers { get; protected set; } = new();
        public Dictionary<string, string> Parameters { get; protected set; } = new();

        protected HttpRequest(string url)
        {
            Url = url;
        }

        public virtual bool TryGetUri(Uri baseUri, out Uri uri)
        {
            return Uri.TryCreate(baseUri, Url, out uri) && uri.IsWellFormedOriginalString();
        }

        public HttpRequest WithHeader(string key, string value)
        {
            Headers[key] = value;
            return this;
        }

        public HttpRequest WithParameter(string key, string value)
        {
            Parameters[key] = value;
            return this;
        }
    }

    public class HttpGetRequest : HttpRequest
    {
        private HttpGetRequest(string url) : base(url) { }

        public static implicit operator HttpGetRequest(string url) => new(url);
        public static HttpGetRequest Create(string url) => new(url);

        public override bool TryGetUri(Uri baseUri, out Uri uri)
        {
            if (Parameters.Count > 0)
            {
                Url = BuildUrlWithParameters(Url, Parameters);
            }
            return base.TryGetUri(baseUri, out uri);
        }

        private string BuildUrlWithParameters(string originalUrl, Dictionary<string, string> parameters)
        {
            var sb = new StringBuilder(originalUrl);
            string sep = Url.Contains("?") ? "" : "?";

            foreach (var kvp in parameters)
            {
                sb.Append(sep);
                sb.Append(kvp.Key);
                sb.Append("=");
                sb.Append(kvp.Value);
                sep = "&";
            }

            return sb.ToString();
        }
    }

    public class HttpPostRequest : HttpRequest
    {
        private HttpPostRequest(string url) : base(url) { }

        public static implicit operator HttpPostRequest(string url) => new(url);

        public static HttpPostRequest Create(string url) => new(url);
    }
}
