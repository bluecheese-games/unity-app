//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

namespace BlueCheese.App
{
	public class FXPlayer : MonoBehaviour, IDespawnable, IRecyclable
	{
		[SerializeField] private FX _fx;
		[SerializeField] private PlayTime _playTime = PlayTime.None;
		[SerializeField] private float _delay = 0f;
		[SerializeField] private Transform _target;
		[SerializeField] private Vector3 _offset;

		private bool _enabled = true;
		private bool _started = false;

		private async void Awake() => await HandlePlayTimeAsync(PlayTime.OnAwake);

		private async void Start()
		{
			_started = true;
			await HandlePlayTimeAsync(PlayTime.OnStart);
		}

		private async void OnEnable() => await HandlePlayTimeAsync(PlayTime.OnEnable);

		void IRecyclable.OnRecycle() => HandlePlayTime(PlayTime.OnRecycle, true);

		private async void OnDisable() => await HandlePlayTimeAsync(PlayTime.OnDisable, true);

		void IDespawnable.OnDespawn() => HandlePlayTime(PlayTime.OnDespawn, true);

		private void OnDestroy() => HandlePlayTime(PlayTime.OnDestroy, true);

		private async UniTask HandlePlayTimeAsync(PlayTime playTime, bool requireStarted = false)
		{
			if (!_enabled || (requireStarted && !_started) || !_playTime.HasFlag(playTime))
			{
				return;
			}
			await PlayAsync(_delay);
		}

		private void HandlePlayTime(PlayTime playTime, bool requireStarted = false)
		{
			if (!_enabled || (requireStarted && !_started) || !_playTime.HasFlag(playTime))
			{
				return;
			}
			Play(_delay);
		}

		public async UniTask PlayAsync(float delay = 0f)
		{
			if (delay > 0f)
			{
				await UniTask.Delay(TimeSpan.FromSeconds(delay));
			}

			Play();
		}

		public void Play(float delay)
		{
			if (delay > 0f)
			{
				UniTask.RunOnThreadPool(() => PlayAsync(delay));
			}
			else
			{
				Play();
			}
		}

		public void Play()
		{
			if (_target == null)
			{
				_fx.Play(transform.position + _offset);
			}
			else
			{
				_fx.Play(_target, _offset);
			}
		}

		private void OnApplicationQuit()
		{
			_enabled = false;
		}

		[Flags]
		public enum PlayTime
		{
			None = 0,
			OnAwake = 1 << 0,
			OnStart = 1 << 1,
			OnEnable = 1 << 2,
			OnDisable = 1 << 3,
			OnDestroy = 1 << 4,
			OnDespawn = 1 << 5,
			OnRecycle = 1 << 6,
		}
	}
}
