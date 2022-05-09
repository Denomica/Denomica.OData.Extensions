using System;
using Denomica.OData.Extensions;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using Microsoft.OData.ModelBuilder;
using System.Collections.Generic;

namespace Denomica.OData.Tests
{
    [TestClass]
    public class ODataTests
    {
        [TestMethod]
        public void BuildModel01()
        {
            var model = new EdmModelBuilder()
                .AddEntityType(typeof(Person))
                .Build();

            var personType = (EdmEntityType)model.SchemaElements.First();
            Assert.IsNotNull(personType);

            var dobProperty = (EdmStructuralProperty)personType.DeclaredProperties.First(x => x.Name == nameof(Person.DateOfBirth));
            Assert.IsFalse(dobProperty.Type.IsNullable, "The DateOfBirth property must not be nullable.");
        }

        [TestMethod]
        public void BuildModel02()
        {
            var model = new EdmModelBuilder()
                .AddEntityType(typeof(Person))
                .AddEntityType(typeof(Employee))
                .Build();

            Assert.AreEqual(3, model.SchemaElements.Count()); // 1 container and 2 entities.
            var personType = (EdmEntityType)model.FindDeclaredType(typeof(Person).FullName);
            Assert.IsNotNull(personType);

            var employeeType = (EdmEntityType)model.FindDeclaredType(typeof(Employee).FullName);
            Assert.IsNotNull(employeeType);
            Assert.AreEqual(personType, employeeType.BaseEntityType());
        }

        [TestMethod]
        public void BuildModel03()
        {
            // Make sure that types are properly added regardless of the order in which
            // they are added to the model.
            var model = new EdmModelBuilder()
                .AddEntityType(typeof(Employee))
                .AddEntityType(typeof(Person))
                .Build();

            var personType = model.FindEntityType(typeof(Person));
            var employeeType = model.FindEntityType(typeof(Employee));

            Assert.IsNotNull(personType);
            Assert.IsNotNull(employeeType);
            Assert.AreEqual(personType, employeeType.BaseEntityType());
        }



        [TestMethod]
        public void ParseUri01()
        {
            var model = new EdmModelBuilder()
                .AddEntityType<Person>()
                .AddEntitySet<Person>("persons")
                .AddEntityKey<Person>(nameof(Person.Id))
                .Build();

            var parser = model.CreateUriParser("/persons?$filter=dateOfBirth lt 1980-01-01");
            Assert.IsNotNull(parser);

            var filter = parser.ParseFilter();
            Assert.IsNotNull(filter);

            var count = parser.ParseCount();
            Assert.IsNull(count);
        }
    }
}