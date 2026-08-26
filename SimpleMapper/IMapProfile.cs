namespace SimpleMapper;

public interface IMapProfile
{
    void Configure(IMapperConfigurationExpression config);
}

public abstract class MapProfile : IMapProfile
{
    public abstract void Configure(IMapperConfigurationExpression config);
}