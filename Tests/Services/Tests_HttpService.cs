//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using BlueCheese.App;
using NUnit.Framework;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System;
using BlueCheese.Core.ServiceLocator;

namespace BlueCheese.Tests.Services
{
	[TestFixture]
	public class Tests_HttpService
	{
		private FakeHttpClient _fakeHttpClient;
		private FakeLogger<HttpService> _fakeLogger;
		private HttpService _httpService;

		[SetUp]
		public void SetUp()
		{
			_fakeHttpClient = new FakeHttpClient();
			_fakeLogger = new FakeLogger<HttpService>();
			var options = new HttpService.Options { BaseUri = new Uri("http://example.com") };
			_httpService = new HttpService(options, _fakeHttpClient, _fakeLogger);
			ServiceContainer.Default.Register<IJsonService, NewtonSoftJsonService>();
		}

		[TearDown]
		public void TearDown()
		{
			ServiceContainer.Default.Reset();
		}

		[Test]
		public async void GetAsync_ReturnsSuccessResponse()
		{
			// Arrange
			var request = new HttpGetRequest("test");
			var expectedResponse = new IHttpClient.Result(true, "{\"Key\":\"value\"}", 200);
			var expectedData = new FakeData { Key = "value" };
			_fakeHttpClient.GetAsyncResult = expectedResponse;

			// Act
			var response = await _httpService.GetAsync(request);

			// Assert
			Assert.IsTrue(response.IsSuccess);
			Assert.AreEqual(expectedData, response.GetData<FakeData>());
			Assert.AreEqual(expectedResponse.Content, response.JsonData);
			Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
		}

		[Test]
		public async void GetAsync_ReturnsFailureResponse_WhenUrlIsInvalid()
		{
			// Arrange
			var request = new HttpGetRequest("invalid url");

			// Act
			var response = await _httpService.GetAsync(request);

			// Assert
			Assert.IsFalse(response.IsSuccess);
			Assert.AreEqual("Bad Url: invalid url", response.ErrorMessage);
			Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
		}

		[Test]
		public async void PostAsync_ReturnsSuccessResponse()
		{
			// Arrange
			var request = new HttpPostRequest("test");
			var expectedResponse = new IHttpClient.Result(true, "{\"Key\":\"value\"}", 200);
			var expectedData = new FakeData { Key = "value" };
			_fakeHttpClient.PostAsyncResult = expectedResponse;

			// Act
			var response = await _httpService.PostAsync(request);

			// Assert
			Assert.IsTrue(response.IsSuccess);
			Assert.AreEqual(expectedData, response.GetData<FakeData>());
			Assert.AreEqual(expectedResponse.Content, response.JsonData);
			Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
		}

		[Test]
		public async void PostAsync_ReturnsFailureResponse_WhenUrlIsInvalid()
		{
			// Arrange
			var request = new HttpPostRequest("invalid url");

			// Act
			var response = await _httpService.PostAsync(request);

			// Assert
			Assert.IsFalse(response.IsSuccess);
			Assert.AreEqual("Bad Url: invalid url", response.ErrorMessage);
			Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
		}

		[Test]
		public async void GetAsync_CallsHandleResponseOnEachMiddleware()
		{
			// Arrange
			var request = new HttpGetRequest("http://test");
			var expectedResponse = new IHttpClient.Result(true, "{\"Key\":\"value\"}", 200);
			var expectedData = new FakeData { Key = "value" };
			_fakeHttpClient.GetAsyncResult = expectedResponse;
			var middleware1 = new FakeHttpMiddleware();
			var middleware2 = new FakeHttpMiddleware();
			var options = new HttpService.Options { Middlewares = new List<IHttpMiddleware> { middleware1, middleware2 } };
			_httpService = new HttpService(options, _fakeHttpClient, _fakeLogger);

			// Act
			var response = await _httpService.GetAsync(request);

			// Assert
			Assert.IsTrue(response.IsSuccess);
			Assert.AreEqual(expectedData, response.GetData<FakeData>());
			Assert.AreEqual(expectedResponse.Content, response.JsonData);
			Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
			Assert.AreEqual(1, middleware1.HandleRequestCallCount);
			Assert.AreEqual(1, middleware2.HandleRequestCallCount);
			Assert.AreEqual(1, middleware1.HandleResponseCallCount);
			Assert.AreEqual(1, middleware2.HandleResponseCallCount);
		}

		[Test]
		public async void GetAsync_ReturnsFailureResponse_WhenIHttpClientReturnsFailureResult()
		{
			// Arrange
			var request = new HttpGetRequest("test");
			var expectedResponse = new IHttpClient.Result(false, "Error message", 500);
			_fakeHttpClient.GetAsyncResult = expectedResponse;
			var options = new HttpService.Options { BaseUri = new Uri("http://example.com") };
			_httpService = new HttpService(options, _fakeHttpClient, _fakeLogger);

			// Act
			var response = await _httpService.GetAsync(request);

			// Assert
			Assert.IsFalse(response.IsSuccess);
			Assert.AreEqual(expectedResponse.Content, response.ErrorMessage);
			Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
		}

