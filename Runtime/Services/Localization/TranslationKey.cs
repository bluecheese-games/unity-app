//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlueCheese.App
{
	[Serializable]
	public struct TranslationKey
	{
		[SerializeField] private string _key;
		[SerializeField] private string _pluralKey;
		[SerializeField] private string[] _parameters;

		private int? _pluralValue;

		public readonly string Key => _key;
		public readonly string PluralKey => _pluralKey;
		public readonly IReadOnlyList<string> Parameters => _parameters;

		private int GetPluralValue()
		{
			if (_pluralValue == null && _parameters != null && _parameters.Length > 0)
			{
				// Extract plural value from parameters
				foreach (string p in _parameters)
				{
					if (int.TryParse(p, out int intValue))
					{
						_pluralValue = intValue;
						break;
					}
				}
			}
			return _pluralValue.HasValue ? _pluralValue.Value : 0;
		}

		public TranslationKey(string key, string[] parameters = null, string pluralKey = null)
		{
			_key = key;
			_parameters = parameters;
			_pluralKey = pluralKey;
			_pluralValue = null;
		}

		public void SetParameter(int index, string value)
		{
			if (index >= 0 && index < _parameters.Length && value != _parameters[index])
			{
				_parameters[index] = value;
				_pluralValue = null;
			}
		}

		public string Format(string singular, string plural = null)
		{
			if (_parameters == null || _parameters.Length == 0)
			{
				return singular;
			}
			if (!string.IsNullOrWhiteSpace(plural) && GetPluralValue() > 1)
			{
				return string.Format(plural, _parameters);
			}
			return string.Format(singular, _parameters);
		}

		public readonly string Translate() => Translator.Translate(this);

		public static implicit operator string(TranslationKey key) => key.Translate();
		public static implicit operator TranslationKey(string key) => new(key);
	}
}
