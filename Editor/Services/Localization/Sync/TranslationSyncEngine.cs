using System.Collections.Generic;
using UnityEditor;

namespace BlueCheese.App.Editor
{
	/// <summary>
	/// Reconciles a <see cref="TranslationTableAsset"/> with an external <see cref="ITranslationSource"/>
	/// using per-cell last-write-wins (based on timestamps). The same operation runs in both directions,
	/// so it is idempotent and converges (a second run finds nothing to change).
	/// </summary>
	public static class TranslationSyncEngine
	{
		public static void Sync(TranslationTableAsset table, ITranslationSource source)
		{
			if (table == null || source == null)
			{
				return;
			}
			table.Validate();

			var sourceSnapshot = source.CanRead ? source.Read() : new TranslationSnapshot();
			var tableSnapshot = BuildFromTable(table);
			var merged = Merge(sourceSnapshot, tableSnapshot);

			bool tableChanged = ApplyToTable(table, merged);
			if (source.CanWrite)
			{
				source.Write(merged); // Write only rewrites the file when cells actually differ from it.
			}

			if (tableChanged)
			{
				EditorUtility.SetDirty(table);
			}
		}

		private static TranslationSnapshot BuildFromTable(TranslationTableAsset table)
		{
			var snapshot = new TranslationSnapshot();
			foreach (var item in table.Items)
			{
				var entry = snapshot.GetOrAdd(item.Key);
				foreach (var translation in item.Translations)
				{
					if (translation == null || translation.Language == Language.Unknown || translation.Value == null)
					{
						continue;
					}
					entry.Set(translation.Language, translation.Value, translation.LastModified);
				}
			}
			return snapshot;
		}

		private static TranslationSnapshot Merge(TranslationSnapshot source, TranslationSnapshot table)
		{
			var merged = new TranslationSnapshot();

			var keys = new HashSet<string>(source.Entries.Keys);
			keys.UnionWith(table.Entries.Keys);

			foreach (var key in keys)
			{
				source.Entries.TryGetValue(key, out var sourceEntry);
				table.Entries.TryGetValue(key, out var tableEntry);

				var languages = new HashSet<Language>();
				if (sourceEntry != null) languages.UnionWith(sourceEntry.Cells.Keys);
				if (tableEntry != null) languages.UnionWith(tableEntry.Cells.Keys);

				var mergedEntry = merged.GetOrAdd(key);
				foreach (var language in languages)
				{
					TranslationSnapshot.Cell sourceCell = default;
					TranslationSnapshot.Cell tableCell = default;
					bool hasSource = sourceEntry != null && sourceEntry.Cells.TryGetValue(language, out sourceCell);
					bool hasTable = tableEntry != null && tableEntry.Cells.TryGetValue(language, out tableCell);

					TranslationSnapshot.Cell chosen;
					if (hasSource && hasTable)
					{
						chosen = sourceCell.Ticks > tableCell.Ticks ? sourceCell : tableCell; // tie → table
					}
					else if (hasSource)
					{
						chosen = sourceCell;
					}
					else
					{
						chosen = tableCell;
					}
					mergedEntry.Set(language, chosen.Value, chosen.Ticks);
				}
			}

			return merged;
		}

		private static bool ApplyToTable(TranslationTableAsset table, TranslationSnapshot merged)
		{
			bool changed = false;
			foreach (var entry in merged.Entries.Values)
			{
				foreach (var cell in entry.Cells)
				{
					if (table.ImportCell(cell.Key, entry.Key, cell.Value.Value, cell.Value.Ticks))
					{
						changed = true;
					}
				}
			}
			return changed;
		}
	}
}
