﻿using Newtonsoft.Json;

namespace Quiz.Api.Models
{
    public class TopicData
    {
        [JsonProperty("topic")]
        public string Topic { get; set; }

        [JsonProperty("partitions")]
        public IReadOnlyDictionary<string, PartitionData> Partitions { get; set; }
    }
}
