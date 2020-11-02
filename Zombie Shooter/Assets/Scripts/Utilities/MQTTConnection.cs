using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Protocol;
using System.Threading;
using System.Threading.Tasks;
using System;
using UnityEngine.Events;
using MQTTnet.Client.Connecting;

public class MQTTConnnection
{
    private IManagedMqttClient client;

    public UnityEvent<GameState> MessageReceived = new UnityEvent<GameState>();
    public MQTTConnnection()
    {
        var factory = new MqttFactory();
        var options = new ManagedMqttClientOptionsBuilder()
            .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
            .WithClientOptions(new MqttClientOptionsBuilder()
                        .WithTcpServer(Constants.MQTT_BROKER_URL)
                        .WithTls()
                        .WithCleanSession()
                        .Build())
            .Build();
        client = factory.CreateManagedMqttClient();

        Debug.Log("MQTT Publisher and Subscriber created");

        var topicFilter = new MqttTopicFilter { Topic = Constants.MQTT_TOPIC };
        client.SubscribeAsync(topicFilter);
        client.StartAsync(options);

        client.UseApplicationMessageReceivedHandler(HandleMessageReceive);

        Debug.Log("MQTT Connected");
    }

    private void HandleMessageReceive(MqttApplicationMessageReceivedEventArgs args)
    {
        GameState state = JsonUtility.FromJson<GameState>(args.ApplicationMessage.ConvertPayloadToString());
        MessageReceived.Invoke(state);
    }

    public void Publish(string data)
    {
        var message = new MqttApplicationMessageBuilder()
            .WithPayload(data)
            .WithTopic(Constants.MQTT_TOPIC)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
            //.WithRetainFlag()
            .Build();

        //Debug.Log($"MQTT Publishing \"{data}\"");
        client.PublishAsync(message);
    }

    public void Stop()
    {
        client.StopAsync();
        Debug.Log("MQTT Disconnected");
    }
}
