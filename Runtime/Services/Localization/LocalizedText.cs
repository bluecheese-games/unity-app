using BlueCheese.App.Editor;
using BlueCheese.Core.ServiceLocator;
using Core.Signals;
using TMPro;
using UnityEngine;

namespace BlueCheese.App
{
	public class LocalizedText : MonoBehaviour
	{
		[SerializeField] private TMP_Text _text;
		[SerializeField] private string _key;
		[SerializeField] private string _pluralKey;
		[SerializeField] private string[] _parameters;

		private bool _needsUpdate = true;

		private void Start()
		{
			SignalAPI.Subscribe<ChangeLocaleSignal>(OnLocaleChanged, this);
		}

		private void OnLocaleChanged(ChangeLocaleSignal signal)
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

		public string Key
		{
			get => _key;
			set
			{
				if (value != _key)
				{
					_key = value;
					_needsUpdate = true;
				}
			}
		}

		public string PluralKey
		{
			get => _pluralKey;
			set
			{
				if (value != _pluralKey)
				{
					_pluralKey = value;
					_needsUpdate = true;
				}
			}
		}

		public void SetParameter(int index, string value)
		{
			if (index >= 0 && index < _parameters.Length && value != _parameters[index])
			{
				_parameters[index] = value;
				_needsUpdate = true;
			}
		}

		private void UpdateText()
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
				_text.text = EditorServices.Get<ITranslationService>().Translate(GetTranslationKey());
			}
		}

		private TranslationKey GetTranslationKey() => new(_key, _parameters, _pluralKey);

		private string GetTranslation() => Services.Get<ITranslationService>().Translate(GetTranslationKey());
	}
}
