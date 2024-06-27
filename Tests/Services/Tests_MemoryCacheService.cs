//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using NUnit.Framework;
using BlueCheese.App.Services;
using System;

namespace BlueCheese.Tests.Services
{
    public class Tests_MemoryCacheService
    {
        private MemoryCacheService _service;

        [SetUp]
        public void Setup()
        {
            _service = new();
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
            var expirationDate = DateTime.UtcNow.AddDays(1);

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
            var expirationDate = DateTime.UtcNow.AddDays(-1);

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
            var expirationDate = DateTime.UtcNow.AddDays(1);
            var expirationDate2 = DateTime.UtcNow.AddDays(2);

            _service.Set("foo", "bar")
                .WithExpirationDate(expirationDate);

            Assert.That(_service.Get("foo").ExpiresIn.TotalDays, Is.EqualTo(1).Within(0.001d));

            _service.Set("foo", "bar")
                .WithExpirationDate(expirationDate2);

            Assert.That(_service.Get("foo").ExpiresIn.TotalDays, Is.EqualTo(2).Within(0.001d));
        }
    }
}