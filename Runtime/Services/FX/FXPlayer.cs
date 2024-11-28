//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using BlueCheese.Core.Utils;
using UnityEngine;

namespace BlueCheese.App
{
	public class FXPlayer : MonoBehaviour
	{
		[SerializeField] private FX _fx;
		[SerializeField] private PlayTimeEvent _playTime = PlayTimeEvent.None;
		[SerializeField] private float _delay = 0f;
		[SerializeField] private float _scale = 1f;
		[SerializeField] private Transform _target;
		[SerializeField] private Vector3 _offset;

		private void Awake()
		{
			PlayTimeTrigger.Setup(gameObject, _playTime, _delay, Play);
		}

		public void Play()
		{
			if (_target == null)
			{
				_fx.Play(transform.position + _offset, _scale);
			}
			else
			{
				_fx.Play(_target, _offset, _scale);
			}
		}
	}
}
