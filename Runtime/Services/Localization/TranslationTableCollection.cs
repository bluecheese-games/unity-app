using BlueCheese.Core.Utils;
using UnityEngine;

namespace BlueCheese.App
{
	[CreateAssetMenu(menuName = "BlueCheese/Localization/Translation Table Collection", fileName = "TranslationTableCollection")]
	public class TranslationTableCollection : AutoCollection<TranslationTableAsset>
	{
#if UNITY_EDITOR
		public override bool SearchFilter(TranslationTableAsset item, string filter)
		{
			if (string.IsNullOrEmpty(filter))
				return true;

			return item != null && (item.ContainsKey(filter) || item.ContainsTranslation(filter));
		}
#endif
	}
}
