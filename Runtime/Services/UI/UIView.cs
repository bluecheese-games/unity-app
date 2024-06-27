//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using BlueCheese.Core.ServiceLocator;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlueCheese.App.Services
{
    [RequireComponent(typeof(BackHandler))]
    public class UIView : MonoBehaviour
    {
        [SerializeField] private Button _defaultButton;

        [Injectable] private IUIService _ui;

        private BackHandler _backHandler;

        public bool HasFocus { get; private set; }

        private void Awake()
        {
            ServiceContainer.Default.Inject(this);
            _backHandler = GetComponent<BackHandler>();

            if (_defaultButton == null)
            {
                _defaultButton = GetComponentInChildren<Button>();
            }
        }

        private void OnEnable()
        {
            _ui.RegisterView(this);
        }

        private void OnDisable()
        {
            _ui.UnregisterView(this);
        }

        public void CreateUIView(string viewName)
        {
            _ui.CreateView(viewName);
        }

        public virtual void Show()
        {
            gameObject.SetActive(true);
        }

        public virtual void Hide()
        {
            gameObject.SetActive(false);
        }

        public void Focus(bool focus)
        {
            HasFocus = focus;

            if (focus)
            {
                GameObject target = gameObject;
                if (_defaultButton != null)
                {
                    target = _defaultButton.gameObject;
                }

                if (target != null && EventSystem.current != null)
                {
                    EventSystem.current.SetSelectedGameObject(target);
                }
            }
        }

        public async Task HandleBackButton() => await _backHandler.HandleBackButton();

        public virtual void Destroy()
        {
            Destroy(gameObject);
            _ui = null;
        }

        private void OnDestroy()
        {
            _ui = null;
        }
    }
}