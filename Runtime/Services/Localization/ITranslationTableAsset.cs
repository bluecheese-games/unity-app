//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;
using System.Collections.Generic;

namespace BlueCheese.App
{
	public interface ITranslationTableAsset
	{
		string Name { get; }
		List<string> Keys { get; }
		List<Language> Languages { get; }

		IDictionary<string, string> GetTranslations(Language language);
		bool ContainsKey(string searchText);
		bool ContainsTranslation(string searchText);
	}
}