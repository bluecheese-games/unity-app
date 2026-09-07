using BlueCheese.Core.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace BlueCheese.App
{
	[CreateAssetMenu(menuName = "BlueCheese/Localization/Localization Settings", fileName = "LocalizationSettings")]
    public class LocalizationSettingsAsset : AssetBase
	{
		public Language DefaultLanguage = Language.English;
		public List<Language> SupportedLanguages;

		public LocalizationService.Settings Options => new()
		{
			DefaultLanguage = DefaultLanguage,
			SupportedLanguages = SupportedLanguages
		};
    }
}
