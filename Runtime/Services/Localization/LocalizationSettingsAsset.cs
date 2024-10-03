using System.Collections.Generic;
using UnityEngine;

namespace BlueCheese.App
{
	[CreateAssetMenu(menuName = "Localization/Settings", fileName = "LocalizationSettings")]
    public class LocalizationSettingsAsset : ScriptableObject
	{
		public Language DefaultLanguage = Language.English;
		public List<Language> SupportedLanguages;

		public LocalizationService.Options Options => new()
		{
			DefaultLanguage = DefaultLanguage,
			SupportedLanguages = SupportedLanguages
		};
    }
}
