using System.Collections.Generic;
using System.Linq;
using TMPro;
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
			public readonly int MaxChars;         // estimated character budget from the UI (0 = unknown/unconstrained)
			public readonly string[] SiblingTexts; // other texts found under the same parent object

			public Reference(string assetPath, bool isScene, string objectPath, string componentType, bool isPlural, int maxChars, string[] siblingTexts)
			{
				AssetPath = assetPath;
				IsScene = isScene;
				ObjectPath = objectPath;
				ComponentType = componentType;
				IsPlural = isPlural;
				MaxChars = maxChars;
				SiblingTexts = siblingTexts;
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
						int maxChars = EstimateMaxChars(component);
						var siblings = GatherSiblingTexts(component);
						if (keyProp != null && keyProp.stringValue == key)
						{
							results.Add(new Reference(path, isScene, GetHierarchyPath(component.transform), component.GetType().Name, isPlural: false, maxChars, siblings));
						}
						if (pluralProp != null && pluralProp.stringValue == key)
						{
							results.Add(new Reference(path, isScene, GetHierarchyPath(component.transform), component.GetType().Name, isPlural: true, maxChars, siblings));
						}
					}
				}
			}
		}

		// Scans the whole project ONCE for references to any of the given keys (efficient for batches).
		public static Dictionary<string, List<Reference>> FindReferences(IEnumerable<string> keys, System.Action<float, string> onProgress = null)
		{
			var keySet = new HashSet<string>(keys);
			var results = new Dictionary<string, List<Reference>>();
			foreach (var key in keySet)
			{
				results[key] = new List<Reference>();
			}
			if (keySet.Count == 0)
			{
				return results;
			}

			var prefabPaths = FindPrefabPaths();
			var scenePaths = FindScenePaths();
			int total = prefabPaths.Count + scenePaths.Count;
			int done = 0;

			foreach (var path in prefabPaths)
			{
				onProgress?.Invoke(total == 0 ? 1f : (float)done / total, path);
				var root = PrefabUtility.LoadPrefabContents(path);
				try { ScanRootsForKeys(new[] { root }, path, isScene: false, keySet, results); }
				finally { PrefabUtility.UnloadPrefabContents(root); }
				done++;
			}
			foreach (var path in scenePaths)
			{
				onProgress?.Invoke(total == 0 ? 1f : (float)done / total, path);
				var scene = SceneManager.GetSceneByPath(path);
				bool wasOpen = scene.IsValid() && scene.isLoaded;
				if (!wasOpen)
				{
					scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
				}
				try { ScanRootsForKeys(scene.GetRootGameObjects(), path, isScene: true, keySet, results); }
				finally { if (!wasOpen) EditorSceneManager.CloseScene(scene, removeScene: true); }
				done++;
			}
			return results;
		}

		private static void ScanRootsForKeys(IEnumerable<GameObject> roots, string path, bool isScene, HashSet<string> keys, Dictionary<string, List<Reference>> results)
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

						enter = false;
						var keyProp = iterator.FindPropertyRelative("_key");
						var pluralProp = iterator.FindPropertyRelative("_pluralKey");
						int maxChars = EstimateMaxChars(component);
						var siblings = GatherSiblingTexts(component);
						if (keyProp != null && keys.Contains(keyProp.stringValue))
						{
							results[keyProp.stringValue].Add(new Reference(path, isScene, GetHierarchyPath(component.transform), component.GetType().Name, isPlural: false, maxChars, siblings));
						}
						if (pluralProp != null && keys.Contains(pluralProp.stringValue))
						{
							results[pluralProp.stringValue].Add(new Reference(path, isScene, GetHierarchyPath(component.transform), component.GetType().Name, isPlural: true, maxChars, siblings));
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

		// Rough UI character budget from the TMP field size and font size (0 = unknown/unconstrained).
		internal static int EstimateMaxChars(Component component)
		{
			var tmp = component.GetComponent<TMP_Text>();
			if (tmp == null || tmp.enableAutoSizing)
			{
				return 0;
			}
			if (component.transform is not RectTransform rectTransform)
			{
				return 0;
			}
			var rect = rectTransform.rect;
			float fontSize = tmp.fontSize;
			if (rect.width <= 1f || fontSize <= 0f)
			{
				return 0;
			}
			int perLine = Mathf.Max(1, Mathf.FloorToInt(rect.width / (fontSize * 0.5f)));
			int lines = Mathf.Max(1, Mathf.FloorToInt(rect.height / (fontSize * 1.2f)));
			return perLine * lines;
		}

		// Collects the text values of other TMP_Text components under the same parent object,
		// to give the AI nearby-UI context (e.g. neighbouring labels/buttons).
		internal static string[] GatherSiblingTexts(Component component)
		{
			var parent = component.transform.parent;
			if (parent == null)
			{
				return System.Array.Empty<string>();
			}
			var texts = new List<string>();
			foreach (var tmp in parent.GetComponentsInChildren<TMP_Text>(true))
			{
				if (tmp.gameObject == component.gameObject)
				{
					continue; // skip the text on the key's own object
				}
				var value = tmp.text;
				if (!string.IsNullOrWhiteSpace(value) && !texts.Contains(value))
				{
					texts.Add(value);
					if (texts.Count >= 10)
					{
						break;
					}
				}
			}
			return texts.ToArray();
		}

		internal static string GetHierarchyPath(Transform transform)
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
