using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MqttClient.Models;
using MQTTnet;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MqttClient
{
    public class MqttClient
    {
        private readonly ILogger<MqttClient> _logger;
        private readonly IConfiguration _config;
        private readonly IManagedMqttClient _client;

        private readonly MqttDataStore _dataStore;

        public bool IsAlive { get; set; }
        public bool IsStarted => _client.IsStarted;
        public bool IsConnected => _client.IsConnected;

        public MqttClient(
            ILogger<MqttClient> logger,
            IConfiguration config
            )
        {
            _logger = logger;
            _config = config;

            _dataStore = new MqttDataStore
            {
                DateFrom = null,
                DateTo = null,
                Data = new List<MqttTopicData>()
            };

            IsAlive = false;

            _client = new MqttFactory().CreateManagedMqttClient();

            _client.UseConnectedHandler(e => OnConnected(e));

            _client.UseDisconnectedHandler(e => OnDisconnected(e));

            _client.UseApplicationMessageReceivedHandler(e => OnMessageReceived(e));
        }

        public async Task ConnectAsync(string uri, string user, string password, int port, bool secure)
        {
            string clientId = Guid.NewGuid().ToString();

            var messageBuilder = string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password)
                ? new MqttClientOptionsBuilder()
                    .WithClientId(clientId)
                    .WithTcpServer(uri, port)
                    .WithCleanSession()
                : new MqttClientOptionsBuilder()
                    .WithClientId(clientId)
                    .WithCredentials(user, password)
                    .WithTcpServer(uri, port)
                    .WithCleanSession();

            var options = secure
                ? messageBuilder
                    .WithTls()
                    .Build()
                : messageBuilder
                    .Build();

            var managedOptions = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .WithClientOptions(options)
                .Build();

            await _client.StartAsync(managedOptions);
        }

        public async Task PublishAsync(string topic, string payload, bool retainFlag = true, int qos = 1)
        {
            await _client.PublishAsync(new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel((MQTTnet.Protocol.MqttQualityOfServiceLevel)qos)
                .WithRetainFlag(retainFlag)
                .Build());
        }

        public async Task SubscribeAsync(string topic, int qos = 1)
        {
            await _client.SubscribeAsync(topic, (MQTTnet.Protocol.MqttQualityOfServiceLevel)qos);
        }

        public void SetupMqttSchema(string[] topics)
        {
            _logger.LogDebug("Setting up an MQTT schema...");

            foreach (string topic in topics)
                _dataStore.Data.Add(new MqttTopicData { Topic = topic, Values = new List<MqttMessage>() });

            _logger.LogDebug("All topics added to the schema.");
            _logger.LogTrace($"Topics added: {string.Join(", ", _dataStore.Topics)}");
        }

        public async Task PushDataToDatabase(DataAccess db)
        {
            _logger.LogDebug("Pushing cached data to database...");

            MqttDataStore storeState = _dataStore.GetCurrentState(out var stateDt);
            _dataStore.DateFrom = stateDt;

            try
            {
                string dataJson = JsonConvert.SerializeObject(storeState);
                if (await db.InsertData(dataJson))
                {
                    if (!_config["Logging:LogLevel:Default"].ToString().Equals("Trace"))
                        _logger.LogDebug($"Data from {storeState.DateFrom} to {storeState.DateTo} pushed to database.");
                    _logger.LogTrace($"Data ({dataJson}) pushed to database.");
                }
                else
                    _logger.LogWarning("Stored procedure failed to save data in database (InsertData returned false).");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Inserting data failed with exception: {0}", ex.Message);
            }

            _logger.LogDebug("Finished pushing cached data.");
        }

        public void Teardown()
        {
            _client.Dispose();
        }

        private async void OnConnected(MqttClientConnectedEventArgs e)
        {
            _logger.LogInformation("Connected successfully with MQTT Broker.");

            await _client.SubscribeAsync(
                (_dataStore.Topics as List<string>)
                .ConvertAll(
                    t => new MqttTopicFilter
                    {
                        Topic = t,
                        QualityOfServiceLevel = MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce
                    })
                );
        }

        private void OnDisconnected(MqttClientDisconnectedEventArgs e)
        {
            _logger.LogInformation("Disconnected from MQTT Broker.");
        }

        private void OnMessageReceived(MqttApplicationMessageReceivedEventArgs e)
        {
            try
            {
                string topic = e.ApplicationMessage.Topic;

                if (string.IsNullOrWhiteSpace(topic))
                    throw new Exception("Topic is null.");

                string payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                _logger.LogTrace($"Topic: {topic}. Message Received: {payload}");

                if (!_dataStore.AddTopicValue(topic, payload))
                    throw new Exception("Topic not found.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Receiving message failed with exception: {ex.Message}");
            }
        }
    }
}
