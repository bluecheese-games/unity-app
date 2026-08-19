//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using BlueCheese.Core.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlueCheese.App
{
	[CreateAssetMenu(fileName = "AudioSettings", menuName = "BlueCheese/Audio/AudioSettings", order = 1)]
	public class AudioSettings : AutoCollection<AudioBank>
	{
		[SerializeField] private int _audioPoolCapacity = 10;
		private Func<AudioItemPlayer> _audioPlayerFactory = null;

		/// <summary>
		/// Custom audio player factory.
		/// </summary>
		public Func<AudioItemPlayer> AudioPlayerFactory => _audioPlayerFactory;

		/// <summary>
		/// The AudioPlayer pool size.
		/// </summary>
		public int AudioPoolCapacity => _audioPoolCapacity;

		public static AudioSettings FromAssetBankOrDefault()
		{
			var settings = AssetBank.GetAssetOfType<AudioSettings>();
			if (settings == null)
			{
				Debug.LogWarning("AudioSettings asset not found. Using default settings.");
				settings = ScriptableObject.CreateInstance<AudioSettings>();
				settings._audioPoolCapacity = 10; // Default pool size
			}
			return settings;
		}

		public void AddAudioBank(AudioBank bank)
		{
			_items ??= new List<AudioBank>();
			if (bank != null && !_items.Contains(bank))
			{
				_items.Add(bank);
			}
		}

		public void SetAudioPlayerFactory(Func<AudioItemPlayer> factory)
		{
			_audioPlayerFactory = factory;
		}
	}
}
