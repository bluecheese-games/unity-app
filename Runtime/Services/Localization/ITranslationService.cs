using BlueCheese.Core.ServiceLocator;
using System.Collections.Generic;

namespace BlueCheese.App
{
	public interface ITranslationService : IInitializable
	{
		void AddTranslations(Language language, Dictionary<string, string> translations);
		string Translate(TranslationKey key);
	}
}
