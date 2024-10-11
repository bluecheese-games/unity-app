//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System;

namespace BlueCheese.App
{
    public class CacheEntry
    {
        public string Value { get; private set; }

        public DateTime? ExpirationDate { get; private set; }

        public bool HasExpirationDate => ExpirationDate != null;

        public bool IsExpired => ExpirationDate != null && DateTime.UtcNow > ExpirationDate;

        public TimeSpan ExpiresIn => ExpirationDate != null ? ExpirationDate.Value - _getNow() : TimeSpan.Zero;

        private readonly Func<DateTime> _getNow;

		static public CacheEntry Create(string value, Func<DateTime> getNow = null) => new(value, getNow);

		private CacheEntry(string value, Func<DateTime> getNow = null)
        {
            Value = value;
            ExpirationDate = null;
            _getNow = getNow ??= () => DateTime.Now;
        }

        public CacheEntry WithExpirationTime(TimeSpan time)
        {
            ExpirationDate = _getNow().Add(time);
            return this;
        }

        public CacheEntry WithExpirationDate(DateTime expirationDate)
        {
            ExpirationDate = expirationDate;
            return this;
        }
    }
}
