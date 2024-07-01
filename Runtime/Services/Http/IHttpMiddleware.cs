//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

namespace BlueCheese.App.Services
{
    public interface IHttpMiddleware
    {
        void HandleRequest(IHttpRequest request);
        void HandleResponse(IHttpResponse response);
    }
}
