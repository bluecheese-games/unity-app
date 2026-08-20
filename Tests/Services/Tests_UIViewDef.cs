//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using BlueCheese.App;
using BlueCheese.Core.DI;
using NUnit.Framework;
using UnityEngine;

namespace BlueCheese.Tests.Services
{
	[TestFixture]
	public class Tests_UIViewDef
	{
		private UIViewDef _def;
		private GameObject _prefabObject;
		private ServiceContainer _container;

		[SetUp]
		public void SetUp()
		{
			// UIView.Awake() injects [Injectable] fields via ServiceInjector, which requires an
			// initialized ServiceLocator even in a bare unit test.
			_container = new ServiceContainer();
			_container.Register<IUIService>(new NullUIService());
			ServiceLocator.Initialize(_container);

			_prefabObject = new GameObject("Prefab");
			_def = ScriptableObject.CreateInstance<UIViewDef>();
		}

		[TearDown]
		public void TearDown()
		{
			ServiceLocator.Dispose();
			Object.DestroyImmediate(_prefabObject);
			Object.DestroyImmediate(_def);
		}

		[Test]
		public void IsValid_WithoutPrefab_ReturnsFalse()
		{
			// Assert
			Assert.IsFalse(_def.IsValid);
		}

		[Test]
		public void IsValid_WithPrefab_ReturnsTrue()
		{
			// Arrange
			_def.ViewPrefab = _prefabObject.AddComponent<UIView>();

			// Assert
			Assert.IsTrue(_def.IsValid);
		}

		[Test]
		public void ToPoolOptions_WithoutPrewarm_HasZeroFillAmountAndUsesConfiguredCapacity()
		{
			// Arrange
			_def.Prewarm = false;
			_def.PrewarmPoolSize = 5;
			_def.Capacity = 20;
			_def.Overflow = PoolOverflow.RecycleAny;
			_def.DontDestroyOnLoad = true;

			// Act
			var options = _def.ToPoolOptions();

			// Assert
			Assert.AreEqual(0, options.FillAmount);
			Assert.AreEqual(20, options.Capacity);
			Assert.AreEqual(PoolOverflow.RecycleAny, options.Overflow);
			Assert.IsTrue(options.DontDestroyOnLoad);
			Assert.IsTrue(options.UseContainer);
		}

		[Test]
		public void ToPoolOptions_WithPrewarm_FillsUpToPrewarmSize()
		{
			// Arrange
			_def.Prewarm = true;
			_def.PrewarmPoolSize = 5;

			// Act
			var options = _def.ToPoolOptions();

			// Assert
			Assert.AreEqual(5, options.FillAmount);
		}

		[Test]
		public void ToPoolOptions_WithPrewarmSizeAboveConfiguredCapacity_RaisesCapacityToFitPrewarm()
		{
			// Arrange: a designer set a small Capacity but a larger PrewarmPoolSize; the pool must not
			// overflow on its own prewarm fill.
			_def.Prewarm = true;
			_def.PrewarmPoolSize = 5;
			_def.Capacity = 3;

			// Act
			var options = _def.ToPoolOptions();

			// Assert
			Assert.AreEqual(5, options.Capacity);
		}
	}

	// Minimal no-op IUIService used purely to satisfy ServiceInjector when a UIView component's Awake()
	// runs during a test that doesn't exercise UIService itself.
	public class NullUIService : IUIService
	{
		public void Initialize() { }
		public UIView SpawnView(string viewName) => null;
		public UIView SpawnView(UIViewDef viewDef) => null;
		public void DespawnView(UIView view) { }
	}
}
