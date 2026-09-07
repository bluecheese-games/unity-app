using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BlueCheese.App.Editor
{
	/// <summary>
	/// Watches for ".obd" files entering/changing in the project. On the first detection an
	/// auto-linked <see cref="TranslationTableAsset"/> is created next to the file; afterwards any
	/// change re-syncs the linked table (obd → table, with bidirectional merge).
	/// </summary>
	public class ObdAssetPostprocessor : AssetPostprocessor
	{
		private const string ObdExtension = ".obd";

		private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
		{
			List<string> obdPaths = null;
			foreach (var path in importedAssets)
			{
				if (path.EndsWith(ObdExtension, StringComparison.OrdinalIgnoreCase))
				{
					(obdPaths ??= new List<string>()).Add(path);
				}
			}
			if (obdPaths == null)
			{
				return;
			}

			// Defer so we can safely create/modify assets outside the import callback.
			EditorApplication.delayCall += () => Process(obdPaths);
		}

		private static void Process(List<string> obdPaths)
		{
			var tables = TranslationAssetFinder.FindAllTables();
			bool createdAny = false;

			foreach (var obdPath in obdPaths)
			{
				if (TranslationSyncGuard.Consume(obdPath))
				{
					continue; // this change was our own write-back
				}
				if (!File.Exists(obdPath))
				{
					continue;
				}

				var table = tables.FirstOrDefault(t =>
					t != null && t.SourceType == ObdTranslationSource.SourceTypeId && t.SourcePath == obdPath);

				if (table == null)
				{
					table = CreateLinkedTable(obdPath);
					createdAny = true;
				}
				if (table != null)
				{
					TranslationSyncEngine.Sync(table, new ObdTranslationSource(obdPath));
				}
			}

			AssetDatabase.SaveAssets();
			if (createdAny)
			{
				AssetDatabase.Refresh();
			}
		}

		private static TranslationTableAsset CreateLinkedTable(string obdPath)
		{
			var directory = Path.GetDirectoryName(obdPath)?.Replace('\\', '/');
			var name = Path.GetFileNameWithoutExtension(obdPath);
			var candidate = string.IsNullOrEmpty(directory) ? $"{name}.asset" : $"{directory}/{name}.asset";
			var tablePath = AssetDatabase.GenerateUniqueAssetPath(candidate);

			var table = ScriptableObject.CreateInstance<TranslationTableAsset>();
			table.SetSource(ObdTranslationSource.SourceTypeId, obdPath);
			AssetDatabase.CreateAsset(table, tablePath);
			Debug.Log($"[Localization] Created translation table '{tablePath}' linked to '{obdPath}'.");
			return table;
		}
	}
}
