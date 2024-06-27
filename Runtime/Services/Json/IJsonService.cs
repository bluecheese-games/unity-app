//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

namespace BlueCheese.App.Services
{
    public interface IJsonService
    {
        string Serialize<T>(T obj);
        T Deserialize<T>(string str);
    }
}
