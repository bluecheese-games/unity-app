//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System;
using System.Collections.Generic;
using System.Text;

namespace BlueCheese.App
{
    public abstract class HttpRequest : IHttpRequest
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

        public HttpRequest AddHeader(string key, string value)
        {
            Headers[key] = value;
            return this;
        }

        public HttpRequest AddParameter(string key, string value)
        {
            Parameters[key] = value;
            return this;
        }
    }

    public class HttpGetRequest : HttpRequest
    {
        public HttpGetRequest(string url) : base(url) { }

        public static implicit operator HttpGetRequest(string url) => new(url);

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
        public HttpPostRequest(string url) : base(url) { }

        public static implicit operator HttpPostRequest(string url) => new(url);
    }
}
