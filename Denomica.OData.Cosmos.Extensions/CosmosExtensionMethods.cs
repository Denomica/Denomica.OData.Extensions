using Microsoft.Azure.Cosmos;
using Microsoft.OData.UriParser;
using System;
using System.Text;
using Denomica.Cosmos.Extensions;
using Denomica.OData.Extensions;
using System.Linq;

namespace Denomica.OData.Cosmos.Extensions
{
    public static class CosmosExtensionMethods
    {

        public static QueryDefinitionBuilder AppendSelectAndExpand(this QueryDefinitionBuilder builder, ODataUriParser uriParser)
        {
            var select = uriParser.ParseSelectAndExpand();
            if (null != select && !select.AllSelected && !select.SelectedItems.All(x => x is PathSelectItem))
            {
                throw new NotSupportedException("Only path select items are currently supported.");
            }

            builder
                .AppendQueryText("SELECT")
                .AppendQueryTextIf(" *", null == select || select.AllSelected)
                .AppendQueryTextIf($" c.{string.Join(",c.", select.SelectedPaths())}", null != select && !select.AllSelected)
                .AppendQueryText(" FROM c")
                ;

            return builder;
        }

        public static QueryDefinitionBuilder AppendFilter(this QueryDefinitionBuilder builder, ODataUriParser uriParser)
        {
            var filter = uriParser.ParseFilter();
            if (null != filter?.Expression)
            {
                builder
                    .AppendQueryText(" WHERE ")
                    .AppendFilterNode(filter.Expression)
                    ;
            }

            return builder;
        }



        public static QueryDefinitionBuilder AppendOrderBy(this QueryDefinitionBuilder builder, ODataUriParser uriParser)
        {
            var orderBy = uriParser.ParseOrderBy().ToList();
            if (orderBy.Count > 0)
            {
                builder.AppendQueryText(" ORDER BY");
                var orderByIndex = 0;
                foreach (var item in orderBy)
                {
                    builder
                        .AppendQueryTextIf(",", orderByIndex > 0)
                        .AppendQueryTextIf(" ", orderByIndex == 0)
                        .AppendQueryText("c.")
                        .AppendQueryText(item.Item1.Property.Name)
                        .AppendQueryTextIf(" desc", item.Item2 == OrderByDirection.Descending);

                    orderByIndex++;
                }
            }

            return builder;
        }

        public static QueryDefinition CreateQueryDefinition(this ODataUriParser uriParser)
        {
            return new QueryDefinitionBuilder()
                .AppendSelectAndExpand(uriParser)
                .AppendFilter(uriParser)
                .AppendOrderBy(uriParser)
                .Build()
                ;
        }



        private static QueryDefinitionBuilder AppendFilterNode(this QueryDefinitionBuilder builder, SingleValueNode node)
        {
            if (node is BinaryOperatorNode)
            {
                builder.AppendFilterNode((BinaryOperatorNode)node);
            }
            else if (node is ConvertNode)
            {
                builder.AppendFilterNode((ConvertNode)node);
            }
            else if (node is SingleValuePropertyAccessNode)
            {
                builder.AppendFilterNode((SingleValuePropertyAccessNode)node);
            }
            else if (node is ConstantNode)
            {
                builder.AppendFilterNode((ConstantNode)node);
            }
            else
            {
                throw new NotSupportedException($"Unsupported filter node type: {node.GetType().FullName}");
            }

            return builder;
        }

        private static QueryDefinitionBuilder AppendFilterNode(this QueryDefinitionBuilder builder, BinaryOperatorNode node)
        {
            Func<SingleValueNode, bool> isBinaryOr = svNode =>
            {
                return svNode is BinaryOperatorNode && ((BinaryOperatorNode)svNode).OperatorKind == BinaryOperatorKind.Or;
            };

            switch (node.OperatorKind)
            {
                case BinaryOperatorKind.And:
                    builder
                        .AppendFilterNode(node.Left)
                        .AppendQueryText(" and ")
                        .OpenParenthesisIf(isBinaryOr(node.Right))
                        .AppendFilterNode(node.Right)
                        .CloseParenthesisIf(isBinaryOr(node.Right))
                        ;
                    break;

                case BinaryOperatorKind.Or:
                    builder
                        .AppendFilterNode(node.Left)
                        .AppendQueryText(" or ")
                        .AppendFilterNode(node.Right)
                        ;
                    break;

                case BinaryOperatorKind.Equal:
                case BinaryOperatorKind.GreaterThan:
                case BinaryOperatorKind.GreaterThanOrEqual:
                case BinaryOperatorKind.LessThan:
                case BinaryOperatorKind.LessThanOrEqual:
                    builder
                        .AppendFilterNode(node.Left)
                        .AppendQueryTextIf(" =", node.OperatorKind == BinaryOperatorKind.Equal)
                        .AppendQueryTextIf(" >", node.OperatorKind == BinaryOperatorKind.GreaterThan)
                        .AppendQueryTextIf(" >=", node.OperatorKind == BinaryOperatorKind.GreaterThanOrEqual)
                        .AppendQueryTextIf(" <", node.OperatorKind == BinaryOperatorKind.LessThan)
                        .AppendQueryTextIf(" <=", node.OperatorKind == BinaryOperatorKind.LessThanOrEqual)
                        .AppendQueryTextIf(" !=", node.OperatorKind == BinaryOperatorKind.NotEqual)
                        .AppendFilterNode(node.Right)
                        ;
                    break;

                default:
                    throw new NotSupportedException($"Unsupported node operator kind: {node.OperatorKind}");
            }

            return builder;
        }

        private static QueryDefinitionBuilder AppendFilterNode(this QueryDefinitionBuilder builder, ConvertNode node)
        {
            builder.AppendFilterNode(node.Source);
            return builder;
        }

        private static QueryDefinitionBuilder AppendFilterNode(this QueryDefinitionBuilder builder, SingleValuePropertyAccessNode node)
        {
            return builder
                .AppendQueryText("c.")
                .AppendQueryText(node.Property.Name);
        }

        private static QueryDefinitionBuilder AppendFilterNode(this QueryDefinitionBuilder builder, ConstantNode node)
        {
            var name = $"@p{builder.Parameters.Count}";

            object value = node.Value;
            if(value is Microsoft.OData.Edm.Date)
            {
                // We need to convert an Edm.Date to a DateTime, because otherwise filtering on dates
                // will not work and produce the required result.
                var dt = (Microsoft.OData.Edm.Date)node.Value;
                value = new DateTime(dt.Year, dt.Month, dt.Day);
            }

            return builder
                .AppendQueryText(" ")
                .AppendQueryText(name)
                .WithParameter(name, value)
                ;
        }

        private static QueryDefinitionBuilder OpenParenthesis(this QueryDefinitionBuilder builder)
        {
            return builder.AppendQueryText("(");
        }

        private static QueryDefinitionBuilder OpenParenthesisIf(this QueryDefinitionBuilder builder, bool condition)
        {
            if (condition) builder.OpenParenthesis();
            return builder;
        }

        private static QueryDefinitionBuilder CloseParenthesis(this QueryDefinitionBuilder builder)
        {
            return builder.AppendQueryText(")");
        }

        private static QueryDefinitionBuilder CloseParenthesisIf(this QueryDefinitionBuilder builder, bool condition)
        {
            if (condition) builder.CloseParenthesis();
            return builder;
        }
    }
}
