using BlueCheese.Core.DI;
using BlueCheese.Core.Utils;
using System;
using UnityEngine;

namespace BlueCheese.App
{
	/// <summary>
	/// Designer-facing reference to a <see cref="UIViewDef"/>, picked by asset via a searchable dropdown in
	/// the inspector (see the UIViewRefPropertyDrawer editor script) instead of typed in by name. Mirrors
	/// the <see cref="FX"/> (and, more loosely, <see cref="SoundFX"/>) reference pattern already used
	/// elsewhere in this module: declare a field like
	/// <code>[SerializeField] private UIViewRef _okPopup;</code>
	/// then spawn it with <c>_okPopup.Spawn&lt;Popup&gt;()</c>.
	/// </summary>
	[Serializable]
	public struct UIViewRef
	{
		[SerializeField] private AssetRef<UIViewDef> _viewDef;

		// Test seam: BlueCheese.App.Tests is granted InternalsVisibleTo (see NavigableView.cs). Designers
		// always assign this via the inspector picker; this constructor exists purely so tests can build a
		// UIViewRef pointing at a known GUID without going through the inspector.
		internal UIViewRef(string guid)
		{
			_viewDef = new AssetRef<UIViewDef> { Guid = guid };
		}

		/// <summary>
		/// Resolves the referenced <see cref="UIViewDef"/>, or null if none is assigned.
		/// </summary>
		public readonly UIViewDef Def => _viewDef.Asset;

		/// <summary>
		/// The registered name of the referenced view (as used by <see cref="IUIService.SpawnView(string)"/>),
		/// or null if none is assigned.
		/// </summary>
		public readonly string Name => Def?.Name;

		public readonly bool IsValid => Def != null && Def.IsValid;

		/// <summary>
		/// Spawns the referenced view via <see cref="IUIService"/> (resolved from the <see cref="ServiceLocator"/>).
		/// Resolves the <see cref="UIViewDef"/> directly (by asset reference), so this does not go through the
		/// name-based lookup that <see cref="IUIService.SpawnView(string)"/> uses.
		/// </summary>
		public readonly UIView Spawn()
		{
			var def = Def;
			if (def == null)
			{
				throw new InvalidOperationException($"Unable to spawn UIView: this {nameof(UIViewRef)} has no UIViewDef assigned.");
			}
			return ServiceLocator.Resolve<IUIService>().SpawnView(def);
		}

		/// <summary>
		/// Spawns the referenced view and returns the given component on it, e.g. <c>_okPopup.Spawn&lt;Popup&gt;()</c>.
		/// </summary>
		public readonly T Spawn<T>() where T : Component => Spawn().GetComponent<T>();
	}
}
