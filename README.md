# MqttClient

Generic MQTT Client for receiving messages published by the MQTT Broker. Full configuration is pulled from the database, including Broker's URI address and port, credentials, interval of the service pushing to database, topic filters, and topics listened to.

---

## App configuration example

> appsettings.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "MqttClient": {
    "ConfigCode": "mqtt.client.config",
    "ReceiveDataProcedure": "dbo.sp_receive_mqtt_data"
  },
  "ConnectionStrings": {
    "PaulosDb": "Data Source=127.0.0.1;Initial Catalog=dbname;User ID=username;Password=passwd;Application Name=mqttclient;Connection Timeout=5;Timeout=5;"
  }
}
```


## Database configuration example

> exec dbo.sp_select_config_by_code @Code = 'mqtt.client.config';
```json
{
  "PushDataInterval": 1000,
  "URI": "127.0.0.1",
  "Port": 1883,
  "Username": null,
  "Password": null,
  "Secure": 0,
  "Filters": [
    "mqtt/topics/#"
  ],
  "Topics": [
    "mqtt/topics/topic1",
    "mqtt/topics/topic2",
    "mqtt/topics/topic3",
    "mqtt/topics/topic4"
  ]
}
```


---

## Deployment:

1. Build the application with the following command:
```
dotnet publish -c release -r linux-x64 --no-self-contained -p:PublishSingleFile=true
```
2. Copy the `bin/release/net5.0/linux-x64/publish` directory to the server directory where the app will be hosted (under a descriptive name).
3. Configure the `appsettings.json` file.
4. Put the `mqttclient.service` file in `/etc/systemd/system/` directory (and set the appropriate paths if needed).
5. (Re)Start the application service using the following command:
```
systemctl (re)start mqttclient
```


---

## TODO

* Add support for configuring wildcarded filters.
* Add support for configuring topics excluded from wildcarded filters.
* Add support for configuring topics bypassed by excluded filters.


---

## Changelog

* v0.1.0.0
    * Added project files.
    * Filters are the same as topics (without wildcards).

