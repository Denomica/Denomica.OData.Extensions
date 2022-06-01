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

        public static ODataUriParser CreateUriParser(this IEdmModel model, string uri)
        {
            var u = new Uri(uri, UriKind.RelativeOrAbsolute);
            if (!u.IsAbsoluteUri)
            {
                var pathPrefix = uri.StartsWith("/") ? "" : "/";
                u = new Uri($"odata://host{pathPrefix}{uri}");
            }
            return model.CreateUriParser(u);
        }

        public static ODataUriParser CreateUriParser(this IEdmModel model, Uri uri)
        {
            if (!uri.IsAbsoluteUri) throw new ArgumentException("The given URI must be an absolute URI.", nameof(uri));
            if(uri.LocalPath.Split(new char[] {'/'}, StringSplitOptions.RemoveEmptyEntries).Length == 0)
            {
                throw new ArgumentException("The given URI must specify a path with at least one segment.", nameof(uri));
            }

            var pathOnlyUri = uri.GetLeftPart(UriPartial.Path).ToString();
            var rootUri = new Uri(pathOnlyUri.Substring(0, pathOnlyUri.LastIndexOf('/')));

            return new ODataUriParser(model, rootUri, uri);
        }

        public static ODataUriParser CreateUriParser<TEntity>(this EdmModelBuilder builder, string uri)
        {
            return builder.CreateUriParser<TEntity>(new Uri(uri, UriKind.RelativeOrAbsolute));
        }

        public static ODataUriParser CreateUriParser<TEntity>(this EdmModelBuilder builder, Uri uri)
        {
            var u = uri.IsAbsoluteUri ? new Uri(uri.PathAndQuery, UriKind.Relative) : uri;
            var path = u.OriginalString.Contains('?') ? u.OriginalString.Substring(0, u.OriginalString.IndexOf('?')) : u.OriginalString;
            var segments = path.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if(segments.Length > 1)
            {
                builder.AddEntitySet<TEntity>(segments.Last());
            }

            return builder
                .Build()
                .CreateUriParser(uri);
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
