using System.Linq.Expressions;

namespace SimpleMapper;

public interface IMapperConfigurationExpression
{
    ITypeMapExpression<TSource, TDestination> CreateMap<TSource, TDestination>();
}

public interface ITypeMapExpression<TSource, TDestination>
{
    ITypeMapExpression<TSource, TDestination> ForMember<TMember>(
        Expression<Func<TDestination, TMember>> destinationMember,
        Expression<Func<TSource, TMember>> mapExpression);

    ITypeMapExpression<TSource, TDestination> Ignore<TMember>(
        Expression<Func<TDestination, TMember>> destinationMember);
}