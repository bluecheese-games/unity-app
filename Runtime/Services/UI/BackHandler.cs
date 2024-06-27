//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using BlueCheese.Core.ServiceLocator;
using Core.Signals;
using NaughtyAttributes;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace BlueCheese.App.Services
{
    [RequireComponent(typeof(UIView))]
    public class BackHandler : MonoBehaviour
    {
        [SerializeField] protected BackBehaviour _backBehaviour;
        [ShowIf(nameof(_backBehaviour), BackBehaviour.InvokeEvent)]
        [SerializeField] protected UnityEvent _onBack;

        [Injectable] private ILogger<BackHandler> _logger;
        [Injectable] private IApp _app;
        [Injectable] private IInputService _input;

        private UIView _uiView;

        private void Awake()
        {
            ServiceContainer.Default.Inject(this);
            _uiView = GetComponent<UIView>();
        }

        private async void Update()
        {
            if (!_uiView.HasFocus)
            {
                return;
            }

            if (_input.GetButtonDown("Cancel") || _input.GetKeyDown(KeyCode.Escape))
            {
                await HandleBackButton();
            }
        }

        public async Task HandleBackButton()
        {
            switch (_backBehaviour)
            {
                case BackBehaviour.HideView:
                    _uiView.Hide();
                    break;
                case BackBehaviour.DestroyView:
                    _uiView.Destroy();
                    break;
                case BackBehaviour.InvokeEvent:
                    _onBack?.Invoke();
                    break;
                case BackBehaviour.ExitApp:
                    _logger.Log("Exiting app...");
                    var signal = new ExitAppRequestSignal();
                    await SignalAPI.PublishAsync(signal);
                    if (signal.IsCancelled)
                    {
                        _logger.Log("Exit app cancelled");
                    }
                    else
                    {
                        _logger.Log("Exit app!");
                        _app.Quit();
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
            InvokeEvent,
            ExitApp,
        }
    }
}