using BlueCheese.Core.ServiceLocator;
using System.Collections.Generic;

namespace BlueCheese.App
{
	public interface ITranslationService : IInitializable
	{
		void AddTranslations(Locale locale, Dictionary<string, string> translations);
		string Translate(TranslationKey key);
	}
}
