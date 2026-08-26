using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.Extensions.DependencyInjection;

namespace SimpleMapper.Benchmarks;

[MemoryDiagnoser]                          // reports allocations — important, not just speed
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class MapperBenchmarks
{
    private Order _order = null!;
    private IMapper _simpleMapper = null!;

    [GlobalSetup]
    public void Setup()
    {
        _order = new Order
        {
            Id = 1,
            CustomerName = "Jane Doe",
            CreatedAt = DateTime.UtcNow,
            ShippingAddress = new Address { Street = "1 Main St", City = "Perth", PostCode = "6000" },
            Lines = Enumerable.Range(1, 5)
                .Select(i => new OrderLine { ProductName = $"Product {i}", Quantity = i, UnitPrice = 9.99m * i })
                .ToList()
        };

        var services = new ServiceCollection();
        services.AddSimpleMapper(typeof(BenchmarkProfile).Assembly);
        _simpleMapper = services.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    [Benchmark(Baseline = true, Description = "Manual mapping (hand-written)")]
    public OrderDto ManualMapping()
    {
        return new OrderDto
        {
            Id = _order.Id,
            CustomerName = _order.CustomerName,
            CreatedAt = _order.CreatedAt,
            ShippingAddress = new AddressDto
            {
                Street = _order.ShippingAddress.Street,
                City = _order.ShippingAddress.City,
                PostCode = _order.ShippingAddress.PostCode
            },
            Lines = _order.Lines.Select(l => new OrderLineDto
            {
                ProductName = l.ProductName,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice
            }).ToList()
        };
    }

    [Benchmark(Description = "SimpleMapper (compiled expression, cached)")]
    public OrderDto SimpleMapperMapping()
    {
        return _simpleMapper.Map<Order, OrderDto>(_order);
    }

    [Benchmark(Description = "Raw reflection (no caching, naive)")]
    public OrderDto RawReflectionMapping()
    {
        return (OrderDto)MapViaReflection(_order, typeof(OrderDto))!;
    }

    // Deliberately naive/uncached reflection — represents the "do it yourself
    // quickly without a library" baseline that shows why compiled expressions matter.
    private static object? MapViaReflection(object? source, Type destType)
    {
        if (source is null) return null;

        var dest = Activator.CreateInstance(destType)!;
        var sourceType = source.GetType();

        foreach (var destProp in destType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanWrite))
        {
            var sourceProp = sourceType.GetProperty(destProp.Name, BindingFlags.Public | BindingFlags.Instance);
            if (sourceProp is null) continue;

            var value = sourceProp.GetValue(source);

            if (destProp.PropertyType.IsAssignableFrom(sourceProp.PropertyType))
            {
                destProp.SetValue(dest, value);
            }
            else if (value is System.Collections.IEnumerable enumerable && value is not string)
            {
                var destElemType = destProp.PropertyType.GetGenericArguments().FirstOrDefault();
                if (destElemType is null) continue;

                var listType = typeof(List<>).MakeGenericType(destElemType);
                var list = (System.Collections.IList)Activator.CreateInstance(listType)!;

                foreach (var item in enumerable)
                    list.Add(MapViaReflection(item, destElemType));

                destProp.SetValue(dest, list);
            }
            else if (sourceProp.PropertyType.IsClass && destProp.PropertyType.IsClass)
            {
                destProp.SetValue(dest, MapViaReflection(value, destProp.PropertyType));
            }
        }

        return dest;
    }
}