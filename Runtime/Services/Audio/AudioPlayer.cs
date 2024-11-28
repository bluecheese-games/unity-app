//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using BlueCheese.Core.Utils;
using UnityEngine;

namespace BlueCheese.App
{
	public class AudioPlayer : MonoBehaviour
	{
		[SerializeField] private SoundFX _soundFX;
		[SerializeField] private PlayTimeEvent _playTime = PlayTimeEvent.None;

		private void Awake()
		{
			PlayTimeTrigger.Setup(gameObject, _playTime, 0f, Play);
		}

		private void Play()
		{
			_soundFX.Play(transform.position);
		}
	}
}