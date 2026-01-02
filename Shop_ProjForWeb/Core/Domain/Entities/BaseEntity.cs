namespace Shop_ProjForWeb.Core.Domain.Entities;

public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    protected BaseEntity()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public virtual void ValidateEntity()
    {
        if (Id == Guid.Empty)
            throw new ArgumentException("Entity ID cannot be empty");
        
        if (CreatedAt == default)
            throw new ArgumentException("CreatedAt must be set");
        
        if (UpdatedAt == default)
            throw new ArgumentException("UpdatedAt must be set");
        
        if (IsDeleted && DeletedAt == null)
            throw new InvalidOperationException("DeletedAt must be set when entity is soft deleted");
    }

    protected void ValidateStringProperty(string? value, string propertyName, int minLength = 1, int maxLength = int.MaxValue, bool required = true)
    {
        if (required && string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{propertyName} is required and cannot be null or empty");
        
        if (!string.IsNullOrEmpty(value))
        {
            if (value.Length < minLength)
                throw new ArgumentException($"{propertyName} must be at least {minLength} characters long");
            
            if (value.Length > maxLength)
                throw new ArgumentException($"{propertyName} cannot exceed {maxLength} characters");
        }
    }

    protected void ValidateDecimalProperty(decimal value, string propertyName, decimal minValue = decimal.MinValue, decimal maxValue = decimal.MaxValue)
    {
        if (value < minValue)
            throw new ArgumentException($"{propertyName} cannot be less than {minValue}");
        
        if (value > maxValue)
            throw new ArgumentException($"{propertyName} cannot be greater than {maxValue}");
    }

    protected void ValidateIntProperty(int value, string propertyName, int minValue = int.MinValue, int maxValue = int.MaxValue)
    {
        if (value < minValue)
            throw new ArgumentException($"{propertyName} cannot be less than {minValue}");
        
        if (value > maxValue)
            throw new ArgumentException($"{propertyName} cannot be greater than {maxValue}");
    }
}
