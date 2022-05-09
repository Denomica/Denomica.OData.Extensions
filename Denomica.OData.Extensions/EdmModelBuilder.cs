using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Denomica.OData.Extensions
{
    using EntityTypeConfiguratorHandler = Action<ODataModelBuilder, EntityTypeConfiguration>;

    public class EdmModelBuilder
    {
        public EdmModelBuilder(EdmModelBuilderOptions options = null)
        {
            this.Options = options ?? new EdmModelBuilderOptions();
        }

        private EdmModelBuilderOptions Options = new EdmModelBuilderOptions();
        private List<Type> EntityTypes = new List<Type>();
        private Dictionary<Type, string> EntitySets = new Dictionary<Type, string>();
        private Dictionary<Type, List<string>> EntityKeys = new Dictionary<Type, List<string>>();

        public EdmModelBuilder AddEntityKey<TEntity>(string propertyName)
        {
            return this.AddEntityKey(typeof(TEntity), propertyName);
        }

        public EdmModelBuilder AddEntityKey(Type entityType, string propertyName)
        {
            if(!this.EntityKeys.ContainsKey(entityType))
            {
                this.EntityKeys[entityType] = new List<string>();
            }

            this.EntityKeys[entityType].Add(propertyName);
            return this;
        }

        public EdmModelBuilder AddEntitySet<TEntity>(string name)
        {
            return this.AddEntitySet(typeof(TEntity), name);
        }

        public EdmModelBuilder AddEntitySet(Type entityType, string name)
        {
            this.EntitySets[entityType] = name;
            return this;
        }

        public EdmModelBuilder AddEntityType<TEntity>()
        {
            return this.AddEntityType(typeof(TEntity));
        }

        public EdmModelBuilder AddEntityType(Type entityType)
        {
            if (!this.EntityTypes.Contains(entityType))
            {
                this.EntityTypes.Add(entityType);
            }

            return this;
        }

        public IEdmModel Build()
        {
            var builder = new ODataModelBuilder();


            foreach (var t in this.EntityTypes)
            {
                var typeConfig = this.AddEntityType(builder, t);
                if(this.EntitySets.ContainsKey(t))
                {
                    builder.AddEntitySet(this.EntitySets[t], typeConfig);
                }
            }

            return builder.GetEdmModel();
        }



        private EntityTypeConfiguration AddEntityType(ODataModelBuilder builder, Type entityType)
        {
            var config = builder.AddEntityType(entityType);
            var baseType = this.EntityTypes.FirstOrDefault(x => x == entityType.BaseType);
            if(null != baseType)
            {
                var baseConfig = this.AddEntityType(builder, baseType);
                config.BaseType = baseConfig;
            }
            this.AddEntityProperties(builder, config, entityType);

            if(this.EntityKeys.ContainsKey(entityType))
            {
                foreach(var propertyName in this.EntityKeys[entityType])
                {
                    var prop = entityType.GetProperty(propertyName);
                    if(null != prop)
                    {
                        config.HasKey(prop);
                    }
                }
            }
            return config;
        }

        private IEnumerable<PropertyConfiguration> AddEntityProperties(ODataModelBuilder builder, EntityTypeConfiguration entityConfig, Type entityType)
        {
            var configs = new List<PropertyConfiguration>();
            var flags = BindingFlags.Public | BindingFlags.Instance;
            if(null != entityConfig.BaseType)
            {
                flags |= BindingFlags.DeclaredOnly;
            }

            foreach(var p in from x in entityType.GetProperties(flags) select x)
            {
                var propertyConfig = entityConfig.AddProperty(p);
                propertyConfig.Name = this.ModifyPropertyName(p.Name);
                configs.Add(propertyConfig);
            }

            return configs;
        }

        private void AddEntityProperties(EdmModel model, EdmStructuredType schemaType, Type sourceType)
        {
            var flags = BindingFlags.Public | BindingFlags.Instance;
            if(null != schemaType.BaseType)
            {
                // If the base type is set for the entity we are adding properties to,
                // then we know the base entity is inlcluded in the model, so we don't
                // need to add inherited properties to the current entity.
                flags |= BindingFlags.DeclaredOnly;
            }

            foreach (var p in from x in sourceType.GetProperties(flags) select x)
            {
                bool isNullable = Nullable.GetUnderlyingType(p.PropertyType) != null;

                var primitiveType = this.GetPrimitiveType(p.PropertyType);
                if (primitiveType.HasValue)
                {
                    schemaType.AddStructuralProperty(this.ModifyPropertyName(p.Name), primitiveType.Value, isNullable);
                }
                else
                {
                    // If the property is not a primitive type, but we find the property's type
                    // among the entities included in the model build, then we add a reference
                    // to that type for the property.
                    var entityType = this.EntityTypes.FirstOrDefault(x => x == p.PropertyType);
                    if(null != entityType)
                    {
                        var propertyType = this.BuildEntityType(model, entityType);
                        schemaType.AddStructuralProperty(this.ModifyPropertyName(p.Name), new EdmEntityTypeReference(propertyType, isNullable));
                    }
                }
            }
        }

        private EdmEntityType BuildEntityType(EdmModel model, Type entityType)
        {
            var baseType = this.EntityTypes.FirstOrDefault(x => x == entityType.BaseType);
            EdmEntityType parentType = null;
            if(null != baseType)
            {
                // If the base type is found among the entities included in the model build,
                // then we first must build that so that we can specify it as a base
                // type for the current entity.
                parentType = this.BuildEntityType(model, baseType);
            }

            EdmEntityType addedType = model.FindEntityType(entityType);
            if(null == addedType)
            {
                // Only add the entity to the model if it has not yet been added.
                if(null != parentType)
                {
                    addedType = model.AddEntityType(entityType.Namespace, entityType.Name, parentType);
                }
                else
                {
                    addedType = model.AddEntityType(entityType.Namespace, entityType.Name);
                }

                this.AddEntityProperties(model, addedType, entityType);
            }

            return addedType;
        }

        private EdmPrimitiveTypeKind? GetPrimitiveType(Type type)
        {
            var t = Nullable.GetUnderlyingType(type) ?? type;

            EdmPrimitiveTypeKind? pType = null;

            if (t == typeof(string))
            {
                pType = EdmPrimitiveTypeKind.String;
            }
            else if (t == typeof(bool))
            {
                pType = EdmPrimitiveTypeKind.Boolean;
            }
            else if (t == typeof(byte))
            {
                pType = EdmPrimitiveTypeKind.Byte;
            }
            else if (t == typeof(sbyte))
            {
                pType = EdmPrimitiveTypeKind.SByte;
            }
            else if (t == typeof(int))
            {
                pType = EdmPrimitiveTypeKind.Int32;
            }
            else if (t == typeof(short))
            {
                pType = EdmPrimitiveTypeKind.Int16;
            }
            else if (t == typeof(long))
            {
                pType = EdmPrimitiveTypeKind.Int64;
            }
            else if (t == typeof(DateTime))
            {
                pType = EdmPrimitiveTypeKind.DateTimeOffset;
            }
            else if (t == typeof(DateTimeOffset))
            {
                pType = EdmPrimitiveTypeKind.DateTimeOffset;
            }
            else if (t == typeof(TimeSpan))
            {
                pType = EdmPrimitiveTypeKind.Duration;
            }
            else if (t == typeof(Guid))
            {
                pType = EdmPrimitiveTypeKind.Guid;
            }
            else if (t == typeof(double))
            {
                pType = EdmPrimitiveTypeKind.Double;
            }
            else if (t == typeof(decimal))
            {
                pType = EdmPrimitiveTypeKind.Decimal;
            }
            else if (t == typeof(float))
            {
                pType = EdmPrimitiveTypeKind.Single;
            }
            else if (t == typeof(byte))
            {
                pType = EdmPrimitiveTypeKind.Byte;
            }
            else if (t == typeof(sbyte))
            {
                pType = EdmPrimitiveTypeKind.SByte;
            }
            else if (t == typeof(byte[]))
            {
                pType = EdmPrimitiveTypeKind.Binary;
            }

            return pType;
        }

        private string ModifyPropertyName(string name)
        {
            if(name?.Length > 0 && this.Options.PropertyNaming.NamingPolicy == PropertyNamingPolicy.CamelCase)
            {
                return $"{name.Substring(0, 1).ToLower()}{name.Substring(1)}";
            }

            return name;
        }
    }
}
