//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlueCheese.App.Editor
{
	/// <summary>
	/// Editor-only scanner that locates every serialized <c>TranslationKey</c> in the project
	/// (inside prefabs and scenes) that references a given localization key, and can rewrite
	/// those references. It works by serialized type, so it covers <c>LocalizedText</c> and any
	/// other component that embeds a <c>TranslationKey</c> (including its plural key field).
	/// </summary>
	public static class TranslationKeyReferenceFinder
	{
		private const string TranslationKeyType = "TranslationKey";

		public readonly struct Reference
		{
			public readonly string AssetPath;
			public readonly bool IsScene;
			public readonly string ObjectPath;
			public readonly string ComponentType;
			public readonly bool IsPlural;

			public Reference(string assetPath, bool isScene, string objectPath, string componentType, bool isPlural)
			{
				AssetPath = assetPath;
				IsScene = isScene;
				ObjectPath = objectPath;
				ComponentType = componentType;
				IsPlural = isPlural;
			}
		}

		public static List<string> FindPrefabPaths()
			=> AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }).Select(AssetDatabase.GUIDToAssetPath).ToList();

		public static List<string> FindScenePaths()
			=> AssetDatabase.FindAssets("t:Scene", new[] { "Assets" }).Select(AssetDatabase.GUIDToAssetPath).ToList();

		#region Scan

		public static void ScanPrefab(string path, string key, List<Reference> results)
		{
			var root = PrefabUtility.LoadPrefabContents(path);
			try
			{
				ScanRoots(new[] { root }, path, isScene: false, key, results);
			}
			finally
			{
				PrefabUtility.UnloadPrefabContents(root);
			}
		}

		public static void ScanScene(string path, string key, List<Reference> results)
		{
			var scene = SceneManager.GetSceneByPath(path);
			bool wasOpen = scene.IsValid() && scene.isLoaded;
			if (!wasOpen)
			{
				scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
			}
			try
			{
				ScanRoots(scene.GetRootGameObjects(), path, isScene: true, key, results);
			}
			finally
			{
				if (!wasOpen)
				{
					EditorSceneManager.CloseScene(scene, removeScene: true);
				}
			}
		}

		private static void ScanRoots(IEnumerable<GameObject> roots, string path, bool isScene, string key, List<Reference> results)
		{
			foreach (var root in roots)
			{
				foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
				{
					if (component == null)
					{
						continue;
					}
					var so = new SerializedObject(component);
					var iterator = so.GetIterator();
					bool enter = true;
					while (iterator.Next(enter))
					{
						enter = true;
						if (iterator.propertyType != SerializedPropertyType.Generic || iterator.type != TranslationKeyType)
						{
							continue;
						}

						// Found a TranslationKey: inspect its key fields, then skip its children.
						enter = false;
						var keyProp = iterator.FindPropertyRelative("_key");
						var pluralProp = iterator.FindPropertyRelative("_pluralKey");
						if (keyProp != null && keyProp.stringValue == key)
						{
							results.Add(new Reference(path, isScene, GetHierarchyPath(component.transform), component.GetType().Name, isPlural: false));
						}
						if (pluralProp != null && pluralProp.stringValue == key)
						{
							results.Add(new Reference(path, isScene, GetHierarchyPath(component.transform), component.GetType().Name, isPlural: true));
						}
					}
				}
			}
		}

		#endregion

		#region Rename

		public static int RenameInPrefab(string path, string oldKey, string newKey)
		{
			var root = PrefabUtility.LoadPrefabContents(path);
			try
			{
				int changed = RenameInRoots(new[] { root }, oldKey, newKey);
				if (changed > 0)
				{
					PrefabUtility.SaveAsPrefabAsset(root, path);
				}
				return changed;
			}
			finally
			{
				PrefabUtility.UnloadPrefabContents(root);
			}
		}

		public static int RenameInScene(string path, string oldKey, string newKey)
		{
			var scene = SceneManager.GetSceneByPath(path);
			bool wasOpen = scene.IsValid() && scene.isLoaded;
			if (!wasOpen)
			{
				scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
			}
			try
			{
				int changed = RenameInRoots(scene.GetRootGameObjects(), oldKey, newKey);
				if (changed > 0)
				{
					EditorSceneManager.MarkSceneDirty(scene);
					EditorSceneManager.SaveScene(scene);
				}
				return changed;
			}
			finally
			{
				if (!wasOpen)
				{
					EditorSceneManager.CloseScene(scene, removeScene: true);
				}
			}
		}

		private static int RenameInRoots(IEnumerable<GameObject> roots, string oldKey, string newKey)
		{
			int changed = 0;
			foreach (var root in roots)
			{
				foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
				{
					if (component == null)
					{
						continue;
					}
					var so = new SerializedObject(component);
					var iterator = so.GetIterator();
					bool enter = true;
					bool dirty = false;
					while (iterator.Next(enter))
					{
						enter = true;
						if (iterator.propertyType != SerializedPropertyType.Generic || iterator.type != TranslationKeyType)
						{
							continue;
						}

						enter = false;
						var keyProp = iterator.FindPropertyRelative("_key");
						var pluralProp = iterator.FindPropertyRelative("_pluralKey");
						if (keyProp != null && keyProp.stringValue == oldKey)
						{
							keyProp.stringValue = newKey;
							dirty = true;
						}
						if (pluralProp != null && pluralProp.stringValue == oldKey)
						{
							pluralProp.stringValue = newKey;
							dirty = true;
						}
					}
					if (dirty)
					{
						so.ApplyModifiedPropertiesWithoutUndo();
						changed++;
					}
				}
			}
			return changed;
		}

		#endregion

		private static string GetHierarchyPath(Transform transform)
		{
			var parts = new List<string>();
			while (transform != null)
			{
				parts.Add(transform.name);
				transform = transform.parent;
			}
			parts.Reverse();
			return string.Join("/", parts);
		}
	}
}
