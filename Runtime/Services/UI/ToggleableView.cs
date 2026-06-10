//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;

namespace BlueCheese.App
{
	[Serializable]
	public enum ToggleableState
	{
		On,
		Off,
	}

	public class ToggleableView : UIViewBehaviour
	{
		public virtual ToggleableState State
			=> gameObject.activeSelf ? ToggleableState.On : ToggleableState.Off;

		public void Toggle(bool visible)
		{
			if (visible)
			{
				ToggleOn();
				NavigableView.RegisterView(NavigableView);
			}
			else
			{
				ToggleOff();
				NavigableView.UnregisterView(NavigableView);
			}
		}

		protected virtual void ToggleOn() => gameObject.SetActive(true);

		protected virtual void ToggleOff() => gameObject.SetActive(false);
	}
}