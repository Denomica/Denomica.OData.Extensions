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

            var simpleProps = from x in entityType.GetProperties(flags)
                              where
                                    (!x.PropertyType.IsClass || x.PropertyType == typeof(string))
                                    && !x.PropertyType.IsArray
                                    && !x.PropertyType.IsEnum
                              select x;
            foreach(var p in simpleProps)
            {
                var propertyConfig = entityConfig.AddProperty(p);
                propertyConfig.Name = this.ModifyPropertyName(p.Name);
                configs.Add(propertyConfig);
            }

            var complexProps = from x in entityType.GetProperties(flags)
                               where this.EntityTypes.Contains(x.PropertyType)
                               select x;
            foreach(var p in complexProps)
            {
                var propertyConfig = entityConfig.AddNavigationProperty(p, EdmMultiplicity.ZeroOrOne);
                propertyConfig.Name = this.ModifyPropertyName(p.Name);
                configs.Add(propertyConfig);
            }

            return configs;
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
