//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using BlueCheese.Core.DI;
using System;
using UnityEngine;

namespace BlueCheese.App
{
	[Serializable]
	public struct SoundFX
	{
		public string Name;
		public bool HasOptions;
		public SoundOptions Options;
		public Vector3 Position;

        public readonly bool IsValid => !string.IsNullOrEmpty(Name);

		public void Play()
		{
			if (!HasOptions)
			{
				Options = SoundOptions.Default;
			}
			ServiceLocator.Resolve<IAudioService>().PlaySound(this);
		}

		public void Play(Vector3 position)
		{
			Position = position;
			Play();
		}

		public void Play(SoundOptions options)
		{
			Options = options;
			Play();
		}

		public void Play(Vector3 position, SoundOptions options)
		{
			Position = position;
			Options = options;
			Play();
		}

		public readonly void Stop()
		{
			ServiceLocator.Resolve<IAudioService>().StopSound(this);
		}

		public readonly void Stop(float fadeOutDuration)
		{
			ServiceLocator.Resolve<IAudioService>().StopSound(this, fadeOutDuration);
		}

		public static implicit operator SoundFX(string name) => new() { Name = name };
		public static implicit operator string(SoundFX soundFX) => soundFX.Name;
	}
}
