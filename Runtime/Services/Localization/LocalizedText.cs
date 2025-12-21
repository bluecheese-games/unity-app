//
// Copyright (c) 2025 BlueCheese Games All rights reserved
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
			_translationKey.SetParameter(index, value);
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

			if (_text != null && _translationKey.IsValid)
			{
				_text.text = _translationKey;
			}
		}
	}
}
