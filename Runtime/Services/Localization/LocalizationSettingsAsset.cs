using System.Collections.Generic;
using UnityEngine;

namespace BlueCheese.App
{
    [CreateAssetMenu(menuName = "Localization/Settings", fileName = "LocalizationSettings")]
    public class LocalizationSettingsAsset : ScriptableObject
	{
		public SystemLanguage DefaultLanguage = SystemLanguage.English;
		public List<SystemLanguage> SupportedLanguages;

		public LocalizationService.Options Options => new()
		{
			DefaultLanguage = DefaultLanguage,
			SupportedLanguages = SupportedLanguages
		};
    }
}
