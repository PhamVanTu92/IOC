namespace MetadataService.Domain.Exceptions;

public sealed class DuplicateDatasetException : Exception
{
    public string DatasetName { get; }
    public Guid TenantId { get; }

    public DuplicateDatasetException(string name, Guid tenantId)
        : base($"Dataset with name '{name}' already exists for tenant '{tenantId}'.")
    {
        DatasetName = name;
        TenantId = tenantId;
    }
}
