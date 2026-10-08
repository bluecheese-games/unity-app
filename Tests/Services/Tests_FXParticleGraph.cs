using BlueCheese.App;
using NUnit.Framework;
using UnityEngine;

namespace BlueCheese.Tests.Services
{
	public class Tests_FXParticleGraph
	{
		private GameObject _root;

		[TearDown]
		public void TearDown()
		{
			if (_root != null)
			{
				Object.DestroyImmediate(_root);
				_root = null;
			}
		}

		#region Classification

		[Test]
		public void Test_Build_NullRoot_ReturnsEmptyGraph()
		{
			var graph = FXParticleGraph.Build(null);

			Assert.That(graph.IsEmpty, Is.True);
			Assert.That(graph.Roots, Is.Empty);
		}

		[Test]
		public void Test_Build_NoParticleSystemAnywhere_ReturnsEmptyGraph()
		{
			_root = new GameObject("Root");
			AddChild(_root, "Plain");

			var graph = FXParticleGraph.Build(_root);

			Assert.That(graph.IsEmpty, Is.True);
		}

		[Test]
		public void Test_Build_SystemOnRoot_IsTheOnlyRoot()
		{
			_root = NewRootWithSystem();

			var graph = FXParticleGraph.Build(_root);

			Assert.That(graph.All.Length, Is.EqualTo(1));
			Assert.That(graph.Roots.Length, Is.EqualTo(1));
		}

		[Test]
		public void Test_Build_SystemNestedUnderAnother_IsNotARoot()
		{
			_root = NewRootWithSystem();
			AddSystem(AddChild(_root, "Nested"));

			var graph = FXParticleGraph.Build(_root);

			Assert.That(graph.All.Length, Is.EqualTo(2));
			Assert.That(graph.Roots.Length, Is.EqualTo(1), "A system under another one is reached through withChildren.");
		}

		[Test]
		public void Test_Build_SiblingsWithNoSystemOnRoot_AreBothRoots()
		{
			// The regression this guards: FXInstance used to look for a system on the root only, so a prefab
			// shaped like this played nothing at all.
			_root = new GameObject("Root");
			AddSystem(AddChild(_root, "Left"));
			AddSystem(AddChild(_root, "Right"));

			var graph = FXParticleGraph.Build(_root);

			Assert.That(graph.Roots.Length, Is.EqualTo(2));
		}

		[Test]
		public void Test_Build_DeactivatedBranch_IsExcluded()
		{
			_root = NewRootWithSystem();
			var disabled = AddChild(_root, "Disabled");
			AddSystem(disabled);
			disabled.SetActive(false);

			var graph = FXParticleGraph.Build(_root);

			Assert.That(graph.All.Length, Is.EqualTo(1), "A system that can never play must not be counted.");
		}

		[Test]
		public void Test_Build_SystemUnderDeactivatedParent_IsExcluded()
		{
			_root = new GameObject("Root");
			var branch = AddChild(_root, "Branch");
			AddSystem(AddChild(branch, "Leaf"));
			branch.SetActive(false);

			var graph = FXParticleGraph.Build(_root);

			Assert.That(graph.IsEmpty, Is.True, "Being under a deactivated ancestor is just as disabling.");
		}

		[Test]
		public void Test_Build_DeactivatedRoot_StillFindsItsSystems()
		{
			// A pooled FXInstance deactivates its own GameObject when it stops, and Setup runs in that state.
			// Testing the root's own activeSelf would classify the whole prefab as disabled.
			_root = NewRootWithSystem();
			AddSystem(AddChild(_root, "Child"));
			_root.SetActive(false);

			var graph = FXParticleGraph.Build(_root);

			Assert.That(graph.All.Length, Is.EqualTo(2));
			Assert.That(graph.Roots.Length, Is.EqualTo(1));
		}

		#endregion

		#region Curve and emission helpers

		[Test]
		public void Test_MaxOf_Constant_ReturnsConstant()
		{
			var curve = new ParticleSystem.MinMaxCurve(2.5f);

			Assert.That(FXParticleGraph.MaxOf(curve), Is.EqualTo(2.5f));
		}

		[Test]
		public void Test_MaxOf_TwoConstants_ReturnsUpperBound()
		{
			var curve = new ParticleSystem.MinMaxCurve(1f, 4f);

			Assert.That(FXParticleGraph.MaxOf(curve), Is.EqualTo(4f));
		}

		[Test]
		public void Test_MaxOf_Curve_ReturnsMultiplier()
		{
			var curve = new ParticleSystem.MinMaxCurve(3f, AnimationCurve.Linear(0f, 0f, 1f, 1f));

			Assert.That(FXParticleGraph.MaxOf(curve), Is.EqualTo(3f));
		}

		[Test]
		public void Test_LastEmissionTime_BurstAtZero_IsZeroNotDuration()
		{
			_root = NewRootWithSystem();
			var system = Configure(_root, duration: 1f, lifetime: 1f);
			SetBurst(system, time: 0f, cycles: 1);

			Assert.That(FXParticleGraph.LastEmissionTime(system), Is.EqualTo(0f));
		}

