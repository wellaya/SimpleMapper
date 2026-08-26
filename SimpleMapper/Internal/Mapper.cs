using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace SimpleMapper.Internal;

internal sealed class Mapper : IMapper, IMapperConfiguration
{
    private readonly MapperConfigurationExpression _config;
    private readonly ConcurrentDictionary<(Type, Type), Delegate> _compiledCache = new();

    public Mapper(MapperConfigurationExpression config) => _config = config;

    public TDestination Map<TDestination>(object source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var func = GetOrBuildMapper(source.GetType(), typeof(TDestination));
        return (TDestination)func.DynamicInvoke(source)!;
    }

    public TDestination Map<TSource, TDestination>(TSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var func = (Func<TSource, TDestination>)GetOrBuildMapper(typeof(TSource), typeof(TDestination));
        return func(source);
    }

    public TDestination Map<TSource, TDestination>(TSource source, TDestination destination)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);

        var typeMap = GetTypeMap(typeof(TSource), typeof(TDestination));
        var destProps = typeof(TDestination)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite);

        foreach (var destProp in destProps)
        {
            if (typeMap.IgnoredMembers.Contains(destProp.Name)) continue;

            if (typeMap.CustomMemberMaps.TryGetValue(destProp.Name, out var customExpr))
            {
                destProp.SetValue(destination, customExpr.Compile().DynamicInvoke(source));
                continue;
            }

            var sourceProp = typeof(TSource).GetProperty(destProp.Name, BindingFlags.Public | BindingFlags.Instance);
            if (sourceProp is not null && destProp.PropertyType.IsAssignableFrom(sourceProp.PropertyType))
            {
                destProp.SetValue(destination, sourceProp.GetValue(source));
            }
        }

        return destination;
    }

    public IQueryable<TDestination> ProjectTo<TDestination>(IQueryable source)
    {
        var sourceType = source.ElementType;
        var typeMap = GetTypeMap(sourceType, typeof(TDestination));
        var selector = BuildProjectionExpression(sourceType, typeof(TDestination), typeMap);

        var selectCall = Expression.Call(
            typeof(Queryable), nameof(Queryable.Select),
            new[] { sourceType, typeof(TDestination) },
            source.Expression, selector);

        return source.Provider.CreateQuery<TDestination>(selectCall);
    }

    public void AssertConfigurationIsValid()
    {
        var errors = new List<string>();

        foreach (var (key, typeMap) in _config.TypeMaps)
        {
            var destProps = key.Dest.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite);

            foreach (var destProp in destProps)
            {
                if (typeMap.IgnoredMembers.Contains(destProp.Name)) continue;
                if (typeMap.CustomMemberMaps.ContainsKey(destProp.Name)) continue;

                var sourceProp = key.Source.GetProperty(destProp.Name, BindingFlags.Public | BindingFlags.Instance);
                if (sourceProp is null)
                {
                    errors.Add($"{key.Source.Name} -> {key.Dest.Name}: unmapped destination member '{destProp.Name}'. " +
                               $"Add .ForMember(), .Ignore(), or a matching source property.");
                }
            }
        }

        if (errors.Count > 0)
            throw new MappingException("Mapper configuration is invalid:\n" + string.Join("\n", errors));
    }

    private Delegate GetOrBuildMapper(Type sourceType, Type destType) =>
        _compiledCache.GetOrAdd((sourceType, destType), key =>
        {
            var typeMap = GetTypeMap(key.Item1, key.Item2);
            return BuildProjectionExpression(key.Item1, key.Item2, typeMap).Compile();
        });

    private TypeMap GetTypeMap(Type sourceType, Type destType) =>
        _config.TypeMaps.TryGetValue((sourceType, destType), out var typeMap)
            ? typeMap
            : new TypeMap { SourceType = sourceType, DestinationType = destType };

    private LambdaExpression BuildProjectionExpression(Type sourceType, Type destType, TypeMap typeMap)
    {
        var sourceParam = Expression.Parameter(sourceType, "src");
        var bindings = new List<MemberBinding>();

        var destProps = destType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanWrite);

        foreach (var destProp in destProps)
        {
            if (typeMap.IgnoredMembers.Contains(destProp.Name)) continue;

            if (typeMap.CustomMemberMaps.TryGetValue(destProp.Name, out var customExpr))
            {
                var visitor = new ParameterReplaceVisitor(customExpr.Parameters[0], sourceParam);
                bindings.Add(Expression.Bind(destProp, visitor.Visit(customExpr.Body)!));
                continue;
            }

            var sourceProp = sourceType.GetProperty(destProp.Name, BindingFlags.Public | BindingFlags.Instance);
            if (sourceProp is null) continue;

            var sourceAccess = Expression.Property(sourceParam, sourceProp);

            if (destProp.PropertyType.IsAssignableFrom(sourceProp.PropertyType))
            {
                bindings.Add(Expression.Bind(destProp, sourceAccess));
            }
            else if (IsCollection(sourceProp.PropertyType, out var srcElem) && IsCollection(destProp.PropertyType, out var destElem))
            {
                var nestedMap = GetTypeMap(srcElem!, destElem!);
                var nestedLambda = BuildProjectionExpression(srcElem!, destElem!, nestedMap);

                var selectCall = Expression.Call(typeof(Enumerable), nameof(Enumerable.Select),
                    new[] { srcElem!, destElem! }, sourceAccess, nestedLambda);
                var toListCall = Expression.Call(typeof(Enumerable), nameof(Enumerable.ToList),
                    new[] { destElem! }, selectCall);

                bindings.Add(Expression.Bind(destProp, toListCall));
            }
            else if (sourceProp.PropertyType.IsClass && destProp.PropertyType.IsClass && sourceProp.PropertyType != typeof(string))
            {
                var nestedMap = GetTypeMap(sourceProp.PropertyType, destProp.PropertyType);
                var nestedLambda = BuildProjectionExpression(sourceProp.PropertyType, destProp.PropertyType, nestedMap);
                var invoked = Expression.Invoke(nestedLambda, sourceAccess);

                var nullCheck = Expression.Condition(
                    Expression.Equal(sourceAccess, Expression.Constant(null, sourceProp.PropertyType)),
                    Expression.Constant(null, destProp.PropertyType),
                    Expression.Convert(invoked, destProp.PropertyType));

                bindings.Add(Expression.Bind(destProp, nullCheck));
            }
        }

        var initExpr = Expression.MemberInit(Expression.New(destType), bindings);
        var delegateType = typeof(Func<,>).MakeGenericType(sourceType, destType);
        return Expression.Lambda(delegateType, initExpr, sourceParam);
    }

    private static bool IsCollection(Type type, out Type? elementType)
    {
        elementType = null;
        if (type == typeof(string)) return false;

        var enumerable = type.GetInterfaces().Prepend(type)
            .FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        if (enumerable is null) return false;
        elementType = enumerable.GetGenericArguments()[0];
        return true;
    }

    private sealed class ParameterReplaceVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _old, _new;
        public ParameterReplaceVisitor(ParameterExpression oldParam, ParameterExpression newParam) { _old = oldParam; _new = newParam; }
        protected override Expression VisitParameter(ParameterExpression node) => node == _old ? _new : base.VisitParameter(node);
    }
}