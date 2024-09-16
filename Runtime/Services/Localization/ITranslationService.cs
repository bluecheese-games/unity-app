using BlueCheese.Core.ServiceLocator;
using System.Collections.Generic;
using UnityEngine;

namespace BlueCheese.App
{
	public interface ITranslationService : IInitializable
	{
		void AddTranslations(SystemLanguage language, Dictionary<string, string> translations);
		string Translate(TranslationKey key);
	}
}
