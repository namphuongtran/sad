using Confluent.Kafka;
using Microsoft.Extensions.Options;
using Quiz.Api.Configurations;
using Quiz.Api.Models;
using System.Text.Json;


namespace Quiz.Api.Services
{
    public class GameScoreService
    {
        private IProducer<string, string> _kafkaProducer;
        private readonly ILogger<GameScoreService> _logger;
        private Timer _timer;
        private readonly KafkaConfiguration _kafkaConfiguration;

        public GameScoreService(ILogger<GameScoreService> logger, IOptions<KafkaConfiguration> kafkaConfigurationOptions)
        {
            _logger = logger ?? throw new ArgumentException(nameof(logger));
            _kafkaConfiguration = kafkaConfigurationOptions?.Value ?? throw new ArgumentException(nameof(kafkaConfigurationOptions));
            Init();

        }        

        private void PublishRandomGameScores(object state)
        {
            int batchSize = 2000;
            for (int i = 0; i < batchSize; i++)
            {
                PublishGameScoreAsync().Wait();
            }
        }

        public async Task PublishGameScoreAsync(string message = null)
        {
            _timer = new Timer(PublishRandomGameScores, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
            var gameScoreDto = message == null ? BuildGameScore() : JsonSerializer.Deserialize<GameScore>(message);
            var gameScoreTopic = "scoring_topic";

            var kafkaMessage = JsonSerializer.Serialize(gameScoreDto);
            await _kafkaProducer.ProduceAsync(gameScoreTopic, new Message<string, string> { Key = gameScoreDto.UserId.ToString(), Value = kafkaMessage });

            _logger.LogDebug("Game score of user {UserId} was published to Kafka topic: {Topic}", gameScoreDto.UserId, gameScoreTopic);
        }

        private GameScore BuildGameScore()
        {
            var random = new Random();
            return new GameScore
            {
                Score = random.Next(1, 101),
                UserId = random.Next(1, 1000000),
                CreatedAt = DateTime.UtcNow
            };
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
