//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System;
using UnityEngine;

namespace BlueCheese.App.Editor
{
	public class EditorApp : IApp
	{
		public Environment Environment => Environment.Development;

		public Version Version => new(Application.version);

		public void Quit() { }
	}
}