		[Test]
		public void Test_LastEmissionTime_ContinuousRate_IsDuration()
		{
			_root = NewRootWithSystem();
			var system = Configure(_root, duration: 1f, lifetime: 1f);
			SetRate(system, 10f);

			Assert.That(FXParticleGraph.LastEmissionTime(system), Is.EqualTo(1f));
		}

		[Test]
		public void Test_LastEmissionTime_RepeatingBurst_AccountsForCycles()
		{
			_root = NewRootWithSystem();
			var system = Configure(_root, duration: 5f, lifetime: 1f);
			SetBurst(system, time: 0.5f, cycles: 3, repeatInterval: 0.25f);

			Assert.That(FXParticleGraph.LastEmissionTime(system), Is.EqualTo(1f).Within(0.0001f)); // 0.5 + 2 * 0.25
		}

		#endregion

		#region Measurement

		[Test]
		public void Test_Build_BurstOnlySystem_EffectEndsWithTheLastParticle()
		{
			// The regression this guards: duration + lifetime would say 2s for an effect that is over at 1s.
			_root = NewRootWithSystem();
			var system = Configure(_root, duration: 1f, lifetime: 1f);
			SetBurst(system, time: 0f, cycles: 1);

			var graph = FXParticleGraph.Build(_root);

			Assert.That(graph.EffectLength, Is.EqualTo(1f));
		}

		[Test]
		public void Test_Build_ContinuousSystem_StillAllowsForTheTail()
		{
			_root = NewRootWithSystem();
			var system = Configure(_root, duration: 1f, lifetime: 1f);
			SetRate(system, 10f);

			var graph = FXParticleGraph.Build(_root);

			Assert.That(graph.EffectLength, Is.EqualTo(2f));
		}

		[Test]
		public void Test_Build_StartDelay_PushesTheEffectBack()
		{
			_root = NewRootWithSystem();
			var system = Configure(_root, duration: 1f, lifetime: 1f, startDelay: 0.5f);
			SetBurst(system, time: 0f, cycles: 1);

			var graph = FXParticleGraph.Build(_root);

			Assert.That(graph.EffectLength, Is.EqualTo(1.5f));
		}

		[Test]
		public void Test_Build_DeactivatedSystem_DoesNotStretchTheEffect()
		{
			_root = NewRootWithSystem();
			var system = Configure(_root, duration: 1f, lifetime: 1f);
			SetBurst(system, time: 0f, cycles: 1);

			var disabled = AddChild(_root, "Disabled");
			var disabledSystem = Configure(disabled, duration: 10f, lifetime: 10f);
			SetRate(disabledSystem, 10f);
			disabled.SetActive(false);

			var graph = FXParticleGraph.Build(_root);

			Assert.That(graph.EffectLength, Is.EqualTo(1f), "A system that never plays must not set the duration.");
		}

		[Test]
		public void Test_Build_LoopingSystem_ReportsOneLoopAsTheReference()
		{
			_root = NewRootWithSystem();
			var system = Configure(_root, duration: 2f, lifetime: 5f, loop: true);
			SetRate(system, 10f);

			var graph = FXParticleGraph.Build(_root);

			Assert.That(graph.AnyLooping, Is.True);
			Assert.That(graph.LoopLength, Is.EqualTo(2f));
			Assert.That(graph.ReferenceLength, Is.EqualTo(2f), "A looping effect never reaches its effect length.");
		}

		[Test]
		public void Test_Build_NonLoopingSystem_ReferenceIsTheWholeEffect()
		{
			_root = NewRootWithSystem();
			var system = Configure(_root, duration: 1f, lifetime: 1f);
			SetRate(system, 10f);

			var graph = FXParticleGraph.Build(_root);

			Assert.That(graph.AnyLooping, Is.False);
			Assert.That(graph.ReferenceLength, Is.EqualTo(2f));
		}

		[Test]
		public void Test_Build_DeathSubEmitter_ExtendsTheEffectPastItsOwner()
		{
			// Owner is done at 0.6s, which is exactly when the sub-emitter is born; it then lives another 1s.
			// Measuring each system on its own clock would report max(0.6, 1.0) = 1.0 and cut the tail off.
			_root = NewRootWithSystem();
			var owner = Configure(_root, duration: 0.6f, lifetime: 0.6f);
			SetBurst(owner, time: 0f, cycles: 1);

			var child = AddChild(_root, "Sub");
			var childSystem = Configure(child, duration: 1f, lifetime: 1f);
			SetBurst(childSystem, time: 0f, cycles: 1);
			AttachSubEmitter(owner, childSystem, ParticleSystemSubEmitterType.Death);

			var graph = FXParticleGraph.Build(_root);

			Assert.That(graph.EffectLength, Is.EqualTo(1.6f).Within(0.0001f));
		}

