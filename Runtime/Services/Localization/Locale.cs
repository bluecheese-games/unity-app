using System;
using UnityEngine;

namespace BlueCheese.App
{
	public readonly struct Locale : IEquatable<Locale>
	{
		public Locale(SystemLanguage language, string countryCode = null)
		{
			Language = language;
			CountryCode = string.IsNullOrWhiteSpace(countryCode) ? null : countryCode;
		}

		public readonly SystemLanguage Language { get; }
		public readonly string LanguageCode => LangUtilities.GetLanguageCode(Language);
		public readonly string CountryCode { get; }

		public override readonly string ToString()
		{
			if (string.IsNullOrEmpty(CountryCode))
				return LanguageCode;
			return $"{LanguageCode}-{CountryCode}";
		}

		public override bool Equals(object obj)
		{
			var other = (Locale)obj;
			return other.Language == Language && other.CountryCode == CountryCode;
		}
		public bool Equals(Locale other) => other.Language == Language && other.CountryCode == CountryCode;

		public override int GetHashCode() => HashCode.Combine(Language, CountryCode);

		public static bool operator ==(Locale a, Locale b) => a.Equals(b);
		public static bool operator !=(Locale a, Locale b) => !a.Equals(b);

		public static implicit operator string(Locale locale) => locale.ToString();
		public static implicit operator Locale(string locale)
		{
			var parts = locale.Split('-');
			string languageCode = parts[0];
			var language = LangUtilities.GetSystemLanguage(languageCode);
			string countryCode = parts.Length > 1 ? parts[1] : null;
			return new Locale(language, countryCode);
		}

		public static implicit operator Locale(SystemLanguage language) => new(language);
		public static implicit operator SystemLanguage(Locale locale) => locale.Language;
	}
}
