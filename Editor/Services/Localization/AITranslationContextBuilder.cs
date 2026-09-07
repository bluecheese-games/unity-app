using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BlueCheese.App.Editor
{
	/// <summary>
	/// Assembles the richest possible context for an AI translation request: source text,
	/// a consistency glossary of validated translations, the key's usage across the project,
	/// the estimated UI character budget, and the project-level guidelines.
	/// </summary>
	public static class AITranslationContextBuilder
	{
		public static AITranslationRequest Build(
			TranslationTableAsset asset,
			TranslationTableAsset.TranslationItem item,
			Language sourceLanguage,
			List<Language> targetLanguages,
			List<TranslationKeyReferenceFinder.Reference> references,
			AITranslationSettings settings)
		{
			return new AITranslationRequest
			{
				Key = item.Key,
				SourceLanguageName = sourceLanguage.ToString(),
				SourceText = asset.GetTranslation(item.Key, sourceLanguage),
				UsageContext = settings.IncludeUsageContext ? BuildUsageContext(references) : string.Empty,
				MaxChars = settings.EnforceMaxChars ? MinMaxChars(references) : 0,
				Glossary = BuildGlossary(asset, sourceLanguage, targetLanguages, settings.GlossarySampleSize),
				Guidelines = settings.Guidelines,
				ProjectName = settings.ProjectName,
				ProjectInfo = settings.ProjectInfo,
				AlternativesCount = System.Math.Max(1, settings.AlternativesCount),
				TargetLanguageNames = targetLanguages.Select(l => l.ToString()).ToList(),
			};
		}

		private static string BuildUsageContext(List<TranslationKeyReferenceFinder.Reference> references)
		{
			if (references == null || references.Count == 0)
			{
				return "(not used by any LocalizedText in the project)";
			}

			var sb = new StringBuilder();
			sb.AppendLine("Used by:");
			foreach (var line in references
				.Select(r => $"- {r.ComponentType} in {(r.IsScene ? "scene" : "prefab")} '{Path.GetFileNameWithoutExtension(r.AssetPath)}' at {r.ObjectPath}")
				.Distinct())
			{
				sb.AppendLine(line);
			}

			// Nearby texts (other TMP texts under the same parent) help disambiguate meaning/register.
			var siblings = references
				.Where(r => r.SiblingTexts != null)
				.SelectMany(r => r.SiblingTexts)
				.Distinct()
				.Take(15)
				.ToList();
			if (siblings.Count > 0)
			{
				sb.AppendLine("Other texts in the same UI container:");
				foreach (var text in siblings)
				{
					sb.AppendLine($"- \"{text}\"");
				}
			}

			return sb.ToString();
		}

		private static int MinMaxChars(List<TranslationKeyReferenceFinder.Reference> references)
		{
			int min = 0;
			if (references != null)
			{
				foreach (var reference in references)
				{
					if (reference.MaxChars > 0 && (min == 0 || reference.MaxChars < min))
					{
						min = reference.MaxChars;
					}
				}
			}
			return min;
		}

		private static string BuildGlossary(TranslationTableAsset asset, Language sourceLanguage, List<Language> targetLanguages, int sampleSize)
		{
			if (sampleSize <= 0)
			{
				return string.Empty;
			}

			var sb = new StringBuilder();
			int count = 0;
			foreach (var item in asset.Items)
			{
				if (item.Status != TranslationStatus.Validated)
				{
					continue;
				}
				var source = asset.GetTranslation(item.Key, sourceLanguage);
				if (string.IsNullOrEmpty(source))
				{
					continue;
				}
				var pairs = targetLanguages
					.Select(l => (l, t: asset.GetTranslation(item.Key, l)))
					.Where(p => !string.IsNullOrEmpty(p.t))
					.Select(p => $"{p.l}=\"{p.t}\"")
					.ToList();
				if (pairs.Count == 0)
				{
					continue;
				}
				sb.AppendLine($"- \"{source}\" -> {string.Join(", ", pairs)}");
				if (++count >= sampleSize)
				{
					break;
				}
			}
			return sb.ToString();
		}
	}
}
