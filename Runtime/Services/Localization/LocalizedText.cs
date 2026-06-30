//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using BlueCheese.Core.Signals;
using TMPro;
using UnityEngine;

namespace BlueCheese.App
{
	public class LocalizedText : MonoBehaviour
	{
		[SerializeField] private TMP_Text _text;
		[SerializeField] private TranslationKey _translationKey;

		private void Start()
		{
			SignalAPI.Subscribe<ChangeLanguageSignal>(OnChangeLanguage, this);
			UpdateText();
		}

		private void OnChangeLanguage(ChangeLanguageSignal signal)
		{
			UpdateText();
		}

		public void SetParameter(int index, string value, bool immediateUpdate = true)
		{
			_translationKey = _translationKey.WithParameter(index, value);
			if (immediateUpdate)
			{
				UpdateText();
			}
		}

		public void UpdateText()
		{
			if (_text == null || !_translationKey.IsValid)
			{
				return;
			}

			_text.text = _translationKey;
		}

		private void OnValidate()
		{
			if (_text == null)
			{
				_text = GetComponent<TMP_Text>();
			}

			// Only overwrite the preview text in the editor when an actual translation
			// exists, so authored placeholder text isn't clobbered by the raw key.
			if (_text != null && _translationKey.HasTranslation())
			{
				_text.text = _translationKey;
			}
		}
	}
}
