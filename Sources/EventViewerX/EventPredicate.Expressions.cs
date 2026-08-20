using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace EventViewerX;

public sealed partial class EventPredicate {
    /// <summary>
    /// Translates a restricted strongly typed C# expression into the shared serializable predicate model.
    /// Arbitrary method calls are rejected rather than executed.
    /// </summary>
    public static EventPredicate FromExpression<TRecord>(
        Expression<Func<TRecord, bool>> expression) where TRecord : EventTypeRecord {

        if (expression == null) {
            throw new ArgumentNullException(nameof(expression));
        }
        EventPredicate predicate = ExpressionTranslator.Translate(expression.Body, expression.Parameters[0]);
        predicate.Validate();
        return predicate;
    }

    private static class ExpressionTranslator {
        internal static EventPredicate Translate(Expression expression, ParameterExpression parameter) {
            expression = StripConvert(expression);
            if (expression is BinaryExpression binary) {
                return TranslateBinary(binary, parameter);
            }
            if (expression is UnaryExpression { NodeType: ExpressionType.Not } unary) {
                return Not(Translate(unary.Operand, parameter));
            }
            if (expression is MethodCallExpression call) {
                return TranslateCall(call, parameter);
            }
            throw Unsupported(expression);
        }

        private static EventPredicate TranslateBinary(BinaryExpression binary, ParameterExpression parameter) {
            if (binary.NodeType == ExpressionType.AndAlso) {
                return AllOf(Translate(binary.Left, parameter), Translate(binary.Right, parameter));
            }
            if (binary.NodeType == ExpressionType.OrElse) {
                return AnyOf(Translate(binary.Left, parameter), Translate(binary.Right, parameter));
            }

            bool memberOnLeft = TryGetField(binary.Left, parameter, out string? field);
            bool memberOnRight = TryGetField(binary.Right, parameter, out string? rightField);
            if (memberOnLeft == memberOnRight) {
                throw Unsupported(binary);
            }
            Expression valueExpression = memberOnLeft ? binary.Right : binary.Left;
            object? value = ReadCapturedValue(valueExpression);
            if (value == null && binary.NodeType is ExpressionType.Equal or ExpressionType.NotEqual) {
                EventPredicate nullPredicate = Compare(
                    memberOnLeft ? field! : rightField!,
                    binary.NodeType == ExpressionType.Equal
                        ? EventPredicateOperator.IsNull
                        : EventPredicateOperator.IsNotNull);
                nullPredicate.IgnoreCase = false;
                return nullPredicate;
            }
            EventPredicateOperator comparison = ToOperator(binary.NodeType, reverse: memberOnRight);
            return CompareExpression(memberOnLeft ? field! : rightField!, comparison, value);
        }

        private static EventPredicate TranslateCall(MethodCallExpression call, ParameterExpression parameter) {
            if (call.Object != null && TryGetField(call.Object, parameter, out string? field)) {
                EventPredicateOperator comparison = call.Method.Name switch {
                    nameof(string.Contains) => EventPredicateOperator.Contains,
                    nameof(string.StartsWith) => EventPredicateOperator.StartsWith,
                    nameof(string.EndsWith) => EventPredicateOperator.EndsWith,
                    _ => throw Unsupported(call)
                };
                if (call.Arguments.Count != 1) {
                    throw Unsupported(call);
                }
                return CompareExpression(field!, comparison, ReadCapturedValue(call.Arguments[0]));
            }

            Expression? collectionExpression = null;
            Expression? memberExpression = null;
            if (string.Equals(call.Method.Name, nameof(Enumerable.Contains), StringComparison.Ordinal)) {
                if (call.Object != null && call.Arguments.Count == 1) {
                    collectionExpression = call.Object;
                    memberExpression = call.Arguments[0];
                } else if (call.Object == null && call.Arguments.Count == 2) {
                    collectionExpression = call.Arguments[0];
                    memberExpression = call.Arguments[1];
                }
            }
            if (collectionExpression != null && memberExpression != null &&
                TryGetField(memberExpression, parameter, out string? inField)) {
                object? collection = ReadCapturedValue(collectionExpression);
                if (collection is not IEnumerable values || collection is string) {
                    throw Unsupported(call);
                }
                var items = new List<object?>();
                foreach (object? item in values) {
                    items.Add(item);
                }
                return CompareExpression(inField!, EventPredicateOperator.In, items.ToArray());
            }
            throw Unsupported(call);
        }

        private static EventPredicate CompareExpression(
            string field,
            EventPredicateOperator comparison,
            object? value) {

            EventPredicate predicate = Compare(field, comparison, value);
            predicate.IgnoreCase = false;
            return predicate;
        }

        private static bool TryGetField(
            Expression expression,
            ParameterExpression parameter,
            out string? field) {

            expression = StripConvert(expression);
            if (expression is MemberExpression member &&
                StripConvert(member.Expression!) == parameter) {
                field = member.Member.Name;
                return true;
            }
            field = null;
            return false;
        }

        private static object? ReadCapturedValue(Expression expression) {
            expression = StripConvert(expression);
            if (expression is MethodCallExpression {
                    Method.Name: "op_Implicit",
                    Arguments.Count: 1
                } conversion) {
                return ReadCapturedValue(conversion.Arguments[0]);
            }
            if (expression is ConstantExpression constant) {
                return constant.Value;
            }
            if (expression is NewArrayExpression array) {
                return array.Expressions.Select(ReadCapturedValue).ToArray();
            }
            if (expression is MemberExpression member) {
                object? owner = member.Expression == null ? null : ReadCapturedValue(member.Expression);
                return member.Member switch {
                    FieldInfo field => field.GetValue(owner),
                    PropertyInfo property when property.GetMethod != null && property.GetMethod.GetParameters().Length == 0 =>
                        property.GetValue(owner, null),
                    _ => throw Unsupported(expression)
                };
            }
            throw Unsupported(expression);
        }

        private static EventPredicateOperator ToOperator(ExpressionType type, bool reverse) {
            return (type, reverse) switch {
                (ExpressionType.Equal, _) => EventPredicateOperator.Equal,
                (ExpressionType.NotEqual, _) => EventPredicateOperator.NotEqual,
                (ExpressionType.GreaterThan, false) => EventPredicateOperator.GreaterThan,
                (ExpressionType.GreaterThanOrEqual, false) => EventPredicateOperator.GreaterThanOrEqual,
                (ExpressionType.LessThan, false) => EventPredicateOperator.LessThan,
                (ExpressionType.LessThanOrEqual, false) => EventPredicateOperator.LessThanOrEqual,
                (ExpressionType.GreaterThan, true) => EventPredicateOperator.LessThan,
                (ExpressionType.GreaterThanOrEqual, true) => EventPredicateOperator.LessThanOrEqual,
                (ExpressionType.LessThan, true) => EventPredicateOperator.GreaterThan,
                (ExpressionType.LessThanOrEqual, true) => EventPredicateOperator.GreaterThanOrEqual,
                _ => throw new NotSupportedException($"Expression operator '{type}' is not supported by EventPredicate.")
            };
        }

        private static Expression StripConvert(Expression expression) {
            while (expression is UnaryExpression unary &&
                   unary.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked) {
                expression = unary.Operand;
            }
            return expression;
        }

        private static NotSupportedException Unsupported(Expression expression) => new(
            $"Expression node '{expression.NodeType}' ({expression}) is not supported. " +
            "Use property comparisons, &&, ||, !, string Contains/StartsWith/EndsWith, or collection Contains.");
    }
}
