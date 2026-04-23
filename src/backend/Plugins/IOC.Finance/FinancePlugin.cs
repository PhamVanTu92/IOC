using HotChocolate.Execution.Configuration;
using IOC.Core.Kafka;
using IOC.Core.Plugins;
using IOC.Finance.Services;
using Microsoft.Extensions.DependencyInjection;

namespace IOC.Finance;

/// <summary>
/// Finance Plugin — Tài chính
/// Quản lý ngân sách, thu chi, báo cáo tài chính.
///
/// Realtime: publishes MetricUpdatedEvent to Kafka every N seconds
/// so connected dashboards auto-refresh financial KPIs.
/// </summary>
public sealed class FinancePlugin : IPlugin
{
    public string Id          => "finance";
    public string Name        => "Tài Chính";
    public string Version     => "1.0.0";
    public string Description => "Quản lý ngân sách, thu chi, hoá đơn và báo cáo tài chính";

    public void RegisterServices(IServiceCollection services)
    {
        // Publisher options — can be overridden from IConfiguration in host
        services.AddSingleton(new FinancePublisherOptions
        {
            IntervalSeconds = 15,
        });

        // Metric publisher — fires on startup, ticks every IntervalSeconds
        services.AddHostedService<FinanceMetricPublisher>();
    }

    public void RegisterGraphQL(IRequestExecutorBuilder graphqlBuilder)
    {
        // Uncomment when Finance GraphQL types are implemented:
        // graphqlBuilder
        //   .AddType<BudgetType>()
        //   .AddTypeExtension<FinanceQueryExtension>()
        //   .AddTypeExtension<FinanceMutationExtension>();
    }

    public void RegisterKafka(IKafkaBuilder kafkaBuilder)
    {
        kafkaBuilder
            .AddTopic("ioc.finance.budget-updated")
            .AddTopic("ioc.finance.invoice-created")
            .AddTopic("ioc.finance.payment-received");
    }
}
