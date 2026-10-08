using BlueCheese.App;
using NUnit.Framework;
using UnityEngine;

namespace BlueCheese.Tests.Services
{
	public class Tests_FXInstance
	{
		private GameObject _instance;
		private FXDef _def;

		[TearDown]
		public void TearDown()
		{
			if (_instance != null)
			{
				Object.DestroyImmediate(_instance);
				_instance = null;
			}

			if (_def != null)
			{
				Object.DestroyImmediate(_def);
				_def = null;
			}
		}

		[Test]
		public void Test_Play_ScaleNestedSystemsOff_ReachesRootsOnly()
		{
			BuildHierarchy();
			_def.ScaleNestedSystems = false;

			PlayInstance();

			Assert.That(ScaledSystemCount(), Is.EqualTo(1), "Only the system that has to be started explicitly.");
		}

		[Test]
		public void Test_Play_ScaleNestedSystemsOn_ReachesEverySystem()
		{
			BuildHierarchy();
			_def.ScaleNestedSystems = true;

			PlayInstance();

			Assert.That(ScaledSystemCount(), Is.EqualTo(3), "Root, transform child and sub-emitter alike.");
		}

		[Test]
		public void Test_Play_ScaleNestedSystemsOn_DeactivatedSystemIsStillSkipped()
		{
			BuildHierarchy();
			var disabled = AddChild(_instance, "Disabled");
			AddSystem(disabled);
			disabled.SetActive(false);
			_def.ScaleNestedSystems = true;

			PlayInstance();

			Assert.That(ScaledSystemCount(), Is.EqualTo(3), "A system that never plays is not worth scaling.");
		}

		[Test]
		public void Test_Play_NoSystemOnRoot_StillPlaysTheSiblings()
		{
			// The regression this guards: Setup used to look for a system on the root only, so this prefab
			// shape produced an FXInstance that played nothing at all.
			_instance = new GameObject("FX");
			AddSystem(AddChild(_instance, "Left"));
			AddSystem(AddChild(_instance, "Right"));
			NewDef();

			PlayInstance();

			int playing = 0;
			foreach (var system in _instance.GetComponentsInChildren<ParticleSystem>(true))
			{
				if (system.isPlaying)
				{
					playing++;
				}
			}

			Assert.That(playing, Is.EqualTo(2));
		}

		/// <summary>Root system, a plain transform child, and a sub-emitter of the root.</summary>
		private void BuildHierarchy()
		{
			_instance = new GameObject("FX");
			var root = AddSystem(_instance);

			AddSystem(AddChild(_instance, "Child"));

			var sub = AddSystem(AddChild(_instance, "SubEmitter"));
			var subModule = root.subEmitters;
			subModule.enabled = true;
			subModule.AddSubEmitter(sub, ParticleSystemSubEmitterType.Death, ParticleSystemSubEmitterProperties.InheritNothing);

			NewDef();
		}

		private void NewDef()
		{
			_def = ScriptableObject.CreateInstance<FXDef>();
			_def.Prefab = _instance;
			_def.Scalers = new[]
			{
				new FXScaler
				{
					name = FXScaler.Type.ParticleCount.ToString(),
					type = FXScaler.Type.ParticleCount,
					curve = AnimationCurve.Constant(0f, 1f, 1f),
				},
			};
		}

		private void PlayInstance()
		{
			var fx = _instance.AddComponent<FXInstance>();
			fx.Setup(_def);
			fx.PlayAt(Vector3.zero);
		}

		// A scaler installs a component on each system it is applied to, which is the only observable trace
		// of which systems it reached.
		private int ScaledSystemCount() => _instance.GetComponentsInChildren<FXScalerBase>(true).Length;

		private static GameObject AddChild(GameObject parent, string name)
		{
			var child = new GameObject(name);
			child.transform.SetParent(parent.transform, worldPositionStays: false);
			return child;
		}

		private static ParticleSystem AddSystem(GameObject target) => target.AddComponent<ParticleSystem>();
	}
}
