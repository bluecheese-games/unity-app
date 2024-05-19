//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

namespace BlueCheese.App.Services
{
    public interface ILocalStorageService
    {
        /// <summary>
        /// Writes a value in the local storage.
        /// </summary>
        /// <param name="key">The key to store the value.</param>
        /// <param name="value">The value to store.</param>
        void WriteValue<T>(string key, T value = default);

        /// <summary>
        /// Reads a value from local storage.
        /// </summary>
        /// <param name="key">The key to retreive the value.</param>
        /// <param name="defaultValue">A default value if the key does not exist.</param>
        /// <returns></returns>
        T ReadValue<T>(string key, T defaultValue = default);
    }
}
