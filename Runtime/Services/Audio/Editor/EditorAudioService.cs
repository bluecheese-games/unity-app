//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using BlueCheese.Core.ServiceLocator;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlueCheese.App.Editor
{
	public class EditorAudioService : IInitializable
	{
		private readonly IAssetFinderService _assetFinder;

		private readonly Dictionary<string, AudioItem> _items = new();

		private AudioSource _audioSource;

		public EditorAudioService(IAssetFinderService assetFinder)
		{
			_assetFinder = assetFinder;
		}

		public void Initialize()
		{
			// Find all audio banks in resources
			var audioBanks = _assetFinder.FindAssetsInResources<AudioBank>();

			_items.Clear();
			foreach (var bank in audioBanks)
			{
				foreach (var item in bank.Items)
				{
					_items.Add(item.Name, item);
				}
			}
		}

		public string[] AllSounds
		{
			get
			{
				Initialize();
				return _items.Keys.ToArray();
			}
		}

		public void PlaySound(SoundFX sound)
		{
			StopAll();

			if (!_items.TryGetValue(sound.Name, out AudioItem item))
			{
				Debug.LogWarning($"Audio item {sound.Name} not found");
				return;
			}

			GameObject gameObject = new("AudioPlayer_" + sound.Name);
			_audioSource = gameObject.AddComponent<AudioSource>();
			gameObject.hideFlags = HideFlags.HideAndDontSave;

			_audioSource.clip = item.Clip;
			_audioSource.volume = item.Volume * sound.Options.Volume;
			_audioSource.loop = sound.Options.Loop;
			_audioSource.pitch = Random.Range(sound.Options.Pitch.x, sound.Options.Pitch.y);
			_audioSource.spatialize = false;
			_audioSource.Play();
		}

		public bool IsPlaying() => _audioSource != null && _audioSource.isPlaying;

		public void StopAll()
		{
			if (IsPlaying())
			{
				_audioSource.Stop();
			}
			_audioSource = null;
		}
	}
}
