using BlueCheese.App;
using BlueCheese.Core.DI;
using BlueCheese.Core.Utils;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using System;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace BlueCheese.Tests.Services
{
	[TestFixture]
	public class Tests_Popup
	{
		private GameObject _go;

		[SetUp]
		public void SetUp()
		{
			// See Tests_UIViewBehaviour.SetUp: Popup (a UIViewBehaviour) cascades into auto-adding a UIView
			// via an inherited RequireComponent, whose Awake() needs an initialized ServiceLocator to inject
			// IUIService without throwing.
			var container = new ServiceContainer();
			container.Register<IUIService>(new NullUIService());
			ServiceLocator.Initialize(container);
		}

		[TearDown]
		public void TearDown()
		{
			ServiceLocator.Dispose();
			NavigableView.ResetForTests();
			AssetBank.ResetForTests();
			if (_go != null) UnityEngine.Object.DestroyImmediate(_go);
		}

		[Test]
		[Timeout(5000)]
		public async Task ShowAsync_ThenOk_ReturnsOkResult()
		{
			// Arrange
			_go = new GameObject("Popup", typeof(Canvas));
			var popup = _go.AddComponent<Popup>();

			// Act
			var showTask = popup.ShowAsync();
			popup.Ok();
			var result = await showTask;

			// Assert
			Assert.AreEqual(PopupResult.Ok, result);
		}

		[Test]
		[Timeout(5000)]
		public async Task ShowAsync_ThenCancel_ReturnsCancelResult()
		{
			// Arrange
			_go = new GameObject("Popup", typeof(Canvas));
			var popup = _go.AddComponent<Popup>();

			// Act
			var showTask = popup.ShowAsync();
			popup.Cancel();
			var result = await showTask;

			// Assert
			Assert.AreEqual(PopupResult.Cancel, result);
		}

		[Test]
		[Timeout(5000)]
		public async Task ShowAsync_CompletesOnSetResult_EvenThoughGameObjectStaysActive()
		{
			// Regression test: Popup requires a Canvas, so UIViewBehaviour always attaches a
			// CanvasToggleableView to it (see UIViewBehaviour.CreateAppropriateToggleableView), which
			// toggles Canvas.enabled rather than deactivating the GameObject. The previous implementation
			// polled `while (gameObject.activeSelf)`, which stayed true forever for every real Popup and
			// made ShowAsync() hang indefinitely. Using a UniTaskCompletionSource instead fixes this.
			_go = new GameObject("Popup", typeof(Canvas));
			var popup = _go.AddComponent<Popup>();

			// Act
			var showTask = popup.ShowAsync();
			popup.SetResult(PopupResult.Ok);
			var result = await showTask;

			// Assert
			Assert.AreEqual(PopupResult.Ok, result);
			Assert.IsTrue(_go.activeSelf, "Only the Canvas should toggle for a Popup view; the GameObject stays active.");
		}

		[Test]
		public void OnDisable_ResolvesPendingAwaiter()
		{
			// A defensive safety net: if something deactivates the popup (e.g. a parent view being hidden
			// via SetActive) while a ShowAsync() call is pending, bypassing SetResult/Ok/Cancel, the
			// pending awaiter must still resolve instead of hanging.
			//
			// Awake()/OnDisable() are invoked directly via reflection rather than relying on Unity to call
			// them through AddComponent/SetActive: in this Edit Mode test harness Awake was observed to not
			// run automatically (_canvas stayed null), for reasons unrelated to the OnDisable behavior this
			// test actually cares about -- so both are called explicitly to isolate just that logic.
			_go = new GameObject("Popup", typeof(Canvas));
			var popup = _go.AddComponent<Popup>();
			// A bare Canvas was observed reading its renderMode back as WorldSpace before anything else
			// touches it in this test harness, which isn't what this test is about, so it's set explicitly
			// to avoid tripping ValidateCanvasSetup's warning below.
			_go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
			InvokePrivate(popup, "Awake");

			var showTask = popup.ShowAsync();

			// Act
			InvokePrivate(popup, "OnDisable");

			// Assert
			Assert.AreEqual(UniTaskStatus.Succeeded, showTask.Status);
		}

		[Test]
		public void Awake_WithWorldSpaceCanvas_LogsWarning()
		{
			// Arrange: a Popup's Canvas set to World Space renders at a fixed world-unit size regardless of
			// screen resolution -- the exact "popup is way too big" symptom this validation targets.
			_go = new GameObject("Popup", typeof(Canvas), typeof(CanvasScaler));
			_go.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
			var popup = _go.AddComponent<Popup>();

			// Act & Assert
			LogAssert.Expect(LogType.Warning, new Regex("World Space"));
			InvokePrivate(popup, "Awake");
		}

		[Test]
		public void Awake_WithProperlyConfiguredCanvas_DoesNotLogAnyWarning()
		{
			// Arrange: renderMode is set explicitly rather than relied upon as a default -- a bare Canvas
			// was observed reading back as WorldSpace before anything else touches it in this test harness,
			// which would otherwise trip the warning below. Unlike RenderMode, CanvasScaler.uiScaleMode is no
			// longer validated here at all: UIService.SpawnView now forces every spawned view's CanvasScaler
			// to match the app-wide UISettings.Canvas section on every spawn (see Tests_UIService), which
			// makes a per-prefab "is it configured right" warning both redundant and potentially misleading
			// (it would fire based on the prefab's saved value even though the runtime result is corrected).
			_go = new GameObject("Popup", typeof(Canvas), typeof(CanvasScaler));
			_go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
			var popup = _go.AddComponent<Popup>();

			// Act (Unity Test Framework fails the test on any unexpected Warning/Error log, so no explicit
			// assertion is needed here beyond letting Awake run without an [Expect]).
			InvokePrivate(popup, "Awake");
		}

		[Test]
		public void OnValidate_WithRegisteredUISettings_AppliesCanvasSectionToTheScaler()
		{
			// Arrange: OnValidate keeps the prefab's own CanvasScaler in sync with the app-wide
			// UISettings.Canvas section at edit time, so a popup previews correctly without Play Mode.
			const string tempFolder = "Assets/__PopupOnValidateTests__";
			if (!AssetDatabase.IsValidFolder(tempFolder))
				AssetDatabase.CreateFolder("Assets", "__PopupOnValidateTests__");

			var settings = ScriptableObject.CreateInstance<UISettings>();
			settings.Canvas.UiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			settings.Canvas.ReferenceResolution = new Vector2(1280, 720);
			AssetDatabase.CreateAsset(settings, $"{tempFolder}/UISettings.asset");
			AssetBank.InitializeForTests(new[] { AssetBaseRef.FromAsset(settings) });

			_go = new GameObject("Popup", typeof(Canvas), typeof(CanvasScaler));
			_go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
			_go.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
			var popup = _go.AddComponent<Popup>();

			// Act
			InvokePrivate(popup, "OnValidate");

			// Assert: whatever the prefab had saved is irrelevant -- the app-wide settings always win.
			var scaler = _go.GetComponent<CanvasScaler>();
			Assert.AreEqual(CanvasScaler.ScaleMode.ScaleWithScreenSize, scaler.uiScaleMode);
			Assert.AreEqual(new Vector2(1280, 720), scaler.referenceResolution);

			// Cleanup
			AssetDatabase.DeleteAsset(tempFolder);
		}

		[Test]
		public void OnValidate_WithNoRegisteredUISettings_DoesNotThrow()
		{
			// Arrange: no UISettings asset registered yet (e.g. a fresh project) must not spam warnings or
			// throw every time a popup is touched in the editor.
			AssetBank.InitializeForTests(Array.Empty<AssetBaseRef>());
			_go = new GameObject("Popup", typeof(Canvas), typeof(CanvasScaler));
			_go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
			var popup = _go.AddComponent<Popup>();

			// Act & Assert
			Assert.DoesNotThrow(() => InvokePrivate(popup, "OnValidate"));
		}

		private static void InvokePrivate(object target, string methodName)
		{
			typeof(Popup)
				.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance)
				.Invoke(target, null);
		}
	}
}
