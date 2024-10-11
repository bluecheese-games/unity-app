//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using BlueCheese.App.Editor;
using UnityEngine;

namespace BlueCheese.App
{
	public static class Translator
	{
		private static ITranslationService _translationService;

		public static void Initialize(ITranslationService translationService)
		{
			_translationService = translationService;
		}

		public static string Translate(TranslationKey key)
		{
			if (_translationService == null)
			{
				if (Application.isPlaying)
				{
					return key.Key;
				}
				else
				{
					Initialize(EditorServices.Get<ITranslationService>());
				}
			}
			if (_translationService == null)
			{
				return key.Key;
			}
			return _translationService.Translate(key);
		}
	}
}
