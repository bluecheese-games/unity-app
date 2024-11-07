//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

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
