using BlueCheese.Core.DI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlueCheese.App
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

		/// <summary>
		/// Plays an <see cref="AudioClip"/> directly, bypassing the by-name <see cref="_items"/> lookup used
		/// by <see cref="PlaySound"/>. Used by inspectors (e.g. <see cref="AudioBankEditor"/>) that already
		/// hold a direct reference to the clip they want to preview -- which may not even be registered in
		/// any <see cref="AudioBank"/> yet, or may live in a bank outside Resources (so <see cref="_items"/>
		/// wouldn't know about it regardless).
		/// </summary>
		public void PlayClip(AudioClip clip, float volume = 1f)
		{
			StopAll();

			if (clip == null)
			{
				return;
			}

			GameObject gameObject = new("AudioPlayer_" + clip.name);
			_audioSource = gameObject.AddComponent<AudioSource>();
			gameObject.hideFlags = HideFlags.HideAndDontSave;

			_audioSource.clip = clip;
			_audioSource.volume = volume;
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
