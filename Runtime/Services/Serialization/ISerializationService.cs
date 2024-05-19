//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

namespace BlueCheese.App.Services
{
    public interface ISerializationService
    {
        string JsonSerialize<T>(T obj);
        T JsonDeserialize<T>(string str);
    }
}
