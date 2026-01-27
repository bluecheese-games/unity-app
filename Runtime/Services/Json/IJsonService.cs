//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

namespace BlueCheese.App
{
    public interface IJsonService
    {
        string Serialize<T>(T obj);
        T Deserialize<T>(string str);
    }
}
