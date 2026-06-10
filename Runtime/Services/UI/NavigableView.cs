//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlueCheese.App
{

	public class NavigableView : UIViewBehaviour
	{
		[SerializeField] private Button _defaultButton;

		// Handle reload domain to clear static fields
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		static void ReloadDomain()
		{
			_focusedView = null;
			_viewList.Clear();
		}

		private static NavigableView _focusedView;
		private static readonly List<NavigableView> _viewList = new();

		public bool HasFocus { get; private set; }

		private void Awake()
		{
			if (_defaultButton == null)
			{
				_defaultButton = GetComponentInChildren<Button>();
			}
		}

		private void OnEnable()
		{
			if (ToggleableView.State == ToggleableState.On)
			{
				RegisterView(this);
			}
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

		static public void RegisterView(NavigableView view)
		{
			if (view == null || _viewList.Contains(view))
			{
				return;
			}

			_viewList.Add(view);
			UpdateCurrentView();
		}

		static public void UnregisterView(NavigableView view)
		{
			if (view == null || !_viewList.Contains(view))
			{
				return;
			}

			_viewList.Remove(view);
			UpdateCurrentView();
		}

		static private void UpdateCurrentView()
		{
			if (_focusedView != null)
			{
				_focusedView.Focus(false);
			}

			if (_viewList.Count == 0)
			{
				_focusedView = null;
			}
			else
			{
				_focusedView = _viewList.Last();

				// Focus current view
				_focusedView.Focus(true);
			}
		}
	}
}