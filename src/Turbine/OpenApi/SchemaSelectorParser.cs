using System.Linq.Expressions;
using System.Reflection;

namespace Turbine;

internal static class SchemaSelectorParser
{
    public static string ParsePropertyName<T>(Expression<Func<T, ISchema>> selector, string parameterName)
    {
        var body = selector.Body;
        while (body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary)
        {
            body = unary.Operand;
        }

        if (body is MemberExpression member
            && member.Member is PropertyInfo property
            && member.Expression == selector.Parameters[0])
        {
            return property.Name;
        }

        throw new ArgumentException(
            $"Schema selector must be a direct property access on '{typeof(T).Name}' " +
            $"(e.g. x => x.SomeSchemaProperty); got: {selector.Body}.",
            parameterName);
    }
}