		[Test]
		public void Test_Build_BirthSubEmitter_StartsAtTheEndOfEmissionNotOfLife()
		{
			// Birth fires while the owner is still emitting, so the sub-emitter starts at 2s (end of the
			// emission window), not at 3s (when the owner's last particle dies).
			_root = NewRootWithSystem();
			var owner = Configure(_root, duration: 2f, lifetime: 1f);
			SetRate(owner, 10f);

			var child = AddChild(_root, "Sub");
			var childSystem = Configure(child, duration: 1f, lifetime: 2f);
			SetBurst(childSystem, time: 0f, cycles: 1);
			AttachSubEmitter(owner, childSystem, ParticleSystemSubEmitterType.Birth);

			var graph = FXParticleGraph.Build(_root);

			Assert.That(graph.EffectLength, Is.EqualTo(4f).Within(0.0001f));
		}

		[Test]
		public void Test_Build_NoSubEmitterAnywhere_ReportsNone()
		{
			_root = NewRootWithSystem();
			AddSystem(AddChild(_root, "Child"));

			var graph = FXParticleGraph.Build(_root);

			Assert.That(graph.HasSubEmitters, Is.False);
		}

		[Test]
		public void Test_Build_SubEmitterPresent_IsReported()
		{
			// Drives the preview's choice of seek strategy: a single Simulate jump leaves sub-emitter
			// particles with empty renderer bounds, so they vanish while scrubbing.
			_root = NewRootWithSystem();
			var owner = Configure(_root, duration: 1f, lifetime: 1f);
			SetBurst(owner, time: 0f, cycles: 1);

			var child = AddChild(_root, "Sub");
			var childSystem = Configure(child, duration: 1f, lifetime: 1f);
			SetBurst(childSystem, time: 0f, cycles: 1);
			AttachSubEmitter(owner, childSystem, ParticleSystemSubEmitterType.Death);

			var graph = FXParticleGraph.Build(_root);

			Assert.That(graph.HasSubEmitters, Is.True);
		}

		[Test]
		public void Test_Build_SubEmitterModuleDisabled_IsNotReported()
		{
			_root = NewRootWithSystem();
			var owner = Configure(_root, duration: 1f, lifetime: 1f);
			var child = AddChild(_root, "Sub");
			var childSystem = Configure(child, duration: 1f, lifetime: 1f);
			AttachSubEmitter(owner, childSystem, ParticleSystemSubEmitterType.Death);

			var sub = owner.subEmitters;
			sub.enabled = false;

			var graph = FXParticleGraph.Build(_root);

			Assert.That(graph.HasSubEmitters, Is.False, "A disabled module emits nothing, so nothing special is needed.");
		}

		[Test]
		public void Test_Build_MutuallyReferencingSubEmitters_StillReportsAUsableLength()
		{
			// Nothing stops an author from wiring this. It must terminate rather than blow the stack, and it
			// must not report zero -- a zero duration makes FXInstance stop the effect on its first frame.
			_root = NewRootWithSystem();
			var first = Configure(_root, duration: 1f, lifetime: 1f);
			SetBurst(first, time: 0f, cycles: 1);

			var child = AddChild(_root, "Second");
			var second = Configure(child, duration: 1f, lifetime: 1f);
			SetBurst(second, time: 0f, cycles: 1);

			AttachSubEmitter(first, second, ParticleSystemSubEmitterType.Death);
			AttachSubEmitter(second, first, ParticleSystemSubEmitterType.Death);

			var graph = FXParticleGraph.Build(_root);

			Assert.That(graph.EffectLength, Is.GreaterThan(0f));
		}

		#endregion

		private static GameObject NewRootWithSystem()
		{
			var root = new GameObject("Root");
			AddSystem(root);
			return root;
		}

		private static GameObject AddChild(GameObject parent, string name)
		{
			var child = new GameObject(name);
			child.transform.SetParent(parent.transform, worldPositionStays: false);
			return child;
		}

		private static ParticleSystem AddSystem(GameObject target) => target.AddComponent<ParticleSystem>();

		private static ParticleSystem Configure(GameObject target, float duration, float lifetime, float startDelay = 0f, bool loop = false)
		{
			var system = target.GetComponent<ParticleSystem>();
			if (system == null)
			{
				system = AddSystem(target);
			}

			var main = system.main;
			main.duration = duration;
			main.startLifetime = lifetime;
			main.startDelay = startDelay;
			main.loop = loop;

			return system;
		}

		private static void SetBurst(ParticleSystem system, float time, int cycles, float repeatInterval = 0.01f)
		{
			var emission = system.emission;
			emission.enabled = true;
			emission.rateOverTime = 0f;
			emission.rateOverDistance = 0f;
			emission.SetBursts(new[] { new ParticleSystem.Burst(time, 10, cycles, repeatInterval) });
		}

		private static void SetRate(ParticleSystem system, float rateOverTime)
		{
			var emission = system.emission;
			emission.enabled = true;
			emission.rateOverTime = rateOverTime;
			emission.rateOverDistance = 0f;
			emission.SetBursts(new ParticleSystem.Burst[0]);
		}

		private static void AttachSubEmitter(ParticleSystem owner, ParticleSystem child, ParticleSystemSubEmitterType type)
		{
			var sub = owner.subEmitters;
			sub.enabled = true;
			sub.AddSubEmitter(child, type, ParticleSystemSubEmitterProperties.InheritNothing);
		}
	}
}
