using System.Linq.Expressions;

namespace SimpleMapper.Internal;

internal sealed class TypeMap
{
    public required Type SourceType { get; init; }
    public required Type DestinationType { get; init; }
    public HashSet<string> IgnoredMembers { get; } = new();
    public Dictionary<string, LambdaExpression> CustomMemberMaps { get; } = new();
}