//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using System.Collections.Generic;

namespace BlueCheese.App
{
	public interface ITranslationTableAsset
	{
		string Name { get; }
		List<string> Keys { get; }
		List<Language> Languages { get; }

		bool ContainsKey(string searchText);
		bool ContainsTranslation(string searchText);
		void Load(ITranslationService translationService);
		void Open();
	}
}