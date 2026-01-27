//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using BlueCheese.Core.ServiceLocator;
using System.Collections.Generic;

namespace BlueCheese.App
{
	public interface ITranslationService : IInitializable
	{
		void AddTranslations(Language language, IDictionary<string, string> translations);
		string Translate(TranslationKey key);
		bool HasTranslation(TranslationKey key);
	}
}
