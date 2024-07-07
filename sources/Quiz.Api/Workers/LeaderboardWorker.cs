using Confluent.Kafka;
using Microsoft.Extensions.Options;
using Quiz.Api.Configurations;
using Quiz.Api.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace Quiz.Api.Workers
{
    public class LeaderboardWorker
    {
        private IConsumer<string, string> _kafkaConsumer;
        private readonly IDatabase _redisDb;
        private IProducer<string, string> _kafkaProducer;
        private readonly ILogger<LeaderboardWorker> _logger;
        private readonly KafkaConfiguration _kafkaConfiguration;
        private readonly RedisConfiguration _redisConfiguration;

        public LeaderboardWorker(ILogger<LeaderboardWorker> logger, IOptions<KafkaConfiguration> kafkaConfigurationOptions, IOptions<RedisConfiguration> redisConfigurationOptions)
        {
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
            _kafkaConfiguration = kafkaConfigurationOptions?.Value ?? throw new ArgumentException(nameof(kafkaConfigurationOptions));
            _redisConfiguration = redisConfigurationOptions?.Value ?? throw new ArgumentException(nameof(redisConfigurationOptions));
            Init();
        }

        public async Task ConsumeAsync()
        {
            _kafkaConsumer.Subscribe("leaderboard_update_topic");

            while (true)
            {
                var result = _kafkaConsumer.Consume();
                var leaderboardChange = JsonSerializer.Deserialize<LeaderBoardChange>(result.Message.Value);

                _logger.LogDebug("Record consumed with timestampMs: {Timestamp}", leaderboardChange.RecordTimestampMs);

                var top10Users = await GetTop10UsersFromRedisAsync();

                var leaderboardUpdate = new LeaderBoard
                {
                    Users = top10Users,
                    LastModifyTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };

                var kafkaMessage = JsonSerializer.Serialize(leaderboardUpdate);
                await _kafkaProducer.ProduceAsync("leaderboard", new Message<string, string> { Value = kafkaMessage });

                _logger.LogDebug("Leaderboard cache updated, updateTimestamp: {Timestamp}", leaderboardUpdate.LastModifyTimestamp);
            }
        }

        private async Task<List<User>> GetTop10UsersFromRedisAsync()
        {
            var leaderboardKey = "leaderboard_topic";
            var leaderboard = await _redisDb.SortedSetRangeByRankWithScoresAsync(leaderboardKey, 0, 9, Order.Descending);

            var users = leaderboard.Select((entry, index) => new User
            {
                Rank = index + 1,
                Score = entry.Score,
                Nickname = GetCachedNicknameByUserId(entry.Element)
            }).ToList();

            return users;
        }

        private string GetCachedNicknameByUserId(string userId)
        {
            return _redisDb.StringGet(userId);
        }

        private void Init()
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
    }
}
