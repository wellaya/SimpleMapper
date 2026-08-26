using System.Linq.Expressions;

namespace SimpleMapper.Internal;

internal sealed class MapperConfigurationExpression : IMapperConfigurationExpression
{
    public Dictionary<(Type Source, Type Dest), TypeMap> TypeMaps { get; } = new();

    public ITypeMapExpression<TSource, TDestination> CreateMap<TSource, TDestination>()
    {
        var key = (typeof(TSource), typeof(TDestination));

        if (!TypeMaps.TryGetValue(key, out var typeMap))
        {
            typeMap = new TypeMap { SourceType = typeof(TSource), DestinationType = typeof(TDestination) };
            TypeMaps[key] = typeMap;
        }

        return new TypeMapExpression<TSource, TDestination>(typeMap);
    }
}

internal sealed class TypeMapExpression<TSource, TDestination> : ITypeMapExpression<TSource, TDestination>
{
    private readonly TypeMap _typeMap;

    public TypeMapExpression(TypeMap typeMap) => _typeMap = typeMap;

    public ITypeMapExpression<TSource, TDestination> ForMember<TMember>(
        Expression<Func<TDestination, TMember>> destinationMember,
        Expression<Func<TSource, TMember>> mapExpression)
    {
        _typeMap.CustomMemberMaps[GetMemberName(destinationMember)] = mapExpression;
        return this;
    }

    public ITypeMapExpression<TSource, TDestination> Ignore<TMember>(
        Expression<Func<TDestination, TMember>> destinationMember)
    {
        _typeMap.IgnoredMembers.Add(GetMemberName(destinationMember));
        return this;
    }

    private static string GetMemberName<TMember>(Expression<Func<TDestination, TMember>> expr)
    {
        return expr.Body switch
        {
            MemberExpression member => member.Member.Name,
            UnaryExpression { Operand: MemberExpression unaryMember } => unaryMember.Member.Name,
            _ => throw new ArgumentException("Expression must be a simple property access.", nameof(expr))
        };
    }
}