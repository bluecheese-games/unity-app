//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using UnityEngine;

namespace BlueCheese.App
{
	[RequireComponent(typeof(UIView))]
	public abstract class UIViewBehaviour : MonoBehaviour
	{
		private UIView _view;
		private NavigableView _navigableView;
		private ToggleableView _toggleableView;

		public UIView View
		{
			get
			{
				if (_view == null)
				{
					_view = GetComponent<UIView>();
				}
				return _view;
			}
		}

		public NavigableView NavigableView
		{
			get
			{
				if (_navigableView == null)
				{
					if (TryGetComponent(out _navigableView))
					{
						_navigableView = GetComponent<NavigableView>();
					}
					else
					{
						_navigableView = gameObject.AddComponent<NavigableView>();
					}
				}
				return _navigableView;
			}
		}

		public ToggleableView ToggleableView
		{
			get
			{
				if (_toggleableView == null)
				{
					if (TryGetComponent(out _toggleableView))
					{
						_toggleableView = GetComponent<ToggleableView>();
					}
					else
					{
						_toggleableView = CreateAppropriateToggleableView();
					}
				}
				return _toggleableView;
			}
		}

		private ToggleableView CreateAppropriateToggleableView()
		{
			if (TryGetComponent<Canvas>(out _))
			{
				return gameObject.AddComponent<CanvasToggleableView>();
			}
			else if (TryGetComponent<CanvasGroup>(out _))
			{
				return gameObject.AddComponent<CanvasGroupToggleableView>();
			}
			else
			{
				return gameObject.AddComponent<ToggleableView>();
			}
		}
	}
}