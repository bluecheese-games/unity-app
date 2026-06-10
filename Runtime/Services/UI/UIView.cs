//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using BlueCheese.Core.DI;

namespace BlueCheese.App
{
	public class UIView : UIViewBehaviour
	{
		[Injectable] private IUIService _ui;

		private void Awake()
		{
			ServiceInjector.Inject(this);
		}

		public void CreateUIView(string viewName)
		{
			_ui.SpawnView(viewName);
		}

		public virtual void Show() => ToggleableView.Toggle(true);

		public virtual void Hide() => ToggleableView.Toggle(false);

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