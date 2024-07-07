using Confluent.Kafka;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Quiz.Api.Configurations;
using Quiz.Api.Models;

namespace Quiz.Api.Services
{
    public class LeaderBoardService
    {
        private IConsumer<string, string> _kafkaConsumer;
        private readonly WebSocketServer _webSocketServer;
        private readonly ILogger<LeaderBoardService> _logger;
        private readonly KafkaConfiguration _kafkaConfiguration;

        public LeaderBoardService(WebSocketServer webSocketServer, ILogger<LeaderBoardService> logger, IOptions<KafkaConfiguration> kafkaConfigurationOptions)
        {            
            _webSocketServer = webSocketServer;
            _logger = logger ?? throw new ArgumentException(nameof(logger));
            _kafkaConfiguration = kafkaConfigurationOptions?.Value ?? throw new ArgumentException(nameof(kafkaConfigurationOptions));

            Init();
        }

        public async Task ConsumeAsync()
        {
            _kafkaConsumer.Subscribe("leaderboard_topic");

            while (true)
            {
                var result = _kafkaConsumer.Consume();
                var leaderboard = System.Text.Json.JsonSerializer.Deserialize<LeaderBoard>(result.Message.Value);

                _logger.LogDebug("Leaderboard record consumed, timestamp: {Timestamp}", leaderboard.LastModifyTimestamp);

                var message = System.Text.Json.JsonSerializer.Serialize(leaderboard);
                await _webSocketServer.BroadcastAsync(message);
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
    }
}
