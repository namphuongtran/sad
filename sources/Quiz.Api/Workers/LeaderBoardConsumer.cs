using Quiz.Api.Services;

namespace Quiz.Api.Workers
{
    public class LeaderBoardConsumer : IHostedService
    {
        private readonly LeaderBoardService _consumer;
        private readonly ILogger<LeaderBoardConsumer> _logger;

        public LeaderBoardConsumer(LeaderBoardService consumer, ILogger<LeaderBoardConsumer> logger)
        {
            _consumer = consumer;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting LeaderBoardConsumerHostedService");
            await Task.Factory.StartNew(_consumer.ConsumeAsync, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping LeaderBoardConsumerHostedService");
            return Task.CompletedTask;
        }
    }
}
