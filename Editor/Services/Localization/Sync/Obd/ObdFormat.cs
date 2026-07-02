//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;
using System.Collections.Generic;

namespace BlueCheese.App.Editor
{
	/// <summary>Maps .obd language codes to the <see cref="Language"/> enum (both directions).</summary>
	public static class ObdLanguageMap
	{
		// "GD" is the design/reference column and is intentionally not mapped to a translatable language.
		private static readonly Dictionary<string, Language> _codeToLanguage = new()
		{
			{ "EN", Language.English },
			{ "FR", Language.French },
			{ "DE", Language.German },
			{ "ES", Language.Spanish },
			{ "BR", Language.Portuguese },
			{ "IT", Language.Italian },
			{ "KR", Language.Korean },
			{ "CN", Language.ChineseSimplified },
			{ "JP", Language.Japanese },
			{ "RU", Language.Russian },
			{ "TH", Language.Thai },
			{ "VI", Language.Vietnamese },
			{ "AR", Language.Arabic },
			{ "IN", Language.Indonesian },
			{ "TR", Language.Turkish },
			{ "TW", Language.ChineseTraditional },
			{ "NL", Language.Dutch },
			{ "MY", Language.Malay },
		};

		private static readonly Dictionary<Language, string> _languageToCode = BuildReverse();

		private static Dictionary<Language, string> BuildReverse()
		{
			var reverse = new Dictionary<Language, string>();
			foreach (var pair in _codeToLanguage)
			{
				reverse[pair.Value] = pair.Key;
			}
			return reverse;
		}

		public static IReadOnlyCollection<string> Codes => _codeToLanguage.Keys;

		public static bool TryGetLanguage(string code, out Language language) => _codeToLanguage.TryGetValue(code, out language);

		public static bool TryGetCode(Language language, out string code) => _languageToCode.TryGetValue(language, out code);
	}

	/// <summary>Parses/formats the .obd timestamp format "YY:MM:DD-HH:MM:SS" (UTC).</summary>
	public static class ObdDate
	{
		public static long ParseTicks(string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return 0;
			}

			var halves = value.Split('-');
			if (halves.Length != 2)
			{
				return 0;
			}
			var date = halves[0].Split(':');
			var time = halves[1].Split(':');
			if (date.Length != 3 || time.Length != 3)
			{
				return 0;
			}

			if (!int.TryParse(date[0], out int yy) || !int.TryParse(date[1], out int mm) || !int.TryParse(date[2], out int dd) ||
				!int.TryParse(time[0], out int hh) || !int.TryParse(time[1], out int mi) || !int.TryParse(time[2], out int ss))
			{
				return 0;
			}

			try
			{
				return new DateTime(2000 + yy, mm, dd, hh, mi, ss, DateTimeKind.Utc).Ticks;
			}
			catch
			{
				return 0;
			}
		}

		public static string Format(long ticks)
		{
			if (ticks <= 0)
			{
				return null;
			}
			var dt = new DateTime(ticks, DateTimeKind.Utc);
			return $"{dt.Year % 100:00}:{dt.Month:00}:{dt.Day:00}-{dt.Hour:00}:{dt.Minute:00}:{dt.Second:00}";
		}
	}
}
