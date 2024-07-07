using Confluent.Kafka;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Quiz.Api.Configurations;
using Quiz.Api.Models;
using StackExchange.Redis;

namespace Quiz.Api.Workers
{
    public class GameScoreRedisConsumer : IHostedService
    {
        private IConsumer<string, string> _kafkaConsumer;
        private readonly IDatabase _redisDb;
        private IProducer<string, string> _kafkaProducer;
        private readonly ILogger<GameScoreRedisConsumer> _logger;
        private readonly KafkaConfiguration _kafkaConfiguration;
        private readonly RedisConfiguration _redisConfiguration;

        public GameScoreRedisConsumer(ILogger<GameScoreRedisConsumer> logger, IOptions<KafkaConfiguration> kafkaConfigurationOptions, IOptions<RedisConfiguration> redisConfigurationOptions)
        {
            _kafkaConfiguration = kafkaConfigurationOptions?.Value ?? throw new ArgumentException(nameof(kafkaConfigurationOptions));
            _redisConfiguration = redisConfigurationOptions?.Value ?? throw new ArgumentException(nameof(redisConfigurationOptions));

            ConfigurationOptions options = new ConfigurationOptions
            {
                //list of available nodes of the cluster along with the endpoint port.
                EndPoints = {
                    { _redisConfiguration.HostName, _redisConfiguration.Port}
                },
                Password = _redisConfiguration.Password
            };
            var redis = ConnectionMultiplexer.Connect(options);
            _redisDb = redis.GetDatabase();
            _logger = logger ?? throw new ArgumentException(nameof(logger));
            
            InitProducer();
            InitConsumer();
        }

        public async Task ConsumeAsync()
        {
            _kafkaConsumer.Subscribe("scoring_topic");

            while (true)
            {
                var result = _kafkaConsumer.Consume();
                var gameScore = System.Text.Json.JsonSerializer.Deserialize<GameScore>(result.Message.Value);
                var score = gameScore.Score;

                var leaderboardCollectionKey = "leader_board"; // Replace with actual key from config

                _logger.LogDebug("Record consumed, userId: {UserId}, score: {Score}", gameScore.UserId, score);

                await _redisDb.SortedSetIncrementAsync(leaderboardCollectionKey, gameScore.UserId.ToString(), score);

                _logger.LogDebug("Record with userId: {UserId}, score: {Score} added to Redis set", gameScore.UserId, score);

                var leaderboardChangeDto = new LeaderBoardChange
                {
                    RecordTimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };

                var kafkaMessage = System.Text.Json.JsonSerializer.Serialize(leaderboardChangeDto);
                await _kafkaProducer.ProduceAsync("leaderboard_change", new Message<string, string> { Value = kafkaMessage });

                _logger.LogDebug("Record with userId: {UserId}, score: {Score} change timestamp published to Kafka", gameScore.UserId, score);
            }
        }

        private void InitProducer()
        {

            var config = new ProducerConfig()
            {
                BootstrapServers = _kafkaConfiguration.Brokers,
                ClientId = "Kafka.Dotnet",

                //SslCaLocation = pemFileWithKey,
                //SslCertificateLocation = pemFileWithKey,
                //SslKeyLocation = pemFileWithKey,

                //Debug = "broker,topic,msg",

                SecurityProtocol = SecurityProtocol.Plaintext,
                EnableDeliveryReports = false,
                QueueBufferingMaxMessages = 10000000,
                QueueBufferingMaxKbytes = 100000000,
                BatchNumMessages = 500,
                Acks = Acks.None,
                DeliveryReportFields = "none"
            };

            _kafkaProducer = new ProducerBuilder<string, string>(config).Build();
        }

        private void InitConsumer()
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
