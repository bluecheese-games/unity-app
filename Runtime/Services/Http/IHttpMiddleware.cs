//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

namespace BlueCheese.App
{
    public interface IHttpMiddleware
    {
        void HandleRequest(IHttpRequest request);
        void HandleResponse(IHttpResponse response);
    }
}
