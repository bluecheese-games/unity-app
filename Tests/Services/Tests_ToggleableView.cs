//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using BlueCheese.App;
using BlueCheese.Core.DI;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace BlueCheese.Tests.Services
{
	[TestFixture]
	public class Tests_ToggleableView
	{
		private GameObject _go;

		[SetUp]
		public void SetUp()
		{
			// See Tests_UIViewBehaviour.SetUp: every UIViewBehaviour subclass (including ToggleableView and
			// its subclasses) cascades into auto-adding a UIView via an inherited RequireComponent, whose
			// Awake() needs an initialized ServiceLocator to inject IUIService without throwing.
			var container = new ServiceContainer();
			container.Register<IUIService>(new NullUIService());
			ServiceLocator.Initialize(container);
		}

		[TearDown]
		public void TearDown()
		{
			ServiceLocator.Dispose();
			NavigableView.ResetForTests();
			if (_go != null) Object.DestroyImmediate(_go);
		}

		[Test]
		public void Toggle_Show_WithNoTransitionOverride_AppliesStateSynchronously()
		{
			// Arrange: fire-and-forget Toggle() must behave like the old fully-synchronous implementation
			// when PlayShowTransitionAsync/PlayHideTransitionAsync are not overridden.
			_go = new GameObject("View");
			_go.SetActive(false);
			var view = _go.AddComponent<ToggleableView>();
			_go.SetActive(true);

			// Act
			view.Toggle(true);

			// Assert
			Assert.AreEqual(ToggleableState.On, view.State);
			Assert.IsTrue(view.NavigableView.HasFocus);
		}

		[Test]
		public async System.Threading.Tasks.Task ToggleAsync_Show_AwaitsShowTransitionBeforeGrantingNavigationFocus()
		{
			// Arrange: use a CanvasGroup-based view starting fully transparent ("hidden"), with its
			// NavigableView already present. This is the realistic case where the deferred-focus ordering
			// actually matters: toggling a CanvasGroup's alpha never fires Unity's OnEnable, so the only
			// registration path left is ToggleAsync's own explicit call after the transition completes
			// (unlike the plain SetActive-based ToggleableView, where activating the GameObject to let the
			// transition render is indistinguishable from "shown" and triggers NavigableView.OnEnable's own
			// eager registration regardless of ToggleAsync's ordering).
			_go = new GameObject("View", typeof(CanvasGroup));
			var view = _go.AddComponent<RecordingToggleableView>();
			_go.GetComponent<CanvasGroup>().alpha = 0f;
			_go.AddComponent<NavigableView>(); // added while State reads Off, so OnEnable's eager check no-ops

			// Act
			await view.ToggleAsync(true);

			// Assert: the transition hook ran while the view had not yet been granted focus, proving the
			// ordering documented on ToggleableView.ToggleAsync (activate -> transition -> register focus).
			Assert.IsFalse(view.HadFocusDuringShowTransition);
			Assert.IsTrue(view.NavigableView.HasFocus);
			Assert.AreEqual(ToggleableState.On, view.State);
		}

		[Test]
		public async System.Threading.Tasks.Task ToggleAsync_Hide_ReleasesNavigationFocusBeforeHideTransition()
		{
			// Arrange (see previous test for why CanvasGroup + a pre-existing NavigableView is required)
			_go = new GameObject("View", typeof(CanvasGroup));
			var view = _go.AddComponent<RecordingToggleableView>();
			_go.GetComponent<CanvasGroup>().alpha = 0f;
			_go.AddComponent<NavigableView>();
			await view.ToggleAsync(true);

			// Act
			await view.ToggleAsync(false);

			// Assert: focus was already released by the time the hide transition hook ran, proving the
			// ordering documented on ToggleableView.ToggleAsync (unregister -> transition -> deactivate).
			Assert.IsFalse(view.HadFocusDuringHideTransition);
			Assert.AreEqual(ToggleableState.Off, view.State);
		}

		[Test]
		public void Toggle_Hide_OnPoolSpawnedView_ReturnsItToThePool()
		{
			// Regression test: hiding a view never returned it to the pool on its own -- only an explicit
			// UIService.DespawnView() call did, which nothing in the UIView/Popup API ever called -- so
			// spawning the same view again after hiding it always instantiated a brand new instance instead
			// of reusing the hidden one.
			var poolService = new GameObjectPoolService(new FakeGameObjectService(), new FakeLogger<GameObjectPoolService>());
			var prefab = new GameObject("PooledView");
			prefab.AddComponent<ToggleableView>();
			var pool = poolService.GetOrCreatePool(prefab);

			var spawned = pool.Spawn<ToggleableView>();
			_go = spawned.gameObject;
			Assert.AreEqual(1, pool.CountInUse);
			Assert.AreEqual(0, pool.CountAvailable);

			// Act
			spawned.Toggle(false);

			// Assert
			Assert.AreEqual(0, pool.CountInUse);
			Assert.AreEqual(1, pool.CountAvailable);

			// Cleanup
			Object.DestroyImmediate(prefab);
		}

		[Test]
		public void Toggle_Hide_OnViewNotSpawnedFromAPool_DoesNotThrow()
		{
			// A view placed directly in a scene (never pool-spawned) has no PoolItem; hiding it must just
			// toggle visibility as before, without attempting (and failing) to return it to a pool.
			_go = new GameObject("SceneView");
			var view = _go.AddComponent<ToggleableView>();

			// Act & Assert
			Assert.DoesNotThrow(() => view.Toggle(false));
			Assert.AreEqual(ToggleableState.Off, view.State);
		}

		// Records the NavigableView.HasFocus state at the moment each transition hook runs, without any
		// real per-frame delay, so the test stays deterministic and synchronous.
		private class RecordingToggleableView : CanvasGroupToggleableView
		{
			public bool HadFocusDuringShowTransition;
			public bool HadFocusDuringHideTransition;

			protected override UniTask PlayShowTransitionAsync()
			{
				HadFocusDuringShowTransition = NavigableView.HasFocus;
				return UniTask.CompletedTask;
			}

			protected override UniTask PlayHideTransitionAsync()
			{
				HadFocusDuringHideTransition = NavigableView.HasFocus;
				return UniTask.CompletedTask;
			}
		}
	}
}
