//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System;

namespace BlueCheese.App.Services
{
    public class CacheEntry
    {
        public string Value { get; private set; }

        public DateTime? ExpirationDate { get; private set; }

        public bool HasExpirationDate => ExpirationDate != null;

        public bool IsExpired => ExpirationDate != null && DateTime.UtcNow > ExpirationDate;

        public TimeSpan ExpiresIn => ExpirationDate != null ? ExpirationDate.Value - DateTime.UtcNow : TimeSpan.Zero;

        private CacheEntry(string value)
        {
            Value = value;
            ExpirationDate = null;
        }

        public CacheEntry WithExpirationTime(TimeSpan time)
        {
            ExpirationDate = DateTime.UtcNow + time;
            return this;
        }

        public CacheEntry WithExpirationDate(DateTime expirationDate)
        {
            ExpirationDate = expirationDate;
            return this;
        }

        public static implicit operator string(CacheEntry entry) => entry.Value;
        public static implicit operator CacheEntry(string value) => new(value);
    }
}
