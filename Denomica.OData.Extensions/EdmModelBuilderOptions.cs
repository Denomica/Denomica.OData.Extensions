using System;
using System.Collections.Generic;
using System.Text;

namespace Denomica.OData.Extensions
{
    public sealed class EdmModelBuilderOptions
    {
        public EdmPropertyNamingOptions PropertyNaming { get; set; } = new EdmPropertyNamingOptions { NamingPolicy = PropertyNamingPolicy.CamelCase };
    }

    public sealed class EdmPropertyNamingOptions
    {
        public PropertyNamingPolicy NamingPolicy { get; set; }
    }

    public enum PropertyNamingPolicy
    {
        /// <summary>
        /// Names of properties are stored in the EDM model as they are defined.
        /// </summary>
        Default,

        /// <summary>
        /// Properties are changed to camel case.
        /// </summary>
        CamelCase
    }
}
