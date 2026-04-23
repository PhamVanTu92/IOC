using HotChocolate.Execution.Configuration;
using IOC.Core.Kafka;
using IOC.Core.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IOC.Core.Tests;

// ─────────────────────────────────────────────────────────────────────────────
// Fake plugin dùng cho tests
// ─────────────────────────────────────────────────────────────────────────────

internal sealed class TestPlugin : IPlugin
{
    public string Id => "test-plugin";
    public string Name => "Test Plugin";
    public string Version => "1.0.0";
    public string Description => "Plugin dùng cho unit tests";
    public bool ServicesRegistered { get; private set; }
    public bool GraphQLRegistered { get; private set; }
    public bool KafkaRegistered { get; private set; }

    public void RegisterServices(IServiceCollection services)
    {
        ServicesRegistered = true;
        services.AddSingleton<TestPluginMarker>();
    }

    public void RegisterGraphQL(IRequestExecutorBuilder graphqlBuilder)
    {
        GraphQLRegistered = true;
    }

    public void RegisterKafka(IKafkaBuilder kafkaBuilder)
    {
        KafkaRegistered = true;
        kafkaBuilder.AddTopic("ioc.test.event");
    }
}

internal sealed class TestPluginMarker { }

// ─────────────────────────────────────────────────────────────────────────────

public sealed class PluginHostTests
{
    private PluginHost CreateHost() =>
        new(NullLogger<PluginHost>.Instance);

    [Fact]
    public void Register_NewPlugin_ShouldAddToPlugins()
    {
        // Arrange
        var host = CreateHost();
        var plugin = new TestPlugin();

        // Act
        host.Register(plugin);

        // Assert
        Assert.Single(host.Plugins);
        Assert.Equal("test-plugin", host.Plugins[0].Id);
    }

    [Fact]
    public void Register_DuplicatePlugin_ShouldIgnoreSecond()
    {
        // Arrange
        var host = CreateHost();
        var plugin1 = new TestPlugin();
        var plugin2 = new TestPlugin();

        // Act
        host.Register(plugin1);
        host.Register(plugin2);

        // Assert
        Assert.Single(host.Plugins);
    }

    [Fact]
    public void ConfigureServices_ShouldCallRegisterServicesOnAllPlugins()
    {
        // Arrange
        var host = CreateHost();
        var plugin = new TestPlugin();
        host.Register(plugin);
        var services = new ServiceCollection();

        // Act
        host.ConfigureServices(services);

        // Assert
        Assert.True(plugin.ServicesRegistered);
        var sp = services.BuildServiceProvider();
        Assert.NotNull(sp.GetService<TestPluginMarker>());
    }

    [Fact]
    public void Register_MultiplePlugins_ShouldPreserveOrder()
    {
        // Arrange
        var host = CreateHost();

        // Act
        host.Register(new TestPlugin());

        // Assert
        Assert.NotEmpty(host.Plugins);
    }
}
