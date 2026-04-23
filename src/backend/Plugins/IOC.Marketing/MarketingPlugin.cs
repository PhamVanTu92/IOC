using HotChocolate.Execution.Configuration;
using IOC.Core.Kafka;
using IOC.Core.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace IOC.Marketing;

/// <summary>
/// Marketing Plugin
/// Quản lý campaign, leads, conversion, ROI marketing
/// </summary>
public sealed class MarketingPlugin : IPlugin
{
    public string Id          => "marketing";
    public string Name        => "Marketing";
    public string Version     => "1.0.0";
    public string Description => "Quản lý campaign, leads, conversion funnel và ROI marketing";

    public void RegisterServices(IServiceCollection services)
    {
        // services.AddScoped<ICampaignRepository, CampaignRepository>();
        // services.AddScoped<ILeadService, LeadService>();
        // services.AddDbContext<MarketingDbContext>(...);
    }

    public void RegisterGraphQL(IRequestExecutorBuilder graphqlBuilder)
    {
        // graphqlBuilder
        //   .AddType<CampaignType>()
        //   .AddType<LeadType>()
        //   .AddTypeExtension<MarketingQueryExtension>()
        //   .AddTypeExtension<MarketingMutationExtension>();
    }

    public void RegisterKafka(IKafkaBuilder kafkaBuilder)
    {
        kafkaBuilder
            .AddTopic("ioc.marketing.campaign-launched")
            .AddTopic("ioc.marketing.campaign-ended")
            .AddTopic("ioc.marketing.lead-created")
            .AddTopic("ioc.marketing.lead-converted");
    }
}
