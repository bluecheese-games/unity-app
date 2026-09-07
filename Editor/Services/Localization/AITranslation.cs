//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace BlueCheese.App.Editor
{
	public enum AITranslationProviderKind
	{
		None,
		Anthropic,
		Gemini,
	}

	#region Contracts

	/// <summary>Everything the AI needs to translate a single key with maximum context.</summary>
	public class AITranslationRequest
	{
		public string Key;
		public string SourceLanguageName;
		public string SourceText;              // default-language text; may be empty (AI will infer it)
		public string UsageContext;            // where/how the key is used (scenes, prefabs, hierarchy…)
		public int MaxChars;                   // per-translation character budget (0 = unconstrained)
		public string Glossary;                // pre-formatted validated translations for consistency
		public string Guidelines;              // custom tone/rules/forbidden words…
		public string ProjectName;
		public string ProjectInfo;
		public int AlternativesCount = 1;      // number of options requested per language (ordered by likelihood)
		public System.Collections.Generic.List<string> TargetLanguageNames = new();
	}

	public class AITranslationEntry
	{
		public Language Language;
		public System.Collections.Generic.List<string> Options = new(); // ordered by likelihood; [0] = most probable
	}

	public class AITranslationResult
	{
		public bool Success;
		public string Error;
		public string GeneratedSource;                       // AI-proposed default text when the source was empty
		public System.Collections.Generic.List<AITranslationEntry> Entries = new();

		public static AITranslationResult Fail(string error) => new() { Success = false, Error = error };
	}

	/// <summary>A pluggable AI backend. Editor-only, callback-based (no UniTask dependency).</summary>
	public interface IAITranslationProvider
	{
		void Translate(AITranslationRequest request, Action<AITranslationResult> onComplete);

		/// <summary>Finds the best existing key for a text, or proposes new key candidates.</summary>
		void MatchKey(AIKeyMatchRequest request, Action<AIKeyMatchResult> onComplete);

		/// <summary>Minimal round-trip to verify the endpoint/model/API key. onResult(success, message).</summary>
		void TestConnection(Action<bool, string> onResult);
	}

	#endregion

	#region Applying results

	/// <summary>Shared logic to apply an <see cref="AITranslationResult"/> to a table item (no alternatives UI).</summary>
	public static class AITranslationApplier
	{
		public static void ApplyMostProbable(TranslationTableAsset asset, TranslationTableAsset.TranslationItem item, Language defaultLanguage, AITranslationResult result)
		{
			if (asset.IsLanguageSupported(defaultLanguage)
				&& string.IsNullOrEmpty(asset.GetTranslation(item.Key, defaultLanguage))
				&& !string.IsNullOrEmpty(result.GeneratedSource))
			{
				asset.SetTranslation(defaultLanguage, item.Key, result.GeneratedSource, aiTranslated: true);
			}

			foreach (var entry in result.Entries)
			{
				if (entry.Language == defaultLanguage || !asset.IsLanguageSupported(entry.Language) || entry.Options.Count == 0)
				{
					continue;
				}
				asset.SetTranslation(entry.Language, item.Key, entry.Options[0], aiTranslated: true);
			}
		}
	}

	#endregion

	#region Catalog & factory

	public static class AIModelCatalog
	{
		// Predefined suggestions per provider. Users can still type any custom model id.
		public static string[] For(AITranslationProviderKind kind) => kind switch
		{
			AITranslationProviderKind.Anthropic => new[]
			{
				"claude-sonnet-4-5", "claude-opus-4-1", "claude-haiku-4-5", "claude-3-5-sonnet-latest", "claude-3-5-haiku-latest",
			},
			AITranslationProviderKind.Gemini => new[]
			{
				"gemini-3.1-pro", "gemini-3.1-flash", "gemini-3.1-flash-lite",
				"gemini-2.5-pro", "gemini-2.5-flash", "gemini-2.5-flash-lite",
				"gemini-2.0-flash", "gemini-2.0-flash-lite",
				"gemini-1.5-pro", "gemini-1.5-flash",
			},
			_ => Array.Empty<string>(),
		};
	}

	public static class AIProviders
	{
		public static IAITranslationProvider Create(AITranslationSettings settings)
		{
			var model = settings.GetModel();
			var apiKey = AITranslationSettings.GetApiKey(settings.Provider);
			return settings.Provider switch
			{
				AITranslationProviderKind.Anthropic => new AnthropicTranslationProvider(model, apiKey),
				AITranslationProviderKind.Gemini => new GeminiTranslationProvider(model, apiKey),
				_ => null,
			};
		}
	}

	#endregion

	#region Shared prompt + HTTP

	/// <summary>Provider-agnostic prompt building and response parsing (shared JSON schema).</summary>
	internal static class AITranslationPrompt
	{
		public static string BuildSystem(AITranslationRequest request)
		{
			var sb = new StringBuilder();
			sb.AppendLine("You are an expert video game localizer. Translate UI/game strings accurately and idiomatically.");
			if (!string.IsNullOrWhiteSpace(request.ProjectName)) sb.AppendLine($"Project: {request.ProjectName}.");
			if (!string.IsNullOrWhiteSpace(request.ProjectInfo)) sb.AppendLine(request.ProjectInfo);
			if (!string.IsNullOrWhiteSpace(request.Guidelines))
			{
				sb.AppendLine("Guidelines:");
				sb.AppendLine(request.Guidelines);
			}
			sb.AppendLine("Rules:");
			sb.AppendLine("- Preserve format placeholders such as {0}, {1} exactly.");
			sb.AppendLine("- Preserve TextMesh Pro rich text tags (e.g. <b>, <color=#fff>) exactly.");
			sb.AppendLine("- Produce natural translations suited to the UI usage context.");
			sb.AppendLine("- If a maximum character count is given, keep every translation within it.");
			sb.AppendLine("- If the source text is missing, infer it from the key name and usage context, and return it as \"source\".");
			sb.AppendLine($"- Provide exactly {Math.Max(1, request.AlternativesCount)} option(s) per language in \"options\", ordered from most to least likely.");
			sb.AppendLine("- Output ONLY valid minified JSON, no markdown, matching exactly:");
			sb.AppendLine("  {\"source\":\"<default-language text>\",\"translations\":[{\"language\":\"<Language>\",\"options\":[\"<most likely>\",\"<alternative>\"]}]}");
			sb.AppendLine("- Use these exact language names in the \"language\" field.");
			return sb.ToString();
		}

		public static string BuildUser(AITranslationRequest request)
		{
			var sb = new StringBuilder();
			sb.AppendLine($"Key: {request.Key}");
			sb.AppendLine($"Source language: {request.SourceLanguageName}");
			sb.AppendLine(string.IsNullOrEmpty(request.SourceText)
				? "Source text: (missing — infer it from the key and usage context)"
				: $"Source text: {request.SourceText}");
			if (!string.IsNullOrWhiteSpace(request.UsageContext))
			{
				sb.AppendLine("Usage context:");
				sb.AppendLine(request.UsageContext);
			}
			sb.AppendLine(request.MaxChars > 0 ? $"Max characters per translation: {request.MaxChars}" : "Max characters per translation: n/a");
			if (!string.IsNullOrWhiteSpace(request.Glossary))
			{
				sb.AppendLine("Existing validated translations (keep terminology consistent):");
				sb.AppendLine(request.Glossary);
			}
			sb.AppendLine($"Options per language: {Math.Max(1, request.AlternativesCount)}");
			sb.AppendLine($"Translate into these languages: {string.Join(", ", request.TargetLanguageNames)}");
			sb.AppendLine("Return the JSON now.");
			return sb.ToString();
		}

		public static AITranslationResult Parse(string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				return AITranslationResult.Fail("Empty AI response.");
			}
			var json = StripCodeFences(text);
			Payload payload;
			try { payload = JsonUtility.FromJson<Payload>(json); }
			catch (Exception e) { return AITranslationResult.Fail($"Could not parse AI response: {e.Message}\n{text}"); }
			if (payload == null)
			{
				return AITranslationResult.Fail($"Could not parse AI response:\n{text}");
			}

			var result = new AITranslationResult { Success = true, GeneratedSource = payload.source };
			if (payload.translations != null)
			{
				foreach (var entry in payload.translations)
				{
					if (entry == null || !Enum.TryParse<Language>(entry.language, ignoreCase: true, out var language) || language == Language.Unknown)
					{
						continue;
					}
					var mapped = new AITranslationEntry { Language = language };
					if (entry.options != null)
					{
						foreach (var option in entry.options)
						{
							if (!string.IsNullOrEmpty(option))
							{
								mapped.Options.Add(option);
							}
						}
					}
					if (mapped.Options.Count > 0)
					{
						result.Entries.Add(mapped);
					}
				}
			}
			return result;
		}

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

		[Serializable] private class Payload { public string source; public PayloadEntry[] translations; }
		[Serializable] private class PayloadEntry { public string language; public string[] options; }
	}

	/// <summary>Fire-and-poll HTTP POST for the editor (no UniTask dependency).</summary>
	internal static class EditorWebRequest
	{
		// onSuccess receives the response body plus a header accessor (valid only during the callback).
		public static void Post(string url, string json, (string name, string value)[] headers, Action<string, Func<string, string>> onSuccess, Action<string> onError)
		{
			var request = new UnityWebRequest(url, "POST")
			{
				uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
				downloadHandler = new DownloadHandlerBuffer(),
			};
			request.SetRequestHeader("content-type", "application/json");
			if (headers != null)
			{
				foreach (var header in headers)
				{
					request.SetRequestHeader(header.name, header.value);
				}
			}
			request.SendWebRequest();

			EditorApplication.CallbackFunction poll = null;
			poll = () =>
			{
				if (!request.isDone)
				{
					return;
				}
				EditorApplication.update -= poll;
				try
				{
					if (request.result != UnityWebRequest.Result.Success)
					{
						onError($"HTTP {request.responseCode}: {request.error}\n{request.downloadHandler.text}");
					}
					else
					{
						onSuccess(request.downloadHandler.text, request.GetResponseHeader);
					}
				}
				finally
				{
					request.Dispose();
				}
			};
			EditorApplication.update += poll;
		}
	}

	#endregion

	#region Anthropic

	public class AnthropicTranslationProvider : IAITranslationProvider
	{
		private const string Endpoint = "https://api.anthropic.com/v1/messages";
		private const string AnthropicVersion = "2023-06-01";

		private readonly string _model;
		private readonly string _apiKey;

		public AnthropicTranslationProvider(string model, string apiKey)
		{
			_model = string.IsNullOrEmpty(model) ? "claude-sonnet-4-5" : model;
			_apiKey = apiKey;
		}

		public void Translate(AITranslationRequest request, Action<AITranslationResult> onComplete)
		{
			if (string.IsNullOrEmpty(_apiKey))
			{
				onComplete(AITranslationResult.Fail("No Anthropic API key set (see AI Translation Settings)."));
				return;
			}

			var body = new Body
			{
				model = _model,
				max_tokens = 4096,
				system = AITranslationPrompt.BuildSystem(request),
				messages = new[] { new Msg { role = "user", content = AITranslationPrompt.BuildUser(request) } },
			};

			EditorWebRequest.Post(Endpoint, JsonUtility.ToJson(body),
				new[] { ("x-api-key", _apiKey), ("anthropic-version", AnthropicVersion) },
				(text, _) =>
				{
					try
					{
						var response = JsonUtility.FromJson<Response>(text);
						var content = response?.content != null && response.content.Length > 0 ? response.content[0].text : null;
						onComplete(AITranslationPrompt.Parse(content));
					}
					catch (Exception e) { onComplete(AITranslationResult.Fail(e.Message)); }
				},
				error => onComplete(AITranslationResult.Fail(error)));
		}

		public void MatchKey(AIKeyMatchRequest request, Action<AIKeyMatchResult> onComplete)
		{
			if (string.IsNullOrEmpty(_apiKey))
			{
				onComplete(AIKeyMatchResult.Fail("No Anthropic API key set (see AI Translation Settings)."));
				return;
			}

			var body = new Body
			{
				model = _model,
				max_tokens = 4096,
				system = AIKeyMatchPrompt.BuildSystem(request),
				messages = new[] { new Msg { role = "user", content = AIKeyMatchPrompt.BuildUser(request) } },
			};

			EditorWebRequest.Post(Endpoint, JsonUtility.ToJson(body),
				new[] { ("x-api-key", _apiKey), ("anthropic-version", AnthropicVersion) },
				(text, _) =>
				{
					try
					{
						var response = JsonUtility.FromJson<Response>(text);
						var content = response?.content != null && response.content.Length > 0 ? response.content[0].text : null;
						onComplete(AIKeyMatchPrompt.Parse(content));
					}
					catch (Exception e) { onComplete(AIKeyMatchResult.Fail(e.Message)); }
				},
				error => onComplete(AIKeyMatchResult.Fail(error)));
		}

		public void TestConnection(Action<bool, string> onResult)
		{
			if (string.IsNullOrEmpty(_apiKey))
			{
				onResult(false, "No Anthropic API key set.");
				return;
			}
			var body = new Body
			{
				model = _model,
				max_tokens = 1,
				system = "ping",
				messages = new[] { new Msg { role = "user", content = "ping" } },
			};
			EditorWebRequest.Post(Endpoint, JsonUtility.ToJson(body),
				new[] { ("x-api-key", _apiKey), ("anthropic-version", AnthropicVersion) },
				(_, header) =>
				{
					var message = new StringBuilder($"Connected to Anthropic ({_model}).");

					var requestsRemaining = header("anthropic-ratelimit-requests-remaining");
					var requestsLimit = header("anthropic-ratelimit-requests-limit");
					if (!string.IsNullOrEmpty(requestsRemaining) && !string.IsNullOrEmpty(requestsLimit))
					{
						message.Append($"\nRequests: {requestsRemaining}/{requestsLimit} remaining (current window).");
					}

					var tokensRemaining = header("anthropic-ratelimit-tokens-remaining");
					var tokensLimit = header("anthropic-ratelimit-tokens-limit");
					if (!string.IsNullOrEmpty(tokensRemaining) && !string.IsNullOrEmpty(tokensLimit))
					{
						message.Append($"\nTokens: {tokensRemaining}/{tokensLimit} remaining (current window).");
					}

					onResult(true, message.ToString());
				},
				error => onResult(false, error));
		}

		[Serializable] private class Body { public string model; public int max_tokens; public string system; public Msg[] messages; }
		[Serializable] private class Msg { public string role; public string content; }
		[Serializable] private class Response { public Content[] content; }
		[Serializable] private class Content { public string type; public string text; }
	}

	#endregion

	#region Gemini

	public class GeminiTranslationProvider : IAITranslationProvider
	{
		private const string EndpointFormat = "https://generativelanguage.googleapis.com/v1beta/models/{0}:generateContent?key={1}";

		private readonly string _model;
		private readonly string _apiKey;

		public GeminiTranslationProvider(string model, string apiKey)
		{
			_model = string.IsNullOrEmpty(model) ? "gemini-2.5-flash" : model;
			_apiKey = apiKey;
		}

		public void Translate(AITranslationRequest request, Action<AITranslationResult> onComplete)
		{
			if (string.IsNullOrEmpty(_apiKey))
			{
				onComplete(AITranslationResult.Fail("No Gemini API key set (see AI Translation Settings)."));
				return;
			}

			var url = string.Format(EndpointFormat, _model, UnityWebRequest.EscapeURL(_apiKey));
			var body = new Body
			{
				systemInstruction = new SystemInstruction { parts = new[] { new Part { text = AITranslationPrompt.BuildSystem(request) } } },
				contents = new[] { new Content { role = "user", parts = new[] { new Part { text = AITranslationPrompt.BuildUser(request) } } } },
				generationConfig = new GenConfig { responseMimeType = "application/json" },
			};

			EditorWebRequest.Post(url, JsonUtility.ToJson(body), null, // Gemini takes the key in the query string
				(text, _) =>
				{
					try
					{
						var response = JsonUtility.FromJson<Response>(text);
						var part = response?.candidates != null && response.candidates.Length > 0
							&& response.candidates[0].content?.parts != null && response.candidates[0].content.parts.Length > 0
							? response.candidates[0].content.parts[0].text
							: null;
						onComplete(AITranslationPrompt.Parse(part));
					}
					catch (Exception e) { onComplete(AITranslationResult.Fail(e.Message)); }
				},
				error => onComplete(AITranslationResult.Fail(error)));
		}

		public void MatchKey(AIKeyMatchRequest request, Action<AIKeyMatchResult> onComplete)
		{
			if (string.IsNullOrEmpty(_apiKey))
			{
				onComplete(AIKeyMatchResult.Fail("No Gemini API key set (see AI Translation Settings)."));
				return;
			}

			var url = string.Format(EndpointFormat, _model, UnityWebRequest.EscapeURL(_apiKey));
			var body = new Body
			{
				systemInstruction = new SystemInstruction { parts = new[] { new Part { text = AIKeyMatchPrompt.BuildSystem(request) } } },
				contents = new[] { new Content { role = "user", parts = new[] { new Part { text = AIKeyMatchPrompt.BuildUser(request) } } } },
				generationConfig = new GenConfig { responseMimeType = "application/json" },
			};

			EditorWebRequest.Post(url, JsonUtility.ToJson(body), null,
				(text, _) =>
				{
					try
					{
						var response = JsonUtility.FromJson<Response>(text);
						var part = response?.candidates != null && response.candidates.Length > 0
							&& response.candidates[0].content?.parts != null && response.candidates[0].content.parts.Length > 0
							? response.candidates[0].content.parts[0].text
							: null;
						onComplete(AIKeyMatchPrompt.Parse(part));
					}
					catch (Exception e) { onComplete(AIKeyMatchResult.Fail(e.Message)); }
				},
				error => onComplete(AIKeyMatchResult.Fail(error)));
		}

		public void TestConnection(Action<bool, string> onResult)
		{
			if (string.IsNullOrEmpty(_apiKey))
			{
				onResult(false, "No Gemini API key set.");
				return;
			}
			var url = string.Format(EndpointFormat, _model, UnityWebRequest.EscapeURL(_apiKey));
			var body = new Body
			{
				systemInstruction = new SystemInstruction { parts = new[] { new Part { text = "ping" } } },
				contents = new[] { new Content { role = "user", parts = new[] { new Part { text = "ping" } } } },
				generationConfig = new GenConfig { responseMimeType = "text/plain" },
			};
			EditorWebRequest.Post(url, JsonUtility.ToJson(body), null,
				(_, _) => onResult(true, $"Connected to Gemini ({_model}).\nRequest quota is not exposed by the Gemini API (see Google Cloud console)."),
				error => onResult(false, error));
		}

		[Serializable] private class Body { public SystemInstruction systemInstruction; public Content[] contents; public GenConfig generationConfig; }
		[Serializable] private class SystemInstruction { public Part[] parts; }
		[Serializable] private class Content { public string role; public Part[] parts; }
		[Serializable] private class Part { public string text; }
		[Serializable] private class GenConfig { public string responseMimeType; }
		[Serializable] private class Response { public Candidate[] candidates; }
		[Serializable] private class Candidate { public Content content; }
	}

	#endregion
}
