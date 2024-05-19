//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlueCheese.App.Services
{
    [CreateAssetMenu(fileName = "AudioBank", menuName = "Audio/Bank")]
    public class AudioBank : ScriptableObject
    {
        public List<Item> Items;

        private void OnValidate()
        {
            for (int i = 0; i < Items.Count; i++)
            {
                Item item = Items[i];
                if (string.IsNullOrEmpty(item.Name) && item.Clip != null)
                {
                    item.Name = item.Clip.name;
                }
            }
        }

        [Serializable]
        public class Item
        {
            public string Name;
            public AudioClip Clip;
        }
    }
}
