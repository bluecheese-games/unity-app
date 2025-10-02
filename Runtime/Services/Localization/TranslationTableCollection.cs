//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using BlueCheese.Core.Utils;
using System.Linq;
using UnityEngine;

namespace BlueCheese.App
{
	[CreateAssetMenu(menuName = "Collection/Translation Table Collection", fileName = "TranslationTableCollection")]
	public class TranslationTableCollection : Collection<TranslationTableAsset>
	{
#if UNITY_EDITOR
		public override void OnRegister()
		{
			base.OnRegister();

			// Get TranslationTableAsset
			var translationService = Editor.EditorServices.Get<EditorTranslationService>();
			var assets = translationService.TranslationTableAssets;

			// Cleanup empty or null entries
			_items.RemoveAll(item => item == null);

			// Add any assets that are not already in the collection
			foreach (var asset in assets)
			{
				if (!_items.Contains(asset))
				{
					_items.Add(asset);
				}
			}
		}
#endif
	}
}
