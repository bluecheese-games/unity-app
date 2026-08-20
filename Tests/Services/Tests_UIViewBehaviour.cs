//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using BlueCheese.App;
using BlueCheese.Core.DI;
using NUnit.Framework;
using UnityEngine;

namespace BlueCheese.Tests.Services
{
	// Covers UIViewBehaviour's implicit component wiring (NavigableView/ToggleableView lazy getters),
	// including a regression check for a redundant double GetComponent call that used to sit in both
	// getters (harmless output-wise, but wasteful and confusing to read).
	[TestFixture]
	public class Tests_UIViewBehaviour
	{
		private GameObject _go;

		[SetUp]
		public void SetUp()
		{
			// Every UIViewBehaviour subclass inherits [RequireComponent(typeof(UIView))] from the base
			// class, so adding e.g. NavigableView to a bare GameObject cascades into also auto-adding a
			// UIView, whose Awake() injects [Injectable] fields via ServiceInjector -- which requires an
			// initialized ServiceLocator, or it throws and (having thrown mid-cascade) can prevent the
			// GameObject's other components from completing their own Awake/OnEnable.
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
		public void NavigableView_Getter_WhenComponentAlreadyExists_ReturnsSameInstance_WithoutAddingDuplicate()
		{
			// Arrange: add ToggleableView first so NavigableView's own OnEnable finds it already present
			// and doesn't trigger the CreateAppropriateToggleableView fallback as a side effect.
			_go = new GameObject("View");
			var behaviour = _go.AddComponent<ToggleableView>();
			var existingNav = _go.AddComponent<NavigableView>();

			// Act
			var resolved = behaviour.NavigableView;

			// Assert
			Assert.AreSame(existingNav, resolved);
			Assert.AreEqual(1, _go.GetComponents<NavigableView>().Length);
			Assert.AreEqual(1, _go.GetComponents<ToggleableView>().Length);
		}

		[Test]
		public void ToggleableView_Getter_WhenComponentAlreadyExists_ReturnsSameInstance_WithoutAddingDuplicate()
		{
			// Arrange
			_go = new GameObject("View", typeof(CanvasGroup));
			var existingToggleable = _go.AddComponent<CanvasGroupToggleableView>();
			var behaviour = _go.AddComponent<NavigableView>();

			// Act
			var resolved = behaviour.ToggleableView;

			// Assert
			Assert.AreSame(existingToggleable, resolved);
			Assert.AreEqual(1, _go.GetComponents<ToggleableView>().Length);
		}

		[Test]
		public void ToggleableView_Getter_WithNoVisibilityComponentPresent_AddsBaseToggleableView()
		{
			// Arrange
			_go = new GameObject("PlainView");
			var behaviour = _go.AddComponent<NavigableView>();

			// Act
			var toggleable = behaviour.ToggleableView;

			// Assert
			Assert.AreEqual(typeof(ToggleableView), toggleable.GetType());
			Assert.AreEqual(1, _go.GetComponents<ToggleableView>().Length);
		}

		[Test]
		public void ToggleableView_Getter_WithCanvasPresent_AddsCanvasToggleableView()
		{
			// Arrange
			_go = new GameObject("CanvasView", typeof(Canvas));
			var behaviour = _go.AddComponent<NavigableView>();

			// Act
			var toggleable = behaviour.ToggleableView;

			// Assert
			Assert.IsInstanceOf<CanvasToggleableView>(toggleable);
			Assert.AreEqual(1, _go.GetComponents<ToggleableView>().Length);
		}

		[Test]
		public void ToggleableView_Getter_WithCanvasGroupPresent_AddsCanvasGroupToggleableView()
		{
			// Arrange
			_go = new GameObject("CanvasGroupView", typeof(CanvasGroup));
			var behaviour = _go.AddComponent<NavigableView>();

			// Act
			var toggleable = behaviour.ToggleableView;

			// Assert
			Assert.IsInstanceOf<CanvasGroupToggleableView>(toggleable);
			Assert.AreEqual(1, _go.GetComponents<ToggleableView>().Length);
		}
	}
}
