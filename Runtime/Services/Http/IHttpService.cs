//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System.Threading.Tasks;

namespace BlueCheese.App.Services
{
    public interface IHttpService
    {
        Task<HttpResponse<T>> GetAsync<T>(HttpRequest request);
        Task<HttpResponse<T>> PostAsync<T>(HttpRequest request);
    }
}
