//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using BlueCheese.Core.ServiceLocator;
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

		public void Play(Vector3 position = default)
		{
			Position = position;
			if (!HasOptions)
			{
				Options = SoundOptions.Default;
			}
			Services.Get<IAudioService>().PlaySound(this);
		}

		public static implicit operator SoundFX(string name) => new() { Name = name };
		public static implicit operator string(SoundFX soundFX) => soundFX.Name;
	}
}
