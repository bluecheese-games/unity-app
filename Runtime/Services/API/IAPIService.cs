//
// Copyright (c) 2024 Pierre Martin All rights reserved
//

using System.Threading.Tasks;

namespace BlueCheese.Unity.App.Services
{
    public interface IAPIService
    {
        Task<T> GetAsync<T>(string url);
    }
}
