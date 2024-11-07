//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlueCheese.App
{
    [CreateAssetMenu(fileName = "AudioBank", menuName = "Audio/Bank")]
    public class AudioBank : ScriptableObject
    {
        public List<AudioItem> Items;

        private void OnValidate()
        {
            for (int i = 0; i < Items.Count; i++)
            {
				AudioItem item = Items[i];
                if (string.IsNullOrEmpty(item.Name) && item.Clip != null)
                {
                    item.Name = item.Clip.name;
                }
            }
        }
	}

	[Serializable]
	public class AudioItem
	{
		public SoundFX Name;
		public AudioClip Clip;
        [Range(0f, 1f)]
        public float Volume = 1f;

		public bool IsValid => !string.IsNullOrEmpty(Name) && Clip != null;
	}
}
