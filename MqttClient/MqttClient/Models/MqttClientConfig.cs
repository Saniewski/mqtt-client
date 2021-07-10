namespace MqttClient.Models
{
    public class MqttClientConfig
    {
        public int? StepLengthMili { get; set; }
        public string URI { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public int? Port { get; set; }
        public bool? Secure { get; set; }
        public string[] Topics { get; set; }

        public override string ToString()
        {
            return $"StepLengthMili: {StepLengthMili}, URI: {URI}, User: {User}, Password: {Password}, Port: {Port}, Secure: {Secure}";
        }
    }
}
