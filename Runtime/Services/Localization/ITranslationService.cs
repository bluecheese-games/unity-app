using BlueCheese.Core.ServiceLocator;
using System.Collections.Generic;

namespace BlueCheese.App
{
	public interface ITranslationService : IInitializable
	{
		void AddTranslations(Locale locale, Dictionary<string, string> translations);
		string Translate(TranslationKey key);
	}

	public class TranslationTable
	{
		private readonly Dictionary<string, string> _translations = new();

		public void Add(Dictionary<string, string> translations)
		{
			foreach (var translation in translations)
			{
				_translations[translation.Key] = translation.Value;
			}
		}

		public bool TryGet(TranslationKey key, out string singular, out string plural)
		{
			if (_translations.TryGetValue(key.Key, out singular))
			{
				plural = singular;
				if (key.PluralKey != null)
				{
					_translations.TryGetValue(key.PluralKey, out plural);
				}
				return true;
			}
			singular = null;
			plural = null;
			return false;
		}
	}

	public readonly ref struct TranslationKey
	{
		public string Key { get; }
		public object[] Parameters { get; }
		public string PluralKey { get; }
		public int PluralValue { get; }

		public TranslationKey(string key, object[] parameters = null, string pluralKey = null)
		{
			Key = key;
			Parameters = parameters;
			PluralKey = null;
			PluralValue = 0;

			// Extract plural value from parameters
			if (pluralKey != null && parameters != null && parameters.Length > 0)
			{
				foreach (object p in parameters)
				{
					if (p is int intValue || (p is string strValue && int.TryParse(strValue, out intValue)))
					{
						PluralKey = pluralKey;
						PluralValue = intValue;
						break;
					}
				}
			}
		}

		public readonly string Format(string singular, string plural = null)
		{
			if (Parameters == null || Parameters.Length == 0)
			{
				return singular;
			}
			if (!string.IsNullOrWhiteSpace(plural) && PluralValue > 1)
			{
				return string.Format(plural, Parameters);
			}
			return string.Format(singular, Parameters);
		}

		public static implicit operator string(TranslationKey key) => key.Key;
		public static implicit operator TranslationKey(string key) => new(key);
	}
}
