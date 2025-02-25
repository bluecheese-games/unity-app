//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using System;

namespace BlueCheese.App
{
	public interface ILogger<TClass> where TClass : class
	{
		LogLevel LogLevels { get; set; }
		void LogDebug(string message, UnityEngine.Object context = null);
		void LogInfo(string message, UnityEngine.Object context = null);
		void LogWarning(string message, UnityEngine.Object context = null);
		void LogError(string message, UnityEngine.Object context = null);
		void LogException(Exception exeption, UnityEngine.Object context = null);
	}
}
