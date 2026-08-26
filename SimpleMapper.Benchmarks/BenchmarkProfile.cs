namespace SimpleMapper.Benchmarks;

public class BenchmarkProfile : MapProfile
{
    public override void Configure(IMapperConfigurationExpression config)
    {
        config.CreateMap<Order, OrderDto>();
        config.CreateMap<Address, AddressDto>();
        config.CreateMap<OrderLine, OrderLineDto>();
    }
}