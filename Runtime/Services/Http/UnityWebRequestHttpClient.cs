﻿//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using Core.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace BlueCheese.App
{
	public class UnityWebRequestHttpClient : IHttpClient
	{
		public async Task<IHttpClient.Result> GetAsync(Uri uri, Dictionary<string, string> headers)
		{
			var webRequest = UnityWebRequest.Get(uri);
			foreach (var kvp in headers)
			{
				webRequest.SetRequestHeader(kvp.Key, kvp.Value);
			}

			await webRequest.SendWebRequest();

			bool isSuccess = webRequest.result == UnityWebRequest.Result.Success;
			string content = isSuccess ? webRequest.downloadHandler.text : webRequest.error;

			return new IHttpClient.Result(isSuccess, content, webRequest.responseCode);
		}

		public async Task<IHttpClient.Result> PostAsync(Uri uri, Dictionary<string, string> headers, Dictionary<string, string> parameters)
		{
			var webRequest = UnityWebRequest.Post(uri, parameters);
			foreach (var kvp in headers)
			{
				webRequest.SetRequestHeader(kvp.Key, kvp.Value);
			}

			await webRequest.SendWebRequest();

			bool isSuccess = webRequest.result == UnityWebRequest.Result.Success;
			string content = isSuccess ? webRequest.downloadHandler.text : webRequest.error;

			return new IHttpClient.Result(isSuccess, content, webRequest.responseCode);
		}
	}
}
