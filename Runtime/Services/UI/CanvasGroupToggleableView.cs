//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using UnityEngine;

namespace BlueCheese.App
{
	/// <summary>
	/// Toggles visibility via <see cref="CanvasGroup"/> (alpha/interactable/blocksRaycasts) instead of
	/// SetActive, so the GameObject stays active while hidden. This is the natural place to plug in a fade
	/// transition: override <c>PlayShowTransitionAsync</c>/<c>PlayHideTransitionAsync</c> from
	/// <see cref="ToggleableView"/> to animate <see cref="CanvasGroup.alpha"/> over time instead of the
	/// instant 0/1 jump applied by <see cref="ToggleOn"/>/<see cref="ToggleOff"/> below.
	/// </summary>
	public class CanvasGroupToggleableView : ToggleableView
	{
		[SerializeField] private CanvasGroup _canvasGroup;

		public override ToggleableState State
			=> _canvasGroup != null && _canvasGroup.alpha > 0f ? ToggleableState.On : ToggleableState.Off;

		private void OnValidate()
		{
			if (_canvasGroup == null)
			{
				_canvasGroup = GetComponent<CanvasGroup>();
			}
		}

		private void Awake()
		{
			if (_canvasGroup == null)
			{
				_canvasGroup = GetComponent<CanvasGroup>();
			}
			if (_canvasGroup == null)
			{
				Debug.LogError("CanvasGroupView requires a CanvasGroup component.");
			}
		}

		protected override void ToggleOn()
		{
			if (_canvasGroup != null)
			{
				_canvasGroup.alpha = 1f;
				_canvasGroup.interactable = true;
				_canvasGroup.blocksRaycasts = true;
			}
		}

		protected override void ToggleOff()
		{
			if (_canvasGroup != null)
			{
				_canvasGroup.alpha = 0f;
				_canvasGroup.interactable = false;
				_canvasGroup.blocksRaycasts = false;
			}
		}
	}
}