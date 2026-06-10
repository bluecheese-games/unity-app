//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;

namespace BlueCheese.App
{
    public class CacheEntry
    {
        public string Value { get; private set; }

        public DateTimeOffset? ExpirationDate { get; private set; }

        public bool HasExpirationDate => ExpirationDate != null;

        public bool IsExpired => ExpirationDate != null && DateTimeOffset.UtcNow > ExpirationDate;
        public TimeSpan ExpiresIn => ExpirationDate != null ? ExpirationDate.Value - _getNow() : TimeSpan.Zero;

        private readonly Func<DateTimeOffset> _getNow;

		static public CacheEntry Create(string value, Func<DateTimeOffset> getNow = null) => new(value, getNow);

		private CacheEntry(string value, Func<DateTimeOffset> getNow = null)
        {
            Value = value;
            ExpirationDate = null;
            _getNow = getNow ??= () => DateTimeOffset.UtcNow;
        }

        public CacheEntry WithExpirationTime(TimeSpan time)
        {
            ExpirationDate = _getNow().Add(time);
            return this;
        }

        public CacheEntry WithExpirationDate(DateTimeOffset expirationDate)
        {
            ExpirationDate = expirationDate;
            return this;
        }
    }
}
