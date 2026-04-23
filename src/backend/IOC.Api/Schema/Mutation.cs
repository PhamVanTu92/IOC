namespace IOC.Api.Schema;

/// <summary>
/// Root GraphQL Mutation — placeholder.
/// Plugins mở rộng bằng cách AddTypeExtension&lt;PluginMutationExtension&gt;
/// </summary>
[ExtendObjectType("Mutation")]
public sealed class CoreMutation
{
    /// <summary>Placeholder — plugins sẽ thêm mutations riêng</summary>
    public string Ping() => "pong";
}
