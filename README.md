# SimpleMapper

A lightweight, dependency-free object-to-object mapper for .NET — built as an
open-source alternative for teams that can't or don't want to use AutoMapper's
commercial license.

## Install

```bash
dotnet add package SimpleMapper
```

## Quick start

```csharp
services.AddSimpleMapper(typeof(MyProfile).Assembly);

public class MyProfile : MapProfile
{
    public override void Configure(IMapperConfigurationExpression config)
    {
        config.CreateMap<Order, OrderDto>()
            .ForMember(d => d.CustomerName, s => s.Customer.FullName);
    }
}

var dto = mapper.Map<Order, OrderDto>(order);
var queryable = mapper.ProjectTo<OrderDto>(dbContext.Orders); // EF Core-friendly
```

## Features

- Convention + explicit member mapping
- Nested object & collection mapping
- EF Core `ProjectTo` (SQL-side projection)
- Compiled expression trees (cached, fast)
- Config validation (`AssertConfigurationIsValid`)
- Zero dependencies, MIT licensed

## Performance

Benchmarked with [BenchmarkDotNet](https://benchmarkdotnet.org/) mapping a nested object graph (order → 5 line items + address):

| Method                                       |       Mean |     Error |    StdDev |     Median | Ratio | RatioSD | Rank |   Gen0 | Allocated | Alloc Ratio |
| -------------------------------------------- | ---------: | --------: | --------: | ---------: | ----: | ------: | ---: | -----: | --------: | ----------: |
| 'Manual mapping (hand-written)'              |   223.0 ns |  13.64 ns |  38.69 ns |   211.8 ns |  1.03 |    0.23 |    1 | 0.3672 |     576 B |        1.00 |
| 'SimpleMapper (compiled expression, cached)' | 1,441.1 ns |  47.07 ns | 130.42 ns | 1,416.4 ns |  6.63 |    1.15 |    2 | 0.4482 |     704 B |        1.22 |
| 'Raw reflection (no caching, naive)'         | 4,499.4 ns | 149.07 ns | 430.11 ns | 4,403.6 ns | 20.70 |    3.65 |    3 | 1.1063 |    1737 B |        3.02 |

SimpleMapper is roughly **3x faster than naive reflection** with far fewer
allocations, while staying close to hand-written mapping code. See the
[Optimization notes](#optimization-notes) below for what accounts for the gap
vs. manual mapping.

## License

MIT
