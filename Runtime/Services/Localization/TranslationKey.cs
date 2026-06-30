//
// Copyright (c) 2026 BlueCheese Games All rights reserved
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

		public readonly string Key => _key;
		public readonly string PluralKey => _pluralKey;
		public readonly IReadOnlyList<string> Parameters => _parameters;

		/// <summary>
		/// True when a key has been assigned. This is a cheap structural check; it does
		/// not perform a translation lookup. Use <see cref="HasTranslation"/> for that.
		/// </summary>
		public readonly bool IsValid => !string.IsNullOrEmpty(_key);

		public TranslationKey(string key, string[] parameters = null, string pluralKey = null)
		{
			_key = key;
			_parameters = parameters;
			_pluralKey = pluralKey;
		}

		/// <summary>
		/// Returns a copy of this key with the parameter at <paramref name="index"/> replaced.
		/// The original key is left untouched (TranslationKey is immutable).
		/// </summary>
		public readonly TranslationKey WithParameter(int index, string value)
		{
			if (_parameters == null || index < 0 || index >= _parameters.Length || _parameters[index] == value)
			{
				return this;
			}

			var parameters = (string[])_parameters.Clone();
			parameters[index] = value;
			return new TranslationKey(_key, parameters, _pluralKey);
		}

		private readonly int GetPluralValue()
		{
			if (_parameters != null)
			{
				// Use the first parameter that parses as an integer as the plural count.
				foreach (string p in _parameters)
				{
					if (int.TryParse(p, out int intValue))
					{
						return intValue;
					}
				}
			}
			return 0;
		}

		public readonly string Format(string singular, string plural = null)
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
		public readonly bool HasTranslation() => Translator.HasTranslation(this);

		public static implicit operator string(TranslationKey key) => key.Translate();
		public static implicit operator TranslationKey(string key) => new(key);
	}
}
