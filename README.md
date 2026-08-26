# SimpleMapper

A lightweight, dependency-free object-to-object mapper for .NET — built as an
open-source alternative for teams that can't or don't want to use AutoMapper's
commercial license.

## Install

\`\`\`bash
dotnet add package SimpleMapper
\`\`\`

## Quick start

\`\`\`csharp
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
\`\`\`

## Features

- Convention + explicit member mapping
- Nested object & collection mapping
- EF Core `ProjectTo` (SQL-side projection)
- Compiled expression trees (cached, fast)
- Config validation (`AssertConfigurationIsValid`)
- Zero dependencies, MIT licensed

## License

MIT
