//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using UnityEngine;

namespace BlueCheese.App
{
	public class UnityLogger<TClass> : ILogger<TClass> where TClass : class
	{
		private readonly IApp _app;

		private readonly ILogFormatter _formatter;

		public LogLevel LogLevels { get; set; } = LogLevel.Info | LogLevel.Warning | LogLevel.Error | LogLevel.Exception;

		public UnityLogger(IApp app)
		{
			_app = app;
			_formatter = new LogFormatter(typeof(TClass));
		}

		public void LogDebug(string message, Object context = null)
		{
			if (_app.Environment == Environment.Development && LogLevels.HasFlag(LogLevel.Debug))
			{
				Debug.Log(_formatter.Format(message), context);
			}
		}

		public void LogInfo(string message, Object context = null)
		{
			if (LogLevels.HasFlag(LogLevel.Info))
			{
				Debug.Log(_formatter.Format(message), context);
			}
		}

		public void LogWarning(string message, Object context = null)
		{
			if (LogLevels.HasFlag(LogLevel.Warning))
			{
				Debug.LogWarning(_formatter.Format(message), context);
			}
		}

		public void LogError(string message, Object context = null)
		{
			if (LogLevels.HasFlag(LogLevel.Error))
			{
				Debug.LogError(_formatter.Format(message), context);
			}
		}

		public void LogException(System.Exception exeption, Object context = null)
		{
			if (LogLevels.HasFlag(LogLevel.Exception))
			{
				Debug.LogException(exeption, context);
			}
		}
	}
}
