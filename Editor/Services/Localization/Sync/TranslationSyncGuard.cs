//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System.Collections.Generic;

namespace BlueCheese.App.Editor
{
	/// <summary>
	/// Breaks the sync loop: when we write an external file ourselves, the resulting asset reimport
	/// would trigger the importer again. We mark such paths here so the importer skips them once.
	/// </summary>
	public static class TranslationSyncGuard
	{
		private static readonly HashSet<string> _suppressed = new();

		public static void Suppress(string path) => _suppressed.Add(path);

		/// <summary>Returns true (and clears the flag) if the path was suppressed by our own write.</summary>
		public static bool Consume(string path) => _suppressed.Remove(path);
	}
}
