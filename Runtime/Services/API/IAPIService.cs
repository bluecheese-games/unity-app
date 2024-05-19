//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System.Threading.Tasks;

namespace BlueCheese.App.Services
{
    public interface IAPIService
    {
        Task<T> GetAsync<T>(string url);
    }
}
