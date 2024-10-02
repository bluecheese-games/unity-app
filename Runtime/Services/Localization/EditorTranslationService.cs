using System.Collections.Generic;

namespace BlueCheese.App
{
	// Decorator for TranslationService that adds editor-specific functionality
	public class EditorTranslationService : TranslationService
	{
		public EditorTranslationService(ILocalizationService localization, IAssetLoaderService assetLoader) : base(localization, assetLoader) { }

		public IReadOnlyList<string> GetAllKeys()
		{
			// Get all keys from all translation tables, combine, remove duplicates, sort by key and return
			var keys = new HashSet<string>();
			foreach (var table in _translations.Values)
			{
				foreach (var key in table.GetKeys())
				{
					keys.Add(key);
				}
			}
			var sortedKeys = new List<string>(keys);
			sortedKeys.Sort();
			return sortedKeys;
		}
	}
}
