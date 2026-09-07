using Cysharp.Threading.Tasks;
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

		/// <summary>
		/// Fire-and-forget convenience wrapper over <see cref="ToggleAsync"/>, for callers that don't need
		/// to await the transition (UnityEvent callbacks, existing synchronous call sites). As long as
		/// <see cref="PlayShowTransitionAsync"/>/<see cref="PlayHideTransitionAsync"/> are not overridden
		/// (both default to an already-completed task), this applies the state change synchronously, just
		/// like the previous, purely-synchronous implementation of Toggle.
		/// </summary>
		public void Toggle(bool visible) => ToggleAsync(visible).Forget();

		/// <summary>
		/// Toggles the view's visibility, awaiting a show/hide transition hook around the state change.
		/// Override <see cref="PlayShowTransitionAsync"/>/<see cref="PlayHideTransitionAsync"/> in a
		/// subclass to play a fade/slide/scale animation (or anything else) during the transition.
		/// </summary>
		public async UniTask ToggleAsync(bool visible)
		{
			if (visible)
			{
				// Activate first so the transition (and whatever it animates) can actually render, then
				// only grant navigation/back-handling focus once the entrance transition has finished.
				ToggleOn();
				await PlayShowTransitionAsync();
				NavigableView.RegisterView(NavigableView);
			}
			else
			{
				// Drop focus/back-handling immediately, before the exit transition even starts, so a view
				// mid-exit can no longer react to input (e.g. a second "Back" press). Only once the exit
				// transition has finished do we apply the final hidden state.
				NavigableView.UnregisterView(NavigableView);
				await PlayHideTransitionAsync();
				ToggleOff();
				ReturnToPoolIfPooled();
			}
		}

		/// <summary>
		/// If this view was spawned from a pool (i.e. it has a <see cref="GameObjectPool.PoolItem"/>, added
		/// automatically by <see cref="IGameObjectPool.Spawn{T}"/>), returns it to that pool once fully
		/// hidden, so the next <see cref="IUIService.SpawnView(string)"/> / <see cref="UIViewRef.Spawn"/>
		/// call for the same view reuses this instance instead of instantiating a new one.
		///
		/// Views placed directly in a scene (never pool-spawned) have no PoolItem and are left untouched --
		/// Hide()/ToggleAsync(false) on those just toggles visibility as before.
		/// </summary>
		protected virtual void ReturnToPoolIfPooled()
		{
			if (TryGetComponent<GameObjectPool.PoolItem>(out var poolItem))
			{
				poolItem.Despawn();
			}
		}

		/// <summary>
		/// Applies the final "shown" state instantly (activates the GameObject by default). Runs before
		/// <see cref="PlayShowTransitionAsync"/>, so overrides of the latter can assume the view is already
		/// visible (e.g. to fade a CanvasGroup in).
		/// </summary>
		protected virtual void ToggleOn() => gameObject.SetActive(true);

		/// <summary>
		/// Applies the final "hidden" state instantly (deactivates the GameObject by default). Runs after
		/// <see cref="PlayHideTransitionAsync"/> has completed, so the exit transition remains visible while
		/// it plays out.
		/// </summary>
		protected virtual void ToggleOff() => gameObject.SetActive(false);

		/// <summary>
		/// Extension point for a custom show transition (fade/slide/scale/sound cue...). Called after
		/// <see cref="ToggleOn"/> and before the view is granted navigation focus. Default: no-op
		/// (a completed task), which keeps <see cref="Toggle"/>/<see cref="ToggleAsync"/> synchronous.
		/// </summary>
		protected virtual UniTask PlayShowTransitionAsync() => UniTask.CompletedTask;

		/// <summary>
		/// Extension point for a custom hide transition. Called after the view has already released
		/// navigation focus and before <see cref="ToggleOff"/> actually hides it, so the exit transition
		/// remains visible while it plays out. Default: no-op (a completed task).
		/// </summary>
		protected virtual UniTask PlayHideTransitionAsync() => UniTask.CompletedTask;
	}
}
