//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using BlueCheese.Core.ServiceLocator;
using Core.Signals;
using NaughtyAttributes;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlueCheese.App.Services
{
    public class UIView : MonoBehaviour
    {
        [SerializeField] protected BackBehaviour _backBehaviour;
        [ShowIf(nameof(_backBehaviour), BackBehaviour.InvokeEvent)]
        [SerializeField] protected UnityEvent _onBack;
        [SerializeField] private Button _focusButton;

        [Injectable] private IUIService _uiService;
        [Injectable] private ILogger<UIView> _logger;
        [Injectable] private IApp _app;

        private void Awake()
        {
            ServiceContainer.Default.Inject(this);

            if (_focusButton == null)
            {
                _focusButton = GetComponentInChildren<Button>();
            }
        }

        private void OnEnable()
        {
            _uiService.RegisterView(this);
        }

        private void OnDisable()
        {
            _uiService.UnregisterView(this);
        }

        public async void HandleBackButton()
        {
            switch (_backBehaviour)
            {
                case BackBehaviour.HideView:
                    Hide();
                    break;
                case BackBehaviour.DestroyView:
                    Destroy();
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

        public void CreateUIView(string viewName)
        {
            _uiService.CreateView(viewName);
        }

        public virtual void Show()
        {
            gameObject.SetActive(true);
        }

        public virtual void Hide()
        {
            gameObject.SetActive(false);
        }

        public void Focus()
        {
            GameObject target = gameObject;
            if (_focusButton != null)
            {
                target = _focusButton.gameObject;
            }

            if (target != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(target);
            }
        }

        public virtual void Destroy()
        {
            Destroy(gameObject);
            _uiService = null;
        }

        private void OnDestroy()
        {
            _uiService = null;
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