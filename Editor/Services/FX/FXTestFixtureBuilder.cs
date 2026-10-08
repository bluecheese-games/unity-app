using BlueCheese.App;
using UnityEditor;
using UnityEngine;

namespace BlueCheese.App.Editor
{
	/// <summary>
	/// Builds the multi-ParticleSystem prefabs used to exercise how FXDef, FXInstance and the inspector
	/// preview handle child systems. No sample prefab has more than one system, so every child-related
	/// code path is currently untested; these fixtures make the behaviour observable.
	///
	/// Each system gets a distinct color so a missing or mistimed one can be identified by eye in the
	/// preview rather than inferred from numbers.
	/// </summary>
	public static class FXTestFixtureBuilder
	{
		private const string _prefabFolder = "Assets/unity-app/Sample/Prefabs/FX";
		private const string _defFolder = "Assets/unity-app/Sample/Data/FX";

		private const string _hierarchyName = "TestFX_Hierarchy";
		private const string _noRootName = "TestFX_NoRootSystem";
		private const string _detachedSubName = "TestFX_DetachedSubEmitter";
		private const string _localSpaceName = "TestFX_LocalSpace";

		private static readonly Color _orange = new(1f, 0.5f, 0f);
		private static readonly Color _skyBlue = new(0.3f, 0.6f, 1f);
		private static readonly Color _pink = new(1f, 0.4f, 0.7f);

		[MenuItem("BlueCheese/FX/Rebuild Test Fixtures")]
		public static void Build()
		{
			BuildHierarchyFixture();
			BuildNoRootSystemFixture();
			BuildDetachedSubEmitterFixture();
			BuildLocalSpaceFixture();

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			Debug.Log($"[FXTestFixtureBuilder] Rebuilt {_hierarchyName}, {_noRootName}, " +
				$"{_detachedSubName} and {_localSpaceName}.");
		}

		/// <summary>
		/// Root carries a system, so FXInstance has something to drive. Exercises: a transform child that
		/// outlives the root, a sub-emitter, and a system on a deactivated GameObject.
		/// </summary>
		private static void BuildHierarchyFixture()
		{
			var root = new GameObject(_hierarchyName);

			// Short burst. Its particles dying is what triggers the sub-emitter.
			var rootSystem = AddSystem(root, Color.white, duration: 1f, lifetime: 1f, startSize: 0.3f, startSpeed: 1.2f);
			SetBurst(rootSystem, count: 20);

			// Emits for the whole window and its particles live another 2s, so the effect really ends at
			// 0.5 + 2 + 2 = 4.5s -- well after the root is done. This is what any duration measurement has
			// to pick up, and what FXInstance would cut short by deactivating the GameObject.
			var continuous = AddChild(root, "Child_Continuous", new Vector3(-0.8f, 0f, 0f));
			var continuousSystem = AddSystem(continuous, Color.cyan, duration: 2f, lifetime: 2f, startSize: 0.2f, startSpeed: 0.6f);
			var continuousMain = continuousSystem.main;
			continuousMain.startDelay = 0.5f;
			var continuousEmission = continuousSystem.emission;
			continuousEmission.rateOverTime = 20f;

			// Should only appear around t=1s, when the root's particles die. If it shows up at t=0 it is
			// being simulated on its own clock instead of by the sub-emitter module.
			var subEmitter = AddChild(root, "Child_SubEmitter", Vector3.zero);
			var subEmitterSystem = AddSystem(subEmitter, Color.yellow, duration: 1f, lifetime: 1f, startSize: 0.15f, startSpeed: 0.8f);
			SetBurst(subEmitterSystem, count: 8);

			var sub = rootSystem.subEmitters;
			sub.enabled = true;
			sub.AddSubEmitter(subEmitterSystem, ParticleSystemSubEmitterType.Death, ParticleSystemSubEmitterProperties.InheritNothing);

			// Must never render, and must never count towards the duration. Given the longest duration and
			// lifetime of the whole prefab so that any code measuring it stands out immediately.
			var disabled = AddChild(root, "Child_Disabled", new Vector3(0.8f, 0f, 0f));
			var disabledSystem = AddSystem(disabled, Color.red, duration: 5f, lifetime: 2f, startSize: 0.4f, startSpeed: 1f);
			SetBurst(disabledSystem, count: 30);
			disabled.SetActive(false);

			SaveFixture(root, _hierarchyName);
		}

		/// <summary>
		/// Root carries no system at all, and the two children are siblings rather than a parent/child pair.
		/// Simulate/Play(withChildren: true) only covers one subtree, so both have to be driven separately.
		/// </summary>
		private static void BuildNoRootSystemFixture()
		{
			var root = new GameObject(_noRootName);

			var left = AddChild(root, "Sibling_Left", new Vector3(-0.6f, 0f, 0f));
			var leftSystem = AddSystem(left, Color.green, duration: 1.5f, lifetime: 1.5f, startSize: 0.25f, startSpeed: 1.5f);
			SetBurst(leftSystem, count: 15);

			var right = AddChild(root, "Sibling_Right", new Vector3(0.6f, 0f, 0f));
			var rightSystem = AddSystem(right, Color.magenta, duration: 1.5f, lifetime: 1.5f, startSize: 0.25f, startSpeed: 1.5f);
			SetBurst(rightSystem, count: 15);

			SaveFixture(root, _noRootName);
		}

		/// <summary>
		/// The sub-emitter is a SIBLING of the system that owns it, not a descendant. Nothing requires a
		/// sub-emitter to live under its owner, and CollectRoots only excludes systems that have a
		/// ParticleSystem ancestor -- so this one is classified as an independent root.
		///
		/// Timings are staggered so the duration gap is unmistakable: orange dies at 0.6s, which is when
		/// blue is born, and blue then lives another 1s. The effect really ends at 1.6s, while measuring
		/// each system on its own clock gives max(0.6, 1.0) = 1.0s and cuts the blue burst off entirely.
		/// </summary>
		private static void BuildDetachedSubEmitterFixture()
		{
			var root = new GameObject(_detachedSubName);

			var owner = AddChild(root, "Owner", Vector3.zero);
			var ownerSystem = AddSystem(owner, _orange, duration: 0.6f, lifetime: 0.6f, startSize: 0.3f, startSpeed: 1.2f);
			SetBurst(ownerSystem, count: 12);

			var detached = AddChild(root, "Detached_SubEmitter", Vector3.zero);
			var detachedSystem = AddSystem(detached, _skyBlue, duration: 1f, lifetime: 1f, startSize: 0.18f, startSpeed: 0.8f);
			SetBurst(detachedSystem, count: 6);

			var sub = ownerSystem.subEmitters;
			sub.enabled = true;
			sub.AddSubEmitter(detachedSystem, ParticleSystemSubEmitterType.Death, ParticleSystemSubEmitterProperties.InheritNothing);

			SaveFixture(root, _detachedSubName);
		}

		/// <summary>
		/// Mixes simulation spaces so the preview's "Local space" warning has something partial to report:
		/// one system of two is immune to moving the emitter. Ships with a non-zero Move speed so the
		/// difference is visible on selection -- the white world-space particles trail behind, the pink
		/// local-space ones stay glued to the emitter.
		/// </summary>
		private static void BuildLocalSpaceFixture()
		{
			var root = new GameObject(_localSpaceName);

			var rootSystem = AddSystem(root, _pink, duration: 1.5f, lifetime: 1.5f, startSize: 0.25f, startSpeed: 0.8f,
				space: ParticleSystemSimulationSpace.Local);
			SetBurst(rootSystem, count: 15);

			var world = AddChild(root, "Child_WorldSpace", Vector3.zero);
			var worldSystem = AddSystem(world, Color.white, duration: 1.5f, lifetime: 1.5f, startSize: 0.2f, startSpeed: 0.8f);
			SetBurst(worldSystem, count: 15);

			// Fast enough to separate the two systems, slow enough that the trail stays in frame for most of
			// the effect -- the camera tracks the emitter, so the trail exits the view at this speed.
			SaveFixture(root, _localSpaceName, moveSpeed: 2f);
		}

		private static GameObject AddChild(GameObject parent, string name, Vector3 localPosition)
		{
			var child = new GameObject(name);
			child.transform.SetParent(parent.transform, worldPositionStays: false);
			child.transform.localPosition = localPosition;
			return child;
		}

		private static ParticleSystem AddSystem(GameObject target, Color color, float duration, float lifetime, float startSize, float startSpeed,
			ParticleSystemSimulationSpace space = ParticleSystemSimulationSpace.World)
		{
			var system = target.AddComponent<ParticleSystem>();

			var main = system.main;
			main.duration = duration;
			main.loop = false;
			main.startLifetime = lifetime;
			main.startSize = startSize;
			main.startSpeed = startSpeed;
			main.startColor = new ParticleSystem.MinMaxGradient(color);

			// World by default so the preview's Move speed slider produces a real trail; local space systems
			// just follow the emitter and would make that control look broken.
			main.simulationSpace = space;

			// A system added by script has no material on its renderer, which renders as the magenta error
			// shader and makes every system look identical.
			var renderer = target.GetComponent<ParticleSystemRenderer>();
			renderer.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-ParticleSystem.mat");

			return system;
		}

		/// <summary>Burst-only emission: everything is born at t=0 and the system never emits again.</summary>
		private static void SetBurst(ParticleSystem system, int count)
		{
			var emission = system.emission;
			emission.enabled = true;
			emission.rateOverTime = 0f;
			emission.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });
		}

		private static void SaveFixture(GameObject root, string name, float moveSpeed = 0f)
		{
			string prefabPath = $"{_prefabFolder}/{name}.prefab";
			var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
			Object.DestroyImmediate(root);

			string defPath = $"{_defFolder}/{name}.asset";
			var def = AssetDatabase.LoadAssetAtPath<FXDef>(defPath);
			if (def == null)
			{
				def = ScriptableObject.CreateInstance<FXDef>();
				AssetDatabase.CreateAsset(def, defPath);

				// Default zoom frames these fixtures too tightly, which reads as "the FX is broken" rather
				// than "the camera is close". Only set on creation so a hand-tuned value is never clobbered.
				def._previewSettings.zoom = 1.6f;
				def._previewSettings.moveSpeed = moveSpeed;

				// Gives the Scaler slider something to do, and makes it visible which systems a scaler
				// actually reaches depending on ScaleNestedSystems.
				def.Scalers = new[]
				{
					new FXScaler
					{
						name = FXScaler.Type.ParticleCount.ToString(),
						type = FXScaler.Type.ParticleCount,
						curve = AnimationCurve.Linear(0f, 0.2f, 1f, 1f),
					},
				};
			}

			// FXDef.Duration is only ever derived from OnValidate, and neither CreateAsset nor a forced
			// reimport runs it. Assigning through a SerializedObject does, which is also how the inspector
			// would have done it.
			var serialized = new SerializedObject(def);
			serialized.FindProperty(nameof(FXDef.Prefab)).objectReferenceValue = prefab;
			serialized.ApplyModifiedProperties();

			EditorUtility.SetDirty(def);
			AssetDatabase.SaveAssetIfDirty(def);
		}
	}
}
