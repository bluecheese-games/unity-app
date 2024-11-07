//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System;
using System.Reflection;

namespace BlueCheese.App
{
	public class LogFormatter : ILogFormatter
	{
		private readonly string _prefix;

		public LogFormatter(Type type)
		{
			_prefix = BuildPrefix(type);
		}

		private string BuildPrefix(Type type)
		{
			var attribute = type.GetCustomAttribute<LogFormatAttribute>(false);
			if (attribute != null)
			{
				return attribute.FormatPrefix($"[{type.Name}]");
			}
			return $"<b><color=#fff>[{type.Name}]</color></b>";
		}

		public string Format(string message)
		{
			return $"{_prefix} {message}";
		}
	}
}
