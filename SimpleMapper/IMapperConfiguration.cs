namespace SimpleMapper;

public interface IMapperConfiguration
{
    /// <summary>
    /// Throws MappingException if any registered map has an unmapped, non-ignored
    /// destination property with no matching source property or custom mapping.
    /// Call this in a unit test, not in production startup.
    /// </summary>
    void AssertConfigurationIsValid();
}