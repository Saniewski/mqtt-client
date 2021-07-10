using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MqttClient.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MqttClient
{
    public class MqttWorker : BackgroundService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<MqttWorker> _logger;
        private readonly MqttClient _mqttClient;

        private DataAccess _dataAccess;
        private MqttClientConfig _dbConfig;

        public MqttWorker(
            IConfiguration config,
            ILogger<MqttWorker> logger,
            MqttClient mqttClient
            )
        {
            _config = config;
            _logger = logger;
            _mqttClient = mqttClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!ValidateAppsettings())
                return;

            _dataAccess = new DataAccess(
                _config.GetConnectionString("PaulosDb"),
                _config["MqttClient:ConfigCode"],
                _config["MqttClient:ReceiveDataProcedure"]
                );

            while (_dbConfig is null)
                try
                {
                    _logger.LogDebug("Loading configuration from database...");
                    _dbConfig = await _dataAccess.GetConfigFromDb();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Loading configuration from database failed with exception: {ex.Message}");
                    _logger.LogDebug("Reloading configuration from database in 5 seconds...");
                    await Task.Delay(5000, stoppingToken);
                }

            if (!ValidateDatabaseConfig())
                return;

            _logger.LogDebug($"Loaded database configuration - {_dbConfig}");

            _mqttClient.SetupMqttSchema(_dbConfig.Topics);

            _logger.LogInformation($"Connecting to the MQTT Broker at {_dbConfig.URI}:{_dbConfig.Port}...");

            await _mqttClient.ConnectAsync(_dbConfig.URI, _dbConfig.User, _dbConfig.Password, _dbConfig.Port.Value, _dbConfig.Secure.Value);

            int isAliveCounter = 0;
            string mqttBrokerStatus;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    mqttBrokerStatus = _mqttClient.IsAlive ? "alive" : "dead";

                    if (--isAliveCounter <= 0)
                    {
                        _logger.LogInformation($"MqttBroker at {_dbConfig.URI} is {mqttBrokerStatus}.");
                        _logger.LogInformation(
                            string.Format("MqttClient is currently {0} and {1}.",
                            _mqttClient.IsStarted ? "started" : "stopped",
                            _mqttClient.IsConnected ? "connected" : "disconnected")
                            );
                        isAliveCounter = 10;
                    }

                    if (!_mqttClient.IsStarted)
                    {
                        _logger.LogDebug($"Restarting MqttClient connection to broker at {_dbConfig.URI}...");
                        await _mqttClient.ConnectAsync(_dbConfig.URI, _dbConfig.User, _dbConfig.Password, _dbConfig.Port.Value, _dbConfig.Secure.Value);
                    }

                    await _mqttClient.PushDataToDatabase(_dataAccess);

                    await Task.Delay(_dbConfig.StepLengthMili.Value, stoppingToken);
                }
                catch (TaskCanceledException) { }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"MqttWorker loop failed with exception - message: {ex.Message}");
                    await Task.Delay(_dbConfig.StepLengthMili.Value, stoppingToken);
                }
            }

            _mqttClient.Teardown();
        }

        private bool ValidateAppsettings()
        {
            if (string.IsNullOrWhiteSpace(_config["MqttClient:ConfigCode"]))
            {
                _logger.LogError("Program configuration does not specify \"MqttClient:ConfigCode\".");
                return false;
            }

            if (string.IsNullOrWhiteSpace(_config["MqttClient:ReceiveDataProcedure"]))
            {
                _logger.LogError("Program configuration does not specify \"MqttClient:ReceiveDataProcedure\".");
                return false;
            }

            return true;
        }

        private bool ValidateDatabaseConfig()
        {
            if (string.IsNullOrWhiteSpace(_dbConfig?.URI))
            {
                _logger.LogError("Database configuration does not specify \"URI\".");
                return false;
            }

            if (_dbConfig?.Port is null)
            {
                _logger.LogError("Database configuration does not specify \"Port\".");
                return false;
            }

            if (_dbConfig?.StepLengthMili is null)
            {
                _logger.LogError("Database configuration does not specify \"StepLengthMili\".");
                return false;
            }

            if (_dbConfig?.Topics?.Length < 1)
            {
                _logger.LogError("Database configuration does not specify \"Topics\".");
                return false;
            }

            return true;
        }
    }
}
