//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[assembly: InternalsVisibleTo("BlueCheese.App.Tests")]

namespace BlueCheese.App
{
	/// <summary>
	/// Tracks the stack of currently-visible views (app-wide, via a single static list) and keeps input
	/// focus on the most recently shown one. Registration happens automatically whenever a view is toggled
	/// on/off through <see cref="ToggleableView"/> (see <see cref="ToggleableView.ToggleAsync"/>) or, as a
	/// fallback, from <see cref="OnEnable"/> for views that get activated without going through Toggle.
	/// </summary>
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

#if UNITY_EDITOR
		// Test seam: the focus stack above is static (app-wide) by design, so it otherwise leaks state
		// between unrelated unit tests running in the same Editor session. Mirrors AssetBank's
		// ResetForTests()/InternalsVisibleTo pattern (see BlueCheese.Core.Utils.AssetBank).
		internal static void ResetForTests()
		{
			_focusedView = null;
			_viewList.Clear();
		}
#endif

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