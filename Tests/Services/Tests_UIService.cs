//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using BlueCheese.App;
using BlueCheese.Core.DI;
using BlueCheese.Core.Utils;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BlueCheese.Tests.Services
{
	// EditMode tests for UIService. UIViewDef assets (and the UIView prefabs they reference) are backed by
	// real files on disk so AssetBaseRef can resolve them through the AssetDatabase (the editor load path),
	// mirroring the convention established by unity-core's Tests_AssetBank.
	[TestFixture]
	public class Tests_UIService
	{
		private const string TempFolder = "Assets/__UIServiceTests__";

		private readonly List<UnityEngine.Object> _createdAssets = new();
		private ServiceContainer _container;
		private UIService _uiService;
		private UISettings _settings;

		[SetUp]
		public void SetUp()
		{
			if (!AssetDatabase.IsValidFolder(TempFolder))
				AssetDatabase.CreateFolder("Assets", "__UIServiceTests__");

			var gameObjectService = new FakeGameObjectService();
			var logger = new FakeLogger<GameObjectPoolService>();
			var poolService = new GameObjectPoolService(gameObjectService, logger);
			_settings = ScriptableObject.CreateInstance<UISettings>();
			_uiService = new UIService(poolService, new OptionsWrapper<UISettings>(_settings));

			// SpawnView() instantiates a real UIView, whose Awake() injects [Injectable] fields via
			// ServiceInjector; that requires an initialized ServiceLocator even in this unit test.
			_container = new ServiceContainer();
			_container.Register<IUIService>(_uiService);
			ServiceLocator.Initialize(_container);
		}

		[TearDown]
		public void TearDown()
		{
			ServiceLocator.Dispose();
			NavigableView.ResetForTests();
			AssetBank.ResetForTests();
			_createdAssets.Clear();
			UnityEngine.Object.DestroyImmediate(_settings);
			if (AssetDatabase.IsValidFolder(TempFolder))
				AssetDatabase.DeleteAsset(TempFolder);
		}

		#region Helpers

		private UIViewDef CreateViewDef(string name)
		{
			var prefabObject = new GameObject(name);
			prefabObject.AddComponent<UIView>();
			string prefabPath = $"{TempFolder}/{name}.prefab";
			var prefab = PrefabUtility.SaveAsPrefabAsset(prefabObject, prefabPath);
			UnityEngine.Object.DestroyImmediate(prefabObject);
			_createdAssets.Add(prefab);

			var def = ScriptableObject.CreateInstance<UIViewDef>();
			def.Name = name;
			def.ViewPrefab = prefab.GetComponent<UIView>();
			AssetDatabase.CreateAsset(def, $"{TempFolder}/{name}Def.asset");
			_createdAssets.Add(def);
			return def;
		}

		private UIViewDef CreateViewDefWithCanvasScaler(string name, CanvasScaler.ScaleMode prefabScaleMode)
		{
			var prefabObject = new GameObject(name, typeof(Canvas), typeof(CanvasScaler));
			prefabObject.AddComponent<UIView>();
			prefabObject.GetComponent<CanvasScaler>().uiScaleMode = prefabScaleMode;
			string prefabPath = $"{TempFolder}/{name}.prefab";
			var prefab = PrefabUtility.SaveAsPrefabAsset(prefabObject, prefabPath);
			UnityEngine.Object.DestroyImmediate(prefabObject);
			_createdAssets.Add(prefab);

			var def = ScriptableObject.CreateInstance<UIViewDef>();
			def.Name = name;
			def.ViewPrefab = prefab.GetComponent<UIView>();
			AssetDatabase.CreateAsset(def, $"{TempFolder}/{name}Def.asset");
			_createdAssets.Add(def);
			return def;
		}

		#endregion

		[Test]
		public void SpawnView_WithRegisteredName_ReturnsInstance()
		{
			// Arrange
			var def = CreateViewDef("Screen_Registered");
			AssetBank.InitializeForTests(new[] { AssetBaseRef.FromAsset(def) });
			_uiService.Initialize();

			// Act
			var view = _uiService.SpawnView("Screen_Registered");

			// Assert
			Assert.IsNotNull(view);
			Assert.AreNotEqual(def.ViewPrefab, view, "SpawnView should return a pooled instance, not the prefab itself.");
		}

		[Test]
		public void SpawnView_WithUIViewDef_ReturnsInstance_WithoutRequiringAssetBankRegistration()
		{
			// Arrange: the UIViewDef overload takes the def directly, so it works even when the def isn't
			// (or isn't yet) registered in the AssetBank -- unlike the name-based overload.
			var def = CreateViewDef("Screen_ByDef");
			AssetBank.InitializeForTests(Array.Empty<AssetBaseRef>());
			_uiService.Initialize();

			// Act
			var view = _uiService.SpawnView(def);

			// Assert
			Assert.IsNotNull(view);
			Assert.AreNotEqual(def.ViewPrefab, view);
		}

		[Test]
		public void SpawnView_WithInvalidUIViewDef_ThrowsArgumentException()
		{
			// Arrange: a def with no ViewPrefab assigned.
			var def = ScriptableObject.CreateInstance<UIViewDef>();
			_createdAssets.Add(def);

			// Act & Assert
			Assert.Throws<ArgumentException>(() => _uiService.SpawnView(def));
		}

		[Test]
		public void SpawnView_WithNullUIViewDef_ThrowsArgumentException()
		{
			// Act & Assert
			Assert.Throws<ArgumentException>(() => _uiService.SpawnView((UIViewDef)null));
		}

		[Test]
		public void SpawnView_WithUnknownName_ThrowsArgumentException()
		{
			// Arrange
			AssetBank.InitializeForTests(Array.Empty<AssetBaseRef>());
			_uiService.Initialize();

			// Act & Assert
			Assert.Throws<ArgumentException>(() => _uiService.SpawnView("Missing"));
		}

		[Test]
		public void DespawnView_ThenSpawnAgain_ReusesTheSamePooledInstance()
		{
			// Arrange
			var def = CreateViewDef("Screen_Reused");
			AssetBank.InitializeForTests(new[] { AssetBaseRef.FromAsset(def) });
			_uiService.Initialize();
			var first = _uiService.SpawnView("Screen_Reused");

			// Act
			_uiService.DespawnView(first);
			var second = _uiService.SpawnView("Screen_Reused");

			// Assert: the pool recycles the same instance instead of instantiating a new one.
			Assert.AreSame(first, second);
		}

		[Test]
		public void SpawnView_AfterHidingPreviousInstance_ReusesItInsteadOfCreatingANewOne()
		{
			// Regression test for the reported bug: hiding a view (UIView.Hide() / Popup.Hide() / SetResult
			// / Ok() / Cancel(), all of which funnel into ToggleableView.Toggle(false)) previously never
			// returned the instance to the pool -- only an explicit DespawnView() call did, which nothing in
			// the UIView/Popup API ever called on its own. So spawning the same view again after hiding it
			// (e.g. reopening the same popup) always instantiated a brand new instance instead of reusing
			// the one that was just hidden.
			var def = CreateViewDef("Screen_HideThenRespawn");
			AssetBank.InitializeForTests(new[] { AssetBaseRef.FromAsset(def) });
			_uiService.Initialize();
			var first = _uiService.SpawnView("Screen_HideThenRespawn");

			// Act
			first.Hide();
			var second = _uiService.SpawnView("Screen_HideThenRespawn");

			// Assert
			Assert.AreSame(first, second);
		}

		[Test]
		public void SpawnView_WithCanvasScaler_OverridesItToMatchTheGlobalUISettings()
		{
			// Arrange: the prefab's own CanvasScaler is deliberately misconfigured (Constant Pixel Size),
			// while the app-wide UISettings.Canvas section says Scale With Screen Size at a specific
			// resolution. This is exactly the scenario the settings exist to prevent: per-prefab drift.
			_settings.Canvas.UiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			_settings.Canvas.ReferenceResolution = new Vector2(1280, 720);
			var def = CreateViewDefWithCanvasScaler("Screen_WithScaler", CanvasScaler.ScaleMode.ConstantPixelSize);
			AssetBank.InitializeForTests(new[] { AssetBaseRef.FromAsset(def) });
			_uiService.Initialize();

			// Act
			var view = _uiService.SpawnView("Screen_WithScaler");

			// Assert: whatever the prefab had saved is irrelevant -- the app-wide settings always win.
			var scaler = view.GetComponent<CanvasScaler>();
			Assert.AreEqual(CanvasScaler.ScaleMode.ScaleWithScreenSize, scaler.uiScaleMode);
			Assert.AreEqual(new Vector2(1280, 720), scaler.referenceResolution);
		}

		[Test]
		public void SpawnView_WithoutCanvasScaler_DoesNotThrow()
		{
			// Arrange: most views (see CreateViewDef) have no Canvas/CanvasScaler at all.
			var def = CreateViewDef("Screen_NoScaler");
			AssetBank.InitializeForTests(new[] { AssetBaseRef.FromAsset(def) });
			_uiService.Initialize();

			// Act & Assert
			Assert.DoesNotThrow(() => _uiService.SpawnView("Screen_NoScaler"));
		}

		[Test]
		public void DespawnView_WithNullView_DoesNotThrow()
		{
			// Act & Assert
			Assert.DoesNotThrow(() => _uiService.DespawnView(null));
		}

		[Test]
		public void DespawnView_WithViewNotSpawnedByService_ThrowsArgumentException()
		{
			// Arrange: a UIView that exists but was never handed out by UIService.SpawnView, so it carries
			// no GameObjectPool.PoolItem component to route the despawn through.
			var strayView = new GameObject("Stray").AddComponent<UIView>();

			// Act & Assert
			Assert.Throws<ArgumentException>(() => _uiService.DespawnView(strayView));

			// Cleanup
			UnityEngine.Object.DestroyImmediate(strayView.gameObject);
		}
	}
}
