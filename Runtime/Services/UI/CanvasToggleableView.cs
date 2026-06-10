//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using UnityEngine;

namespace BlueCheese.App
{
	public class CanvasToggleableView : ToggleableView
	{
		[SerializeField] private Canvas _canvas;

		public override ToggleableState State
			=> _canvas != null && _canvas.enabled ? ToggleableState.On : ToggleableState.Off;

		private void OnValidate()
		{
			if (_canvas == null)
			{
				_canvas = GetComponent<Canvas>();
			}
		}

		private void Awake()
		{
			if (_canvas == null)
			{
				_canvas = GetComponent<Canvas>();
			}
			if (_canvas == null)
			{
				Debug.LogError("CanvasView requires a Canvas component.");
			}
		}

		protected override void ToggleOn()
		{
			if (_canvas != null)
			{
				_canvas.enabled = true;
			}
		}

		protected override void ToggleOff()
		{
			if (_canvas != null)
			{
				_canvas.enabled = false;
			}
		}
}
}