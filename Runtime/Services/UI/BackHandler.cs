//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using BlueCheese.Core.DI;
using BlueCheese.Core.Signals;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace BlueCheese.App
{
	[RequireComponent(typeof(NavigableView))]
	public class BackHandler : UIViewBehaviour
	{
		[SerializeField] protected BackBehaviour _backBehaviour;
		[SerializeField] protected UnityEvent _onBack;

		[Injectable] private IApp _appService;
		[Injectable] private IInputService _inputService;

		public BackBehaviour Behaviour => _backBehaviour;

		private void Awake()
		{
			ServiceInjector.Inject(this);
		}

		private void Update()
		{
			if (!NavigableView.HasFocus || ToggleableView.State != ToggleableState.On)
			{
				return;
			}

			if (_inputService.GetButtonDown("Cancel") || _inputService.GetKeyDown(KeyCode.Escape))
			{
				_ = HandleBackButton();
			}
		}

		public async Task HandleBackButton()
		{
			await Task.Yield(); // Ensure this runs after the current frame, to avoid potential issues with UI events

			_onBack?.Invoke();
			switch (_backBehaviour)
			{
				case BackBehaviour.HideView:
					ToggleableView.Toggle(false);
					break;
				case BackBehaviour.DestroyView:
					Destroy(gameObject);
					break;
				case BackBehaviour.ExitApp:
					var signal = new ExitAppRequestSignal();
					await SignalAPI.PublishAsync(signal);
					if (!signal.IsCancelled)
					{
						_appService.Quit();
					}
					break;
			}
		}

		[Serializable]
		public enum BackBehaviour
		{
			None,
			HideView,
			DestroyView,
			ExitApp,
		}
	}
}