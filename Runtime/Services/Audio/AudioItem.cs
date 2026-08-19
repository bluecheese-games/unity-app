//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;
using UnityEngine;

namespace BlueCheese.App
{
	[Serializable]
	public class AudioItem
	{
		public string Name;
		public AudioClip Clip;
        [Range(0f, 1f)]
        public float Volume = 1f;

		public bool IsValid => !string.IsNullOrEmpty(Name) && Clip != null;
	}
}
