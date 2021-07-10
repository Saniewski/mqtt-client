using MqttClient.Models;
using Newtonsoft.Json;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace MqttClient
{
    public class DataAccess
    {
        private readonly string _connectionString;
        private readonly string _configCode;
        private readonly string _receiveDataProcedure;

        public DataAccess(string connectionString, string configCode, string receiveDataProcedure)
        {
            _connectionString = connectionString;
            _configCode = configCode;
            _receiveDataProcedure = receiveDataProcedure;
        }

        public async Task<MqttClientConfig> GetConfigFromDb()
        {
            using SqlConnection connection = new(_connectionString);
            using SqlCommand command = new()
            {
                Connection = connection,
                CommandType = CommandType.StoredProcedure,
                CommandText = "dbo.sp_select_config_by_code"
            };

            command.Parameters.Add(new SqlParameter("@Code", _configCode));

            connection.Open();

            string configJson = (await command.ExecuteScalarAsync())?.ToString();

            if (string.IsNullOrWhiteSpace(configJson))
                return null;

            return JsonConvert.DeserializeObject<MqttClientConfig>(configJson);
        }

        public async Task<bool> InsertData(string json)
        {
            using SqlConnection connection = new(_connectionString);
            using SqlCommand command = new()
            {
                Connection = connection,
                CommandType = CommandType.StoredProcedure,
                CommandText = _receiveDataProcedure
            };
            command.Parameters.Add(new SqlParameter("@json", json));

            connection.Open();

            return await command.ExecuteNonQueryAsync() == -1;
        }
    }
}
