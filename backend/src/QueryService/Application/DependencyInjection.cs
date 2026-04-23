using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace QueryService.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Đăng ký QueryService Application layer — MediatR handlers.
    /// </summary>
    public static IServiceCollection AddQueryApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        return services;
    }
}
