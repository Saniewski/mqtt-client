using System;

namespace MqttClient.Models
{
    public class MqttMessage
    {
        public string Value { get; set; }
        public DateTime ReceivedAt { get; set; }
    }
}
