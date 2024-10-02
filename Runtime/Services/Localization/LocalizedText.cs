using Core.Signals;
using TMPro;
using UnityEngine;

namespace BlueCheese.App
{
	public class LocalizedText : MonoBehaviour
	{
		[SerializeField] private TMP_Text _text;
		[SerializeField] private TranslationKey _translationKey;

		private bool _needsUpdate = true;

		private void Start()
		{
			SignalAPI.Subscribe<ChangeLanguageSignal>(OnChangeLanguage, this);
		}

		private void OnChangeLanguage(ChangeLanguageSignal signal)
		{
			_needsUpdate = true;
		}

		private void Update()
		{
			if (_needsUpdate)
			{
				UpdateText();
				_needsUpdate = false;
			}
		}

		public void SetParameter(int index, string value)
		{
			_translationKey.SetParameter(index, value);
			_needsUpdate = true;
		}

		public void UpdateText()
		{
			if (_text == null)
			{
				return;
			}

			_text.text = GetTranslation();
		}

		private void OnValidate()
		{
			if (_text == null)
			{
				_text = GetComponent<TMP_Text>();
			}

			if (_text != null)
			{
				_text.text = _translationKey;
			}
		}

		private string GetTranslation() => _translationKey;
	}
}
