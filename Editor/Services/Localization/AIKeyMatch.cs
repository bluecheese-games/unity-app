//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;
using System.Text;
using UnityEngine;

namespace BlueCheese.App.Editor
{
	#region Contracts

	/// <summary>A candidate existing key offered to the AI for matching, with its default-language text.</summary>
	public class KeyCandidate
	{
		public string Key;
		public string DefaultText;
	}

	/// <summary>Everything the AI needs to find (or propose) the localization key for a piece of UI text.</summary>
	public class AIKeyMatchRequest
	{
		public string SourceText;              // current text on the field; may be empty
		public string ObjectContext;           // hierarchy path, scene/prefab, sibling texts…
		public string SourceLanguageName;
		public string ProjectName;
		public string ProjectInfo;
		public string Guidelines;
		public int NewKeyCandidatesCount = 3;
		public System.Collections.Generic.List<KeyCandidate> ExistingKeys = new();
	}

	public class NewKeySuggestion
	{
		public string Key;
		public string DefaultText;
	}

	public class AIKeyMatchResult
	{
		public bool Success;
		public string Error;
		public string MatchedKey;      // set when an existing key is a confident match
		public string MatchReason;
		public System.Collections.Generic.List<NewKeySuggestion> NewKeySuggestions = new();

		public static AIKeyMatchResult Fail(string error) => new() { Success = false, Error = error };
	}

	#endregion

	/// <summary>Provider-agnostic prompt building and response parsing for key matching/suggestion.</summary>
	internal static class AIKeyMatchPrompt
	{
		public static string BuildSystem(AIKeyMatchRequest request)
		{
			var sb = new StringBuilder();
			sb.AppendLine("You are an expert video game localizer, in charge of keeping a localization key catalog clean and consistent.");
			if (!string.IsNullOrWhiteSpace(request.ProjectName)) sb.AppendLine($"Project: {request.ProjectName}.");
			if (!string.IsNullOrWhiteSpace(request.ProjectInfo)) sb.AppendLine(request.ProjectInfo);
			if (!string.IsNullOrWhiteSpace(request.Guidelines))
			{
				sb.AppendLine("Guidelines:");
				sb.AppendLine(request.Guidelines);
			}
			sb.AppendLine("Task: given a UI text and its usage context, decide whether one of the EXISTING KEYS already represents");
			sb.AppendLine("the exact same text/meaning (same wording or a trivial rewording, same usage). Only match when confident —");
			sb.AppendLine("a wrong match is worse than proposing a new key.");
			sb.AppendLine("- If a confident match exists: return it in \"matchedKey\" with a short \"matchReason\", and leave \"newKeySuggestions\" empty.");
			sb.AppendLine("- Otherwise: leave \"matchedKey\" empty, and propose new key candidates in \"newKeySuggestions\".");
			sb.AppendLine("  Infer the project's key naming convention from the EXISTING KEYS sample (e.g. dot-separated lowercase");
			sb.AppendLine("  segments such as \"area.subarea.name\"). Each candidate needs a \"key\" and a \"defaultText\" (the source text,");
			sb.AppendLine("  verbatim if provided, otherwise inferred from the usage context).");
			sb.AppendLine($"- Provide exactly {Math.Max(1, request.NewKeyCandidatesCount)} new key candidate(s) when proposing new keys.");
			sb.AppendLine("- Output ONLY valid minified JSON, no markdown, matching exactly:");
			sb.AppendLine("  {\"matchedKey\":\"<existing key or empty>\",\"matchReason\":\"<short reason or empty>\",\"newKeySuggestions\":[{\"key\":\"<proposed.key>\",\"defaultText\":\"<text>\"}]}");
			return sb.ToString();
		}

		public static string BuildUser(AIKeyMatchRequest request)
		{
			var sb = new StringBuilder();
			sb.AppendLine("Source language: " + request.SourceLanguageName);
			sb.AppendLine(string.IsNullOrEmpty(request.SourceText)
				? "Source text: (empty — infer it from the usage context)"
				: $"Source text: {request.SourceText}");
			if (!string.IsNullOrWhiteSpace(request.ObjectContext))
			{
				sb.AppendLine("Usage context:");
				sb.AppendLine(request.ObjectContext);
			}
			sb.AppendLine($"New key candidates requested (if no match): {Math.Max(1, request.NewKeyCandidatesCount)}");
			sb.AppendLine("Existing keys:");
			if (request.ExistingKeys == null || request.ExistingKeys.Count == 0)
			{
				sb.AppendLine("(none)");
			}
			else
			{
				foreach (var candidate in request.ExistingKeys)
				{
					sb.AppendLine($"- key: \"{candidate.Key}\" text: \"{candidate.DefaultText}\"");
				}
			}
			sb.AppendLine("Return the JSON now.");
			return sb.ToString();
		}

		public static AIKeyMatchResult Parse(string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				return AIKeyMatchResult.Fail("Empty AI response.");
			}
			var json = StripCodeFences(text);
			Payload payload;
			try { payload = JsonUtility.FromJson<Payload>(json); }
			catch (Exception e) { return AIKeyMatchResult.Fail($"Could not parse AI response: {e.Message}\n{text}"); }
			if (payload == null)
			{
				return AIKeyMatchResult.Fail($"Could not parse AI response:\n{text}");
			}

			var result = new AIKeyMatchResult
			{
				Success = true,
				MatchedKey = payload.matchedKey,
				MatchReason = payload.matchReason,
			};

			if (string.IsNullOrEmpty(result.MatchedKey) && payload.newKeySuggestions != null)
			{
				foreach (var entry in payload.newKeySuggestions)
				{
					if (entry != null && !string.IsNullOrEmpty(entry.key))
					{
						result.NewKeySuggestions.Add(new NewKeySuggestion { Key = entry.key, DefaultText = entry.defaultText });
					}
				}
			}

			return result;
		}

		// Shared with AITranslationPrompt's stripping logic, duplicated here to keep both prompt
		// builders self-contained and independently movable.
		private static string StripCodeFences(string text)
		{
			text = text.Trim();
			if (!text.StartsWith("```"))
			{
				return text;
			}
			int firstNewline = text.IndexOf('\n');
			if (firstNewline >= 0)
			{
				text = text[(firstNewline + 1)..];
			}
			int lastFence = text.LastIndexOf("```", StringComparison.Ordinal);
			if (lastFence >= 0)
			{
				text = text[..lastFence];
			}
			return text.Trim();
		}

		[Serializable] private class Payload { public string matchedKey; public string matchReason; public PayloadSuggestion[] newKeySuggestions; }
		[Serializable] private class PayloadSuggestion { public string key; public string defaultText; }
	}
}
