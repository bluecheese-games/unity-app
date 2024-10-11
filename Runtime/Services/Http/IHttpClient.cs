//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlueCheese.App
{
	public interface IHttpClient
	{
		Task<Result> GetAsync(Uri uri, Dictionary<string, string> headers);

		Task<Result> PostAsync(Uri uri, Dictionary<string, string> headers, Dictionary<string, string> parameters);

		public struct Result
		{
			public readonly bool IsSuccess;
			public string Content;
			public long StatusCode;

			public Result(bool isSuccess, string content, long statusCode)
			{
				IsSuccess = isSuccess;
				Content = content;
				StatusCode = statusCode;
			}
		}
	}
}
