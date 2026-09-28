using BlueCheese.App;
using BlueCheese.Core.DI;
using NUnit.Framework;
using UnityEngine;

namespace BlueCheese.Tests.Services
{
	[TestFixture]
	public class Tests_NavigableView
	{
		private GameObject _goA;
		private GameObject _goB;

		[SetUp]
		public void SetUp()
		{
			// See Tests_ToggleableView.SetUp: NavigableView's inherited RequireComponent chain auto-adds a
			// UIView, whose Awake() needs an initialized ServiceLocator to inject IUIService without throwing.
			var container = new ServiceContainer();
			container.Register<IUIService>(new NullUIService());
			ServiceLocator.Initialize(container);
		}

		[TearDown]
		public void TearDown()
		{
			ServiceLocator.Dispose();
			NavigableView.ResetForTests();
			if (_goA != null) Object.DestroyImmediate(_goA);
			if (_goB != null) Object.DestroyImmediate(_goB);
		}

		[Test]
		public void DestroyingAView_WithoutGoingThroughToggleAsync_RestoresFocusToThePreviousView()
		{
			// Regression test: a scene-placed view (registered via NavigableView.OnEnable's documented
			// fallback for "views that get activated without going through Toggle" -- e.g. a screen loaded
			// additively as part of a scene, never pool-spawned/toggled) left a stale entry in the static
			// view stack forever if its GameObject was destroyed directly (e.g. an additive scene unload)
			// instead of being explicitly hidden via ToggleAsync(false) first -- there was no OnDisable to
			// mirror OnEnable's eager registration, so UnregisterView() was never called for this path.

			// Arrange: two scene-placed views, both auto-registered via the OnEnable fallback (bare
			// GameObject.activeSelf reads On, no Canvas/CanvasGroup so the base ToggleableView is used).
			_goA = new GameObject("ViewA");
			var viewA = _goA.AddComponent<NavigableView>();
			_goB = new GameObject("ViewB");
			var viewB = _goB.AddComponent<NavigableView>();

			Assert.IsFalse(viewA.HasFocus, "ViewB was registered after ViewA, so ViewA should have lost focus.");
			Assert.IsTrue(viewB.HasFocus);

			// Act: destroy the topmost view outright, the way an additive scene unload would, without ever
			// calling ToggleAsync(false) on it first.
			Object.DestroyImmediate(_goB);
			_goB = null;

			// Assert: focus falls back to the remaining view. Before the fix, ViewB stayed in the static
			// view stack as a stale/destroyed reference and ViewA never regained focus.
			Assert.IsTrue(viewA.HasFocus);
		}
	}
}
