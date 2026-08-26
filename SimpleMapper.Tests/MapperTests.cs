using Microsoft.Extensions.DependencyInjection;
using SimpleMapper;
using Xunit;

namespace SimpleMapper.Tests;

public class Source
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class Dest
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class TestProfile : MapProfile
{
    public override void Configure(IMapperConfigurationExpression config)
    {
        config.CreateMap<Source, Dest>();
    }
}

public class MapperTests
{
    private static IMapper BuildMapper()
    {
        var services = new ServiceCollection();
        services.AddSimpleMapper(typeof(TestProfile).Assembly);
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    [Fact]
    public void Maps_Simple_Properties()
    {
        var mapper = BuildMapper();
        var result = mapper.Map<Source, Dest>(new Source { Id = 1, Name = "Test" });

        Assert.Equal(1, result.Id);
        Assert.Equal("Test", result.Name);
    }

    [Fact]
    public void AssertConfigurationIsValid_Throws_On_Unmapped_Property()
    {
        var services = new ServiceCollection();
        services.AddSimpleMapper(typeof(BadProfile).Assembly);
        var provider = services.BuildServiceProvider();
        var config = provider.GetRequiredService<IMapperConfiguration>();

        Assert.Throws<MappingException>(() => config.AssertConfigurationIsValid());
    }
}

public class BadSource { public int Id { get; set; } }
public class BadDest { public int Id { get; set; } public string Unmapped { get; set; } = ""; }

public class BadProfile : MapProfile
{
    public override void Configure(IMapperConfigurationExpression config) =>
        config.CreateMap<BadSource, BadDest>();
}