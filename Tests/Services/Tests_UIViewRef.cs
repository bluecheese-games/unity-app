using BlueCheese.App;
using BlueCheese.Core.DI;
using BlueCheese.Core.Utils;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BlueCheese.Tests.Services
{
	// EditMode tests for UIViewRef. Like Tests_UIService, the referenced UIViewDef/prefab are backed by
	// real files on disk so AssetBaseRef can resolve them through the AssetDatabase.
	[TestFixture]
	public class Tests_UIViewRef
	{
		private const string TempFolder = "Assets/__UIViewRefTests__";

		private readonly List<UnityEngine.Object> _createdAssets = new();
		private ServiceContainer _container;
		private UIService _uiService;
		private UISettings _settings;

		[SetUp]
		public void SetUp()
		{
			if (!AssetDatabase.IsValidFolder(TempFolder))
				AssetDatabase.CreateFolder("Assets", "__UIViewRefTests__");

			var gameObjectService = new FakeGameObjectService();
			var logger = new FakeLogger<GameObjectPoolService>();
			var poolService = new GameObjectPoolService(gameObjectService, logger);
			_settings = ScriptableObject.CreateInstance<UISettings>();
			_uiService = new UIService(poolService, new OptionsWrapper<UISettings>(_settings));

			// UIViewRef.Spawn() resolves IUIService via ServiceLocator, and SpawnView() instantiates a real
			// UIView, whose Awake() injects [Injectable] fields via ServiceInjector.
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

		private UIViewRef CreateViewRef(string name)
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

			var assetRef = AssetBaseRef.FromAsset(def);
			AssetBank.InitializeForTests(new[] { assetRef });

			return new UIViewRef(assetRef.Guid);
		}

		#endregion

		[Test]
		public void Def_WithAssignedViewDef_ResolvesItByAsset()
		{
			// Arrange
			var viewRef = CreateViewRef("Screen_Ref");

			// Act & Assert
			Assert.IsNotNull(viewRef.Def);
			Assert.AreEqual("Screen_Ref", viewRef.Name);
			Assert.IsTrue(viewRef.IsValid);
		}

		[Test]
		public void Def_WithNoAssignedViewDef_IsInvalid()
		{
			// Arrange
			var viewRef = new UIViewRef();

			// Act & Assert
			Assert.IsNull(viewRef.Def);
			Assert.IsNull(viewRef.Name);
			Assert.IsFalse(viewRef.IsValid);
		}

		[Test]
		public void Spawn_ReturnsAPooledInstance_NotThePrefabItself()
		{
			// Arrange
			var viewRef = CreateViewRef("Screen_Spawn");

			// Act
			var view = viewRef.Spawn();

			// Assert
			Assert.IsNotNull(view);
			Assert.AreNotEqual(viewRef.Def.ViewPrefab, view);
		}

		[Test]
		public void SpawnGeneric_ReturnsTheRequestedComponentOnTheSpawnedInstance()
		{
			// Arrange
			var viewRef = CreateViewRef("Screen_SpawnGeneric");

			// Act
			var view = viewRef.Spawn<UIView>();

			// Assert
			Assert.IsNotNull(view);
		}

		[Test]
		public void Spawn_WithNoAssignedViewDef_ThrowsInvalidOperationException()
		{
			// Arrange
			var viewRef = new UIViewRef();

			// Act & Assert
			Assert.Throws<InvalidOperationException>(() => viewRef.Spawn());
		}
	}
}
