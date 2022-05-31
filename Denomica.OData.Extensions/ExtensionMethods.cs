using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Denomica.OData.Extensions
{
    /// <summary>
    /// Exposes extension methods for working with OData and Edm models.
    /// </summary>
    public static class ExtensionMethods
    {

        public static ODataUriParser CreateUriParser(this IEdmModel model, string relativeUri)
        {
            return model.CreateUriParser(new Uri(relativeUri, UriKind.Relative));
        }

        public static ODataUriParser CreateUriParser(this IEdmModel model, Uri relativeUri)
        {
            if (relativeUri.IsAbsoluteUri) throw new ArgumentException("Only relative URIs are supported.", nameof(relativeUri));
            return new ODataUriParser(model, relativeUri);
        }

        public static EdmEntityType FindEntityType<TEntity>(this IEdmModel model)
        {
            return model.FindEntityType(typeof(TEntity));
        }

        public static EdmEntityType FindEntityType(this IEdmModel model, Type type)
        {
            return model.FindDeclaredType(type.FullName) as EdmEntityType;
        }

        public static EdmEntityContainer GetEntityContainer(this IEdmModel model)
        {
            return (EdmEntityContainer)model.EntityContainer;
        }

        public static IEnumerable<string> SelectedPaths(this SelectExpandClause clause)
        {
            if(null != clause && !clause.AllSelected)
            {
                foreach(var path in from x in clause.SelectedItems where x is PathSelectItem select (PathSelectItem)x)
                {
                    foreach(var selectedPath in path.SelectedPath)
                    {
                        yield return selectedPath.Identifier;
                    }
                }
            }

            yield break;
        }

        public static string ToCamelCase(this string s)
        {
            if(s?.Length > 1)
            {
                return s.Substring(0, 1).ToLower() + s.Substring(1);
            }

            return s?.ToLower();
        }

        public static IList<Tuple<SingleValuePropertyAccessNode, OrderByDirection>> ToList(this OrderByClause clause)
        {
            var list = new List<Tuple<SingleValuePropertyAccessNode, OrderByDirection>>();

            var parent = clause;

            while(null != parent)
            {
                if(parent.Expression is SingleValuePropertyAccessNode)
                {
                    list.Add(new Tuple<SingleValuePropertyAccessNode, OrderByDirection>((SingleValuePropertyAccessNode)parent.Expression, parent.Direction));
                }

                parent = parent.ThenBy;
            }

            return list;
        }
    }
}
