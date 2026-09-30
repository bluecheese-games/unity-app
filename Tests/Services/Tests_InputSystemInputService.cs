#if ENABLE_INPUT_SYSTEM
using BlueCheese.App;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace BlueCheese.Tests.Services
{
	[TestFixture]
	public class Tests_InputSystemInputService : InputTestFixture
	{
		private InputSystemInputService _inputService;

		public override void Setup()
		{
			base.Setup();

			InputSystem.AddDevice<Mouse>();
			_inputService = new InputSystemInputService();
		}

		[Test]
		public void GetPointerPosition_AfterMouseMoved_ReturnsMousePosition()
		{
			// Arrange
			var expectedPosition = new Vector2(123, 234);

			// Act
			InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = expectedPosition });
			InputSystem.Update();
			Vector2 result = _inputService.GetPointerPosition();

			// Assert
			Assert.AreEqual(expectedPosition, result);
		}

		[Test]
		public void IsPointerPressed_AfterMouseButtonPressed_ReturnsTrue()
		{
			// Arrange & Act
			Press(Mouse.current.leftButton);
			bool result = _inputService.IsPointerPressed();

			// Assert
			Assert.IsTrue(result);
		}

		[Test]
		public void IsPointerPressed_WithNoInput_ReturnsFalse()
		{
			// Act
			bool result = _inputService.IsPointerPressed();

			// Assert
			Assert.IsFalse(result);
		}
	}
}
#endif
