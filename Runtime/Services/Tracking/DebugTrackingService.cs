//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

namespace BlueCheese.App
{
    public class DebugTrackingService : ITrackingService
    {
        private readonly ILogger<DebugTrackingService> _logger;

        public DebugTrackingService(ILogger<DebugTrackingService> logger)
        {
            _logger = logger;
        }

        public void Track(ITrackingService.Event evt) => _logger.LogInfo(evt);
    }
}
