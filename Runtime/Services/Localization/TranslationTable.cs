//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System.Collections.Generic;

namespace BlueCheese.App
{
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
			if (key.Key == null)
			{
				singular = "";
				plural = "";
				return false;
			}
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

		public IReadOnlyList<string> GetKeys() => new List<string>(_translations.Keys);
	}
}
