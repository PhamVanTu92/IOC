using Dapper;
using System.Data;

namespace MetadataService.Infrastructure.Persistence.TypeHandlers;

/// <summary>
/// Dapper type handler cho DateTimeOffset — Npgsql trả về DateTimeOffset đúng timezone.
/// </summary>
public sealed class DateTimeOffsetHandler : SqlMapper.TypeHandler<DateTimeOffset>
{
    public static readonly DateTimeOffsetHandler Instance = new();

    public override DateTimeOffset Parse(object value) => value switch
    {
        DateTimeOffset dto => dto,
        DateTime dt        => new DateTimeOffset(dt, TimeSpan.Zero),
        _                  => DateTimeOffset.Parse(value.ToString()!)
    };

    public override void SetValue(IDbDataParameter parameter, DateTimeOffset value)
    {
        parameter.Value = value;
    }
}
