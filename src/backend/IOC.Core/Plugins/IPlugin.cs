using HotChocolate.Execution.Configuration;
using IOC.Core.Kafka;
using Microsoft.Extensions.DependencyInjection;

namespace IOC.Core.Plugins;

/// <summary>
/// Contract cốt lõi mà mọi IOC Plugin phải implement.
/// Mỗi plugin đăng ký services, GraphQL schema và Kafka topics của mình.
/// </summary>
public interface IPlugin
{
    /// <summary>Tên định danh duy nhất của plugin (lowercase, kebab-case)</summary>
    string Id { get; }

    /// <summary>Tên hiển thị</summary>
    string Name { get; }

    /// <summary>Phiên bản (semver)</summary>
    string Version { get; }

    /// <summary>Mô tả ngắn</summary>
    string Description { get; }

    /// <summary>Đăng ký Dependency Injection services</summary>
    void RegisterServices(IServiceCollection services);

    /// <summary>Đăng ký GraphQL types/queries/mutations/subscriptions</summary>
    void RegisterGraphQL(IRequestExecutorBuilder graphqlBuilder);

    /// <summary>Đăng ký Kafka topics và consumers</summary>
    void RegisterKafka(IKafkaBuilder kafkaBuilder);
}
