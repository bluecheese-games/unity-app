using System;

namespace BlueCheese.App
{
	[Flags]
	public enum LogLevel
	{
		None,
		Debug,
		Info,
		Warning,
		Error,
		Exception,
	}
}
