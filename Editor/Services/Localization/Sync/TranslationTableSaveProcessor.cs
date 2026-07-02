//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;
using UnityEditor;

namespace BlueCheese.App.Editor
{
	/// <summary>
	/// When a source-linked <see cref="TranslationTableAsset"/> is saved, pushes its changes back to
	/// the external source (table → obd), merging by per-cell timestamp.
	/// </summary>
	public class TranslationTableSaveProcessor : AssetModificationProcessor
	{
		private static string[] OnWillSaveAssets(string[] paths)
		{
			foreach (var path in paths)
			{
				if (!path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				var table = AssetDatabase.LoadAssetAtPath<TranslationTableAsset>(path);
				if (table == null || !table.HasSource)
				{
					continue;
				}

				if (table.SourceType == ObdTranslationSource.SourceTypeId)
				{
					TranslationSyncEngine.Sync(table, new ObdTranslationSource(table.SourcePath));
				}
			}
			return paths;
		}
	}
}
