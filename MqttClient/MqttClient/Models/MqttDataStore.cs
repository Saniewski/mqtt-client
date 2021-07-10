using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace MqttClient.Models
{
    public class MqttDataStore
    {
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public IList<MqttTopicData> Data { get; set; }

        [JsonIgnore]
        public IList<string> Topics => (Data as List<MqttTopicData>).ConvertAll(mtd => mtd.Topic);

        public bool AddTopicValue(string topic, string value)
        {
            bool found = false;
            DateTime receivedAt = DateTime.Now;
            
            for (int i = 0; i < Data.Count && !found; i++)
                if (Data[i].Topic.Equals(topic))
                {
                    Data[i].Values.Add(new MqttMessage { ReceivedAt = receivedAt, Value = value });
                    if (DateFrom is null)
                        DateFrom = receivedAt;
                    found = true;
                }
            
            return found;
        }

        public MqttDataStore GetCurrentState(out DateTime stateDt)
        {
            stateDt = DateTime.Now;

            return new MqttDataStore
            {
                DateFrom = DateFrom,
                DateTo = stateDt,
                Data = (Data as List<MqttTopicData>)
                    .ConvertAll(mtd => new MqttTopicData
                    {
                        Topic = mtd.Topic,
                        Values = (mtd.Values as List<MqttMessage>)
                            .ConvertAll(msg => new MqttMessage
                            {
                                ReceivedAt = msg.ReceivedAt,
                                Value = msg.Value
                            })
                    })
            };
        }
    }
}
