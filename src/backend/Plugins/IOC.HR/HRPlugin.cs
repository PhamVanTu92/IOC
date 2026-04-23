using HotChocolate.Execution.Configuration;
using IOC.Core.Kafka;
using IOC.Core.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace IOC.HR;

/// <summary>
/// HR Plugin — Nhân sự
/// Quản lý nhân viên, chấm công, nghỉ phép, lương thưởng
/// </summary>
public sealed class HRPlugin : IPlugin
{
    public string Id          => "hr";
    public string Name        => "Nhân Sự";
    public string Version     => "1.0.0";
    public string Description => "Quản lý nhân viên, chấm công, nghỉ phép và lương thưởng";

    public void RegisterServices(IServiceCollection services)
    {
        // services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        // services.AddScoped<IAttendanceService, AttendanceService>();
        // services.AddDbContext<HRDbContext>(...);
    }

    public void RegisterGraphQL(IRequestExecutorBuilder graphqlBuilder)
    {
        // graphqlBuilder
        //   .AddType<EmployeeType>()
        //   .AddTypeExtension<HRQueryExtension>()
        //   .AddTypeExtension<HRMutationExtension>()
        //   .AddTypeExtension<HRSubscriptionExtension>();
    }

    public void RegisterKafka(IKafkaBuilder kafkaBuilder)
    {
        kafkaBuilder
            .AddTopic("ioc.hr.employee-joined")
            .AddTopic("ioc.hr.employee-left")
            .AddTopic("ioc.hr.leave-approved")
            .AddTopic("ioc.hr.payroll-processed");
    }
}
