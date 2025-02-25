//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

namespace BlueCheese.App
{
    public interface IJsonService
    {
        string Serialize<T>(T obj);
        T Deserialize<T>(string str);
    }
}
