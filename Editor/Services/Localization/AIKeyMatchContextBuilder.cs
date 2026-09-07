//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BlueCheese.App.Editor
{
	/// <summary>
	/// Assembles the richest possible context to find or create a localization key for a live component
	/// being edited in the inspector: the current text, the object's place in the hierarchy/scene/prefab,
	/// neighbouring UI texts, and every existing key (with its default-language text) to search against.
	/// </summary>
	public static class AIKeyMatchContextBuilder
	{
		public static AIKeyMatchRequest Build(Component component, string sourceText, Language defaultLanguage, List<KeyCandidate> existingKeys, AITranslationSettings settings)
		{
			return new AIKeyMatchRequest
			{
				SourceText = sourceText ?? string.Empty,
				ObjectContext = BuildObjectContext(component),
				SourceLanguageName = defaultLanguage.ToString(),
				ProjectName = settings.ProjectName,
				ProjectInfo = settings.ProjectInfo,
				Guidelines = settings.Guidelines,
				NewKeyCandidatesCount = 3,
				ExistingKeys = existingKeys,
			};
		}

		/// <summary>Every key across every table, paired with its default-language text, for the AI to search.</summary>
		public static List<KeyCandidate> BuildExistingKeysGlossary(EditorTranslationService service, Language defaultLanguage)
		{
			var candidates = new List<KeyCandidate>();
			foreach (var table in service.GetTranslationTableAssets())
			{
				if (table == null)
				{
					continue;
				}
				foreach (var item in table.Items)
				{
					candidates.Add(new KeyCandidate { Key = item.Key, DefaultText = table.GetTranslation(item.Key, defaultLanguage) });
				}
			}
			return candidates;
		}

		private static string BuildObjectContext(Component component)
		{
			var sb = new StringBuilder();
			sb.AppendLine($"- GameObject: {component.gameObject.name}");
			sb.AppendLine($"- Component: {component.GetType().Name}");
			sb.AppendLine($"- Hierarchy path: {TranslationKeyReferenceFinder.GetHierarchyPath(component.transform)}");

			var stage = PrefabStageUtility.GetCurrentPrefabStage();
			if (stage != null)
			{
				sb.AppendLine($"- In prefab: {Path.GetFileNameWithoutExtension(stage.assetPath)}");
			}
			else if (component.gameObject.scene.IsValid())
			{
				sb.AppendLine($"- In scene: {component.gameObject.scene.name}");
			}

			var siblings = TranslationKeyReferenceFinder.GatherSiblingTexts(component);
			if (siblings.Length > 0)
			{
				sb.AppendLine("- Other texts in the same UI container:");
				foreach (var text in siblings)
				{
					sb.AppendLine($"  - \"{text}\"");
				}
			}

			return sb.ToString();
		}
	}
}
