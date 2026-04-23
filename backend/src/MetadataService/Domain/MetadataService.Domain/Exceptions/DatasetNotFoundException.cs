namespace MetadataService.Domain.Exceptions;

public sealed class DatasetNotFoundException : Exception
{
    public Guid DatasetId { get; }
    public Guid TenantId { get; }

    public DatasetNotFoundException(Guid datasetId, Guid tenantId)
        : base($"Dataset '{datasetId}' not found for tenant '{tenantId}'.")
    {
        DatasetId = datasetId;
        TenantId = tenantId;
    }
}
