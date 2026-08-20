//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using BlueCheese.Core.DI;
using Cysharp.Threading.Tasks;

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

		/// <summary>
		/// Shows the view and awaits its show transition (see <see cref="ToggleableView.ToggleAsync"/>).
		/// </summary>
		public virtual UniTask ShowAsync() => ToggleableView.ToggleAsync(true);

		/// <summary>
		/// Hides the view and awaits its hide transition (see <see cref="ToggleableView.ToggleAsync"/>).
		/// </summary>
		public virtual UniTask HideAsync() => ToggleableView.ToggleAsync(false);

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