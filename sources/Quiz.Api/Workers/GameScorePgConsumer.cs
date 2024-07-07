using Confluent.Kafka;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Quiz.Api.Configurations;
using Quiz.Api.Models;
using Quiz.Api.Repositories;

namespace Quiz.Api.Workers
{
    public class GameScorePgConsumer: IHostedService
    {
        private IConsumer<string, string> _kafkaConsumer;
        private readonly ILogger<GameScorePgConsumer> _logger;
        private readonly IGameScoreRepository _gameScoreRepo;
        private readonly KafkaConfiguration _kafkaConfiguration;

        public GameScorePgConsumer(ILogger<GameScorePgConsumer> logger, IOptions<KafkaConfiguration> kafkaConfigurationOptions, IGameScoreRepository gameScoreRepo)
        {
            _logger = logger ?? throw new ArgumentException(nameof(logger));
            _kafkaConfiguration = kafkaConfigurationOptions?.Value ?? throw new ArgumentException(nameof(kafkaConfigurationOptions));
            _gameScoreRepo = gameScoreRepo;
            Init();
        }

        public async Task ConsumeAsync()
        {
            _kafkaConsumer.Subscribe("scoring_topic");

            while (true)
            {
                var result = _kafkaConsumer.Consume();
                var scoreDto = System.Text.Json.JsonSerializer.Deserialize<GameScore>(result.Message.Value);

                _logger.LogDebug("Record consumed, userId: {UserId}, score: {Score}", result.Message.Key, scoreDto.Score);

                var gameScore = new GameScore
                {
                    UserId = scoreDto.UserId,
                    Score = scoreDto.Score,
                    CreatedAt = scoreDto.CreatedAt
                };

                await _gameScoreRepo.SaveAsync(gameScore);

                _logger.LogDebug("Record saved to DB, key: {Key}", result.Message.Key);
            }
        }

        private void Init()
        {

            var config = new ConsumerConfig()
            {
                BootstrapServers = _kafkaConfiguration.Brokers,

                //SslCaLocation = pemFileWithKey,
                //SslCertificateLocation = pemFileWithKey,
                //SslKeyLocation = pemFileWithKey,

                //Debug = "broker,topic,msg",

                GroupId = _kafkaConfiguration.ConsumerGroup,
                SecurityProtocol = SecurityProtocol.Plaintext,
                EnableAutoCommit = false,
                StatisticsIntervalMs = 5000,
                SessionTimeoutMs = 6000,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnablePartitionEof = true
            };

            _kafkaConsumer = new ConsumerBuilder<string, string>(config).SetStatisticsHandler((_, kafkaStatistics) => LogKafkaStats(kafkaStatistics)).
                SetErrorHandler((_, e) => LogKafkaError(e)).Build();
        }

        private void LogKafkaStats(string kafkaStatistics)
        {
            var stats = JsonConvert.DeserializeObject<KafkaStatistics>(kafkaStatistics);

            if (stats?.topics != null && stats.topics.Count > 0)
            {
                foreach (var topic in stats.topics)
                {
                    foreach (var partition in topic.Value.Partitions)
                    {
                        Task.Run(() =>
                        {
                            var logMessage = $"FxRates:KafkaStats Topic: {topic.Key} Partition: {partition.Key} PartitionConsumerLag: {partition.Value.ConsumerLag}";
                            _logger.LogInformation(logMessage);
                        });
                    }
                }
            }
        }

        private void LogKafkaError(Error ex)
        {
            Task.Run(() =>
            {
                var error = $"Kafka Exception: ErrorCode:[{ex.Code}] Reason:[{ex.Reason}] Message:[{ex.ToString()}]";
                _logger.LogError(error);
            });
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await ConsumeAsync();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
