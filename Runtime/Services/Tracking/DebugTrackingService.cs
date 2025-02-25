//
// Copyright (c) 2025 BlueCheese Games All rights reserved
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

        public void Track(in ITrackingService.Event evt) => _logger.LogInfo(evt);
    }
}