		[Test]
		public async void PostAsync_ReturnsFailureResponse_WhenIHttpClientReturnsFailureResult()
		{
			// Arrange
			var request = new HttpPostRequest("test");
			var expectedResponse = new IHttpClient.Result(false, "Error message", 500);
			_fakeHttpClient.PostAsyncResult = expectedResponse;
			var options = new HttpService.Options { BaseUri = new Uri("http://example.com") };
			_httpService = new HttpService(options, _fakeHttpClient, _fakeLogger);

			// Act
			var response = await _httpService.PostAsync(request);

			// Assert
			Assert.IsFalse(response.IsSuccess);
			Assert.AreEqual(expectedResponse.Content, response.ErrorMessage);
			Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
		}

		[Test]
		public async void GetAsync_LogsError_WhenIHttpClientReturnsFailureResult()
		{
			// Arrange
			var request = new HttpGetRequest("test");
			var expectedResponse = new IHttpClient.Result(false, "Error message", 500);
			_fakeHttpClient.GetAsyncResult = expectedResponse;
			var options = new HttpService.Options { BaseUri = new Uri("http://example.com"), LogRequests = true };
			_httpService = new HttpService(options, _fakeHttpClient, _fakeLogger);

			// Act
			var response = await _httpService.GetAsync(request);

			// Assert
			Assert.AreEqual(1, _fakeLogger.LogErrorCallCount);
			Assert.AreEqual($"Request failed with error: {expectedResponse.Content}", _fakeLogger.LastLogErrorMessage);
		}

		[Test]
		public async void PostAsync_LogsError_WhenIHttpClientReturnsFailureResult()
		{
			// Arrange
			var request = new HttpPostRequest("test");
			var expectedResponse = new IHttpClient.Result(false, "Error message", 500);
			_fakeHttpClient.PostAsyncResult = expectedResponse;
			var options = new HttpService.Options { BaseUri = new Uri("http://example.com"), LogRequests = true };
			_httpService = new HttpService(options, _fakeHttpClient, _fakeLogger);

			// Act
			var response = await _httpService.PostAsync(request);

			// Assert
			Assert.AreEqual(1, _fakeLogger.LogErrorCallCount);
			Assert.AreEqual($"Request failed with error: {expectedResponse.Content}", _fakeLogger.LastLogErrorMessage);
		}

		private struct FakeData
		{
			public string Key;
		}
	}

	public class FakeHttpClient : IHttpClient
	{
		public IHttpClient.Result GetAsyncResult { get; set; }
		public IHttpClient.Result PostAsyncResult { get; set; }

		public Task<IHttpClient.Result> GetAsync(Uri uri, Dictionary<string, string> headers)
		{
			return Task.FromResult(GetAsyncResult);
		}

		public Task<IHttpClient.Result> PostAsync(Uri uri, Dictionary<string, string> headers, Dictionary<string, string> parameters)
		{
			return Task.FromResult(PostAsyncResult);
		}
	}

	public class FakeLogger<TClass> : ILogger<TClass> where TClass : class
	{
		public LogType LogTypes { get; set; }
		public int LogCallCount { get; private set; }
		public string LastLogMessage { get; private set; }
		public int LogWarningCallCount { get; private set; }
		public string LastLogWarningMessage { get; private set; }
		public int LogErrorCallCount { get; private set; }
		public string LastLogErrorMessage { get; private set; }
		public int LogExceptionCallCount { get; private set; }
		public Exception LastLogException { get; private set; }

		public void Log(string message, UnityEngine.Object context = null)
		{
			LogCallCount++;
			LastLogMessage = message;
		}

		public void LogWarning(string message, UnityEngine.Object context = null)
		{
			LogWarningCallCount++;
			LastLogWarningMessage = message;
		}

		public void LogError(string message, UnityEngine.Object context = null)
		{
			LogErrorCallCount++;
			LastLogErrorMessage = message;
		}

		public void LogException(Exception exeption, UnityEngine.Object context = null)
		{
			LogExceptionCallCount++;
			LastLogException = exeption;
		}
	}

	public class FakeHttpMiddleware : IHttpMiddleware
	{
		public int HandleRequestCallCount { get; private set; }
		public int HandleResponseCallCount { get; private set; }

		public void HandleRequest(IHttpRequest request)
		{
			HandleRequestCallCount++;
		}

		public void HandleResponse(IHttpResponse response)
		{
			HandleResponseCallCount++;
		}
	}
}