using UnityEngine;

namespace BlueCheese.App
{
	/// <summary>
	/// Base class for all UI-related components living on a <see cref="UIView"/> GameObject
	/// (<see cref="ToggleableView"/>, <see cref="NavigableView"/>, <see cref="BackHandler"/>, ...).
	///
	/// <para>
	/// <b>Implicit component wiring:</b> the <see cref="NavigableView"/> and <see cref="ToggleableView"/>
	/// accessors below lazily auto-add their companion component to the GameObject the first time they are
	/// accessed, instead of requiring every view prefab to have them wired up by hand. This only happens at
	/// runtime (typically the first time a view is shown/hidden or asked to register for input), so a
	/// prefab inspected in Edit Mode will NOT show these components until the game has actually run once
	/// with that prefab. If you need the component to be visible/tweakable in the prefab itself (e.g. to
	/// assign a custom <see cref="ToggleableView"/> subclass or a specific default button on
	/// <see cref="NavigableView"/>), add it explicitly on the prefab instead of relying on this fallback.
	/// </para>
	/// </summary>
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

		/// <summary>
		/// Returns the <see cref="BlueCheese.App.NavigableView"/> on this GameObject, adding a default one
		/// (with no explicit default button) if none exists yet. See the class-level remarks about implicit
		/// component wiring.
		/// </summary>
		public NavigableView NavigableView
		{
			get
			{
				if (_navigableView == null && !TryGetComponent(out _navigableView))
				{
					_navigableView = gameObject.AddComponent<NavigableView>();
				}
				return _navigableView;
			}
		}

		/// <summary>
		/// Returns the <see cref="BlueCheese.App.ToggleableView"/> on this GameObject, adding the most
		/// appropriate implementation if none exists yet (see <see cref="CreateAppropriateToggleableView"/>).
		/// See the class-level remarks about implicit component wiring.
		/// </summary>
		public ToggleableView ToggleableView
		{
			get
			{
				if (_toggleableView == null && !TryGetComponent(out _toggleableView))
				{
					_toggleableView = CreateAppropriateToggleableView();
				}
				return _toggleableView;
			}
		}

		/// <summary>
		/// Picks and adds the <see cref="BlueCheese.App.ToggleableView"/> subclass that matches what's
		/// already on the GameObject: <see cref="CanvasToggleableView"/> if a <see cref="Canvas"/> is
		/// present, <see cref="CanvasGroupToggleableView"/> if a <see cref="CanvasGroup"/> is present,
		/// otherwise the base <see cref="BlueCheese.App.ToggleableView"/> (plain SetActive toggling).
		/// </summary>
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
