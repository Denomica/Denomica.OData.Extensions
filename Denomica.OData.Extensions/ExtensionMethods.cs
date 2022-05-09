using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using System;
using System.Collections.Generic;
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

        public static EdmEntityType FindEntityType(this IEdmModel model, Type type)
        {
            return model.FindDeclaredType(type.FullName) as EdmEntityType;
        }

        public static EdmEntityContainer GetEntityContainer(this IEdmModel model)
        {
            return (EdmEntityContainer)model.EntityContainer;
        }
    }
}
