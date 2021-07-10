using System.Collections.Generic;

namespace MqttClient.Models
{
    public class MqttTopicData
    {
        public string Topic { get; set; }
        public IList<MqttMessage> Values { get; set; }
    }
}