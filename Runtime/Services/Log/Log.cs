//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using BlueCheese.Core.DI;
using System;

namespace BlueCheese.App
{
	public static class Log
	{
		private static class Level
		{
			[LogFormat(prefixColor: "", prefixStyle: UnityEngine.FontStyle.Normal)] public class Debug { }
			[LogFormat(prefixColor: "fff")] public class Info { }
			[LogFormat(prefixColor: "ff0")] public class Warning { }
			[LogFormat(prefixColor: "f00")] public class Error { }
			[LogFormat(prefixColor: "f0f")] public class Exception { }
		}

		public static void Debug(string message, UnityEngine.Object context = null)
			=> ServiceLocator.Resolve<ILogger<Level.Debug>>().LogInfo(message, context);

		public static void Debug<TClass>(string message, UnityEngine.Object context = null) where TClass : class
			=> ServiceLocator.Resolve<ILogger<TClass>>().LogInfo(message, context);

		public static void Info(string message, UnityEngine.Object context = null)
			=> ServiceLocator.Resolve<ILogger<Level.Info>>().LogInfo(message, context);

		public static void Info<TClass>(string message, UnityEngine.Object context = null) where TClass : class
			=> ServiceLocator.Resolve<ILogger<TClass>>().LogInfo(message, context);

		public static void Warning(string message, UnityEngine.Object context = null)
			=> ServiceLocator.Resolve<ILogger<Level.Warning>>().LogWarning(message, context);

		public static void Warning<TClass>(string message, UnityEngine.Object context = null) where TClass : class
			=> ServiceLocator.Resolve<ILogger<TClass>>().LogWarning(message, context);

		public static void Error(string message, UnityEngine.Object context = null)
			=> ServiceLocator.Resolve<ILogger<Level.Error>>().LogError(message, context);

		public static void Error<TClass>(string message, UnityEngine.Object context = null) where TClass : class
			=> ServiceLocator.Resolve<ILogger<TClass>>().LogError(message, context);

		public static void Exception(Exception exception, UnityEngine.Object context = null)
			=> ServiceLocator.Resolve<ILogger<Level.Exception>>().LogException(exception, context);
	}
}
