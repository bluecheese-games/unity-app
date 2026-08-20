//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using BlueCheese.App;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace BlueCheese.Tests.Services
{
	[TestFixture]
	public class Tests_UISettings
	{
		private UISettings _settings;
		private GameObject _go;

		[SetUp]
		public void SetUp()
		{
			_settings = ScriptableObject.CreateInstance<UISettings>();
			_go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler));
		}

		[TearDown]
		public void TearDown()
		{
			Object.DestroyImmediate(_settings);
			Object.DestroyImmediate(_go);
		}

		[Test]
		public void Canvas_ApplyTo_CopiesEveryConfiguredValueOntoTheScaler()
		{
			// Arrange
			_settings.Canvas.UiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			_settings.Canvas.ReferenceResolution = new Vector2(2560, 1440);
			_settings.Canvas.ScreenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
			_settings.Canvas.MatchWidthOrHeight = 0.75f;
			_settings.Canvas.ReferencePixelsPerUnit = 150f;
			var scaler = _go.GetComponent<CanvasScaler>();

			// Act
			_settings.Canvas.ApplyTo(scaler);

			// Assert
			Assert.AreEqual(CanvasScaler.ScaleMode.ScaleWithScreenSize, scaler.uiScaleMode);
			Assert.AreEqual(new Vector2(2560, 1440), scaler.referenceResolution);
			Assert.AreEqual(CanvasScaler.ScreenMatchMode.MatchWidthOrHeight, scaler.screenMatchMode);
			Assert.AreEqual(0.75f, scaler.matchWidthOrHeight);
			Assert.AreEqual(150f, scaler.referencePixelsPerUnit);
		}

		[Test]
		public void Canvas_ApplyTo_OverridesWhateverWasPreviouslySetOnTheScaler()
		{
			// Arrange: simulates a prefab that shipped with a different, drifted configuration.
			var scaler = _go.GetComponent<CanvasScaler>();
			scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
			scaler.referenceResolution = new Vector2(800, 600);
			_settings.Canvas.UiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			_settings.Canvas.ReferenceResolution = new Vector2(1920, 1080);

			// Act
			_settings.Canvas.ApplyTo(scaler);

			// Assert
			Assert.AreEqual(CanvasScaler.ScaleMode.ScaleWithScreenSize, scaler.uiScaleMode);
			Assert.AreEqual(new Vector2(1920, 1080), scaler.referenceResolution);
		}

		[Test]
		public void Canvas_ApplyTo_WithNullScaler_DoesNotThrow()
		{
			// Act & Assert
			Assert.DoesNotThrow(() => _settings.Canvas.ApplyTo(null));
		}
	}
}
