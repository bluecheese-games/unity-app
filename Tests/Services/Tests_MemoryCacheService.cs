//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using BlueCheese.App;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace BlueCheese.Tests.Services
{
	public class Tests_MemoryCacheService
	{
		private MemoryCacheService _service;
		private TestClockService _clock;

		[SetUp]
		public void Setup()
		{
			_clock = new TestClockService();
			_service = new(_clock);
		}

		[Test]
		public void Test_Emptyness()
		{
			Assert.That(_service.GetEntries().Count, Is.EqualTo(0));
		}

		[Test]
		public void Test_Not_Exists()
		{
			Assert.That(_service.Exists("foo"), Is.False);
		}

		[Test]
		public void Test_Get()
		{
			_service.Set("foo", "bar");

			Assert.That(_service.Get("foo").Value, Is.EqualTo("bar"));
		}

		[Test]
		public void Test_GetOrCreate()
		{
			Assert.That(_service.GetOrCreate("foo", () => "bar").Value, Is.EqualTo("bar"));
		}

		[Test]
		public void Test_Set()
		{
			_service.Set("foo", "bar");

			Assert.That(_service.Exists("foo"), Is.True);
		}

		[Test]
		public void Test_Set_WithExpirationDate()
		{
			var expirationDate = DateTime.Now.AddDays(1);

			_service.Set("foo", "bar")
				.WithExpirationDate(expirationDate);

			var entry = _service.Get("foo");

			Assert.That(entry, Is.Not.Null);
			Assert.That(entry.ExpirationDate, Is.EqualTo(expirationDate));
			Assert.That(entry.IsExpired, Is.False);
			Assert.That(entry.HasExpirationDate, Is.True);
		}

		[Test]
		public void Test_Set_WithExpirationDate_Expired()
		{
			var expirationDate = DateTime.Now.AddDays(-1);

			_service.Set("foo", "bar")
				.WithExpirationDate(expirationDate);

			var entry = _service.Get("foo");

			Assert.That(entry, Is.Null);
		}

		[Test]
		public void Test_Set_WithExpirationTime()
		{
			var expirationTime = TimeSpan.FromDays(1);

			_service.Set("foo", "bar")
				.WithExpirationTime(expirationTime);

			var entry = _service.Get("foo");

			Assert.That(entry, Is.Not.Null);
			Assert.That(entry.ExpiresIn, Is.EqualTo(expirationTime).Within(0.001d));
			Assert.That(entry.IsExpired, Is.False);
			Assert.That(entry.HasExpirationDate, Is.True);
		}

		[Test]
		public void Test_Set_WithExpirationTime_Expired()
		{
			var expirationTime = TimeSpan.FromDays(-1);

			_service.Set("foo", "bar")
				.WithExpirationTime(expirationTime);

			var entry = _service.Get("foo");

			Assert.That(entry, Is.Null);
		}

		[Test]
		public void Test_Change_ExpirationDate()
		{
			var expirationDate = DateTime.Now.AddDays(1);
			var expirationDate2 = DateTime.Now.AddDays(2);

			_service.Set("foo", "bar")
				.WithExpirationDate(expirationDate);

			Assert.That(_service.Get("foo").ExpiresIn.TotalDays, Is.EqualTo(1).Within(0.001d));

			_service.Set("foo", "bar")
				.WithExpirationDate(expirationDate2);

			Assert.That(_service.Get("foo").ExpiresIn.TotalDays, Is.EqualTo(2).Within(0.001d));
		}

		private class TestClockService : IClockService
		{
			public DateTime Now => _now;

			public event TickEventHandler OnTick;
			public event Action OnTickSecond;
			public event AsyncTickEventHandler OnTickAsync;
			public event AsyncTickSecondEventHandler OnTickSecondAsync;

			private DateTime _now = DateTime.Now;

			event TickSecondEventHandler IClockService.OnTickSecond
			{
				add
				{
					throw new NotImplementedException();
				}

				remove
				{
					throw new NotImplementedException();
				}
			}

			public void Initialize() { }

			public Task InvokeAsync(Action action, float delay)
			{
				throw new NotImplementedException();
			}

			public Task WaitAsync(float delay)
			{
				throw new NotImplementedException();
			}

			public void SetNow(DateTime now)
			{
				_now = now;
			}

			public void AddTime(TimeSpan time)
			{
				_now += time;
			}
		}
	}
}