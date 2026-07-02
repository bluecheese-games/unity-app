//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BlueCheese.App.Editor
{
	/// <summary>
	/// Local ".obd" file source. The file is a JSON array of entries; each entry has an "ID" (the key),
	/// per-language value fields (EN, FR, …) and matching "{CODE}_UpdateDate" timestamps.
	/// Reads/writes are round-trip friendly: writing only touches changed cells and preserves everything
	/// else (order, the "GD" reference column, untouched languages, null vs empty).
	/// </summary>
	public class ObdTranslationSource : ITranslationSource
	{
		public const string SourceTypeId = "obd";

		private readonly string _path; // project-relative asset path (works directly with File IO in the editor)
		private JArray _document;       // cached parsed document, reused for round-trip writes

		public ObdTranslationSource(string path) => _path = path;

		public bool CanRead => File.Exists(_path);
		public bool CanWrite => true;

		public TranslationSnapshot Read()
		{
			var snapshot = new TranslationSnapshot();
			if (!File.Exists(_path))
			{
				return snapshot;
			}

			_document = JArray.Parse(File.ReadAllText(_path, Encoding.UTF8));
			foreach (var token in _document)
			{
				if (token is not JObject obj)
				{
					continue;
				}
				var id = obj["ID"]?.ToString();
				if (string.IsNullOrEmpty(id))
				{
					continue;
				}

				var entry = snapshot.GetOrAdd(id);
				foreach (var code in ObdLanguageMap.Codes)
				{
					if (!ObdLanguageMap.TryGetLanguage(code, out var language))
					{
						continue;
					}
					var valueToken = obj[code];
					if (valueToken == null || valueToken.Type == JTokenType.Null)
					{
						continue; // language absent for this entry
					}
					long ticks = ObdDate.ParseTicks(obj[code + "_UpdateDate"]?.ToString());
					entry.Set(language, valueToken.ToString(), ticks);
				}
			}
			return snapshot;
		}

		public void Write(TranslationSnapshot snapshot)
		{
			var document = _document;
			if (document == null)
			{
				document = File.Exists(_path) ? JArray.Parse(File.ReadAllText(_path, Encoding.UTF8)) : new JArray();
			}

			var byId = new Dictionary<string, JObject>();
			foreach (var token in document)
			{
				if (token is JObject obj && obj["ID"] != null)
				{
					byId[obj["ID"].ToString()] = obj;
				}
			}

			bool changed = false;
			foreach (var entry in snapshot.Entries.Values)
			{
				if (!byId.TryGetValue(entry.Key, out var obj))
				{
					obj = new JObject { ["ID"] = entry.Key };
					document.Add(obj);
					byId[entry.Key] = obj;
					changed = true;
				}

				foreach (var pair in entry.Cells)
				{
					if (!ObdLanguageMap.TryGetCode(pair.Key, out var code))
					{
						continue;
					}
					var newValue = pair.Value.Value ?? string.Empty;
					var existing = obj[code]?.ToString();
					if (existing == newValue)
					{
						continue; // unchanged: keep the existing value and date untouched
					}

					obj[code] = newValue;
					var date = ObdDate.Format(pair.Value.Ticks);
					obj[code + "_UpdateDate"] = date != null ? (JToken)date : JValue.CreateNull();
					changed = true;
				}
			}

			if (changed)
			{
				File.WriteAllText(_path, document.ToString(Formatting.Indented), new UTF8Encoding(false));
				// Skip the reimport our own write triggers, so the importer doesn't loop.
				TranslationSyncGuard.Suppress(_path);
			}

			_document = document;
		}
	}
}
