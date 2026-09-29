using BlueCheese.Core.Utils;
using UnityEngine;

namespace BlueCheese.App
{
	[CreateAssetMenu(fileName = "AudioBank", menuName = "BlueCheese/Audio/Audio Bank")]
	public class AudioBank : Collection<AudioItem>
	{
#if UNITY_EDITOR
		protected override void OnValidate()
		{
			base.OnValidate();

			if (_items == null) return;

			for (int i = 0; i < _items.Count; i++)
			{
				AudioItem item = _items[i];
				if (string.IsNullOrEmpty(item.Name) && item.Clip != null)
				{
					item.Name = item.Clip.name;
				}
			}
		}
#endif
	}
}
