namespace IOC.Core.Kafka;

/// <summary>
/// Builder để đăng ký Kafka topics và consumers cho từng plugin.
/// Convention topic: ioc.{domain}.{event}
/// </summary>
public interface IKafkaBuilder
{
    /// <summary>Đăng ký topic producer</summary>
    IKafkaBuilder AddTopic(string topicName, int partitions = 1, short replicationFactor = 1);

    /// <summary>Đăng ký consumer với handler</summary>
    IKafkaBuilder AddConsumer<TMessage>(string topicName, Func<TMessage, CancellationToken, Task> handler)
        where TMessage : class;
}
