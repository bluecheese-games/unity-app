//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System.Collections.Generic;
using UnityEngine;

namespace BlueCheese.App
{
    [CreateAssetMenu(fileName = "AudioBank", menuName = "BlueCheese/Audio/Bank")]
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
}
