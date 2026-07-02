//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System.Collections.Generic;

namespace BlueCheese.App.Editor
{
	/// <summary>
	/// Provider-agnostic, normalized view of a set of translations. It is the pivot model between a
	/// <see cref="TranslationTableAsset"/> and any external source (.obd file today, cloud later).
	/// Each cell carries a UTC-ticks timestamp used for last-write-wins merging.
	/// </summary>
	public class TranslationSnapshot
	{
		public readonly Dictionary<string, Entry> Entries = new();

		public Entry GetOrAdd(string key)
		{
			if (!Entries.TryGetValue(key, out var entry))
			{
				entry = new Entry(key);
				Entries[key] = entry;
			}
			return entry;
		}

		public class Entry
		{
			public readonly string Key;
			public readonly Dictionary<Language, Cell> Cells = new();

			public Entry(string key) => Key = key;

			public void Set(Language language, string value, long ticks) => Cells[language] = new Cell(value, ticks);
		}

		public readonly struct Cell
		{
			public readonly string Value;
			public readonly long Ticks; // UTC ticks; 0 = unknown

			public Cell(string value, long ticks)
			{
				Value = value;
				Ticks = ticks;
			}
		}
	}
}
