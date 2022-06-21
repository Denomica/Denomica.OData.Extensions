using System;
using Denomica.OData.Extensions;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using Microsoft.OData.ModelBuilder;
using System.Collections.Generic;
using System.Reflection;

namespace Denomica.OData.Tests
{
    [TestClass]
    public class ODataTests
    {

        [TestMethod]
        public void AppendFilter01()
        {
            var uri = new Uri("https://host/path?$filter=foo eq 'bar'")
                .AppendFilter("age ge 18");

            Assert.AreEqual("https://host/path?$filter=foo eq 'bar' and age ge 18", uri.ToString());
        }

        [TestMethod]
        public void AppendFilter02()
        {
            var uri = new Uri("https://host:1234")
                .AppendFilter("foo eq 'bar'");

            Assert.AreEqual("https://host:1234/?$filter=foo eq 'bar'", uri.ToString());
        }




        [TestMethod]
        public void BuildModel01()
        {
            var model = new EdmModelBuilder()
                .AddEntityType(typeof(Person))
                .Build();

            var personType = (EdmEntityType)model.SchemaElements.First();
            Assert.IsNotNull(personType);

            var dobProperty = (EdmStructuralProperty)personType.DeclaredProperties.First(x => x.Name == nameof(Person.DateOfBirth).ToCamelCase());
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
        public void BuildModel04()
        {
            var model = new EdmModelBuilder()
                .AddEntityType<Person>()
                .Build();

            var person = model.FindEntityType<Person>();
            Assert.IsNotNull(person);
            
            foreach(var prop in typeof(Person).GetProperties())
            {
                var name = prop.Name.ToCamelCase();
                Assert.IsNotNull(person.FindProperty(name), $"The {person.FullName} entity type must have a declared property '{name}'.");
            }
        }

        [TestMethod]
        public void BuildModel05()
        {
            var model = new EdmModelBuilder()
                .AddEntityType<Employee>()
                .Build();

            var et = model.FindEntityType<Employee>();
            Assert.IsNotNull(et);
            var prop = et.FindProperty("lastDayOfEmployment");
            Assert.IsNotNull(prop, "Employee entity type must have a property 'lastDayOfEmployment'.");
        }

        [TestMethod]
        public void BuildModel06()
        {
            var model = new EdmModelBuilder()
                .AddEntityType<Person>()
                .AddEntityType<Employee>()
                .AddEntitySet<Employee>("employees")
                .AddEntityKey<Employee>(nameof(Employee.Id))
                .Build();

            var employeeType = model.FindEntityType<Employee>();
            Assert.IsNotNull(employeeType);

            foreach(var p in typeof(Employee).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                var name = p.Name.ToCamelCase();
                var prop = employeeType.FindProperty(name);
                Assert.IsNotNull(prop, $"There must be a property with the name '{name}' on the {employeeType.FullName} entity.");
            }
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

        [TestMethod]
        public void ParseUri02()
        {
            var model = new EdmModelBuilder()
                .AddEntityType<Person>()
                .AddEntityType<Employee>()
                .AddEntityKey<Employee>(nameof(Employee.Id))
                .AddEntitySet<Employee>("employees")
                .Build();

            var parser = model.CreateUriParser("/employees?$filter=firstDayOfEmployment gt 2000-01-01 and dateOfBirth gt 1980-01-01");
            Assert.IsNotNull(parser);

            var filter = parser.ParseFilter();
            Assert.IsNotNull(filter?.Expression);

            var orderBy = parser.ParseOrderBy();
            Assert.IsNull(orderBy);
        }

        [TestMethod]
        public void ParseUri03()
        {
            var model = new EdmModelBuilder()
                .AddEntityType<Person>(nameof(Person.Id), "persons")
                .Build();

            var parser = model.CreateUriParser("/persons?$filter=dateOfBirth lt 2000-01-01");
            var filter = parser.ParseFilter();
            Assert.IsNotNull(filter);
        }

        [TestMethod]
        public void ParseUri04()
        {
            var model = new EdmModelBuilder()
                .AddEntityType<Person>(nameof(Person.Id), "persons")
                .Build();
            var parser = model.CreateUriParser("/persons?$select=firstName,lastName&$filter=(dateOfBirth ge 1980-01-01 and dateOfBirth lt 2000-01-01) or firstName eq 'Peter'&$orderby=dateOfBirth desc");
            var select = parser.ParseSelectAndExpand();
            var filter = parser.ParseFilter();
            var orderBy = parser.ParseOrderBy();

            Assert.IsNotNull(select);
            Assert.IsNotNull(filter);
            Assert.IsNotNull(orderBy);
        }

        [TestMethod]
        public void ParseUri05()
        {
            var parser = new EdmModelBuilder()
                .AddEntityType<Person>("Id", "persons")
                .Build()
                .CreateUriParser("/persons?$orderby=lastName desc,firstName");

            var orderBy = parser.ParseOrderBy();
            Assert.IsNotNull(orderBy);
            Assert.IsNotNull(orderBy.ThenBy);

            var orderByList = orderBy.ToList();
            Assert.IsNotNull(orderByList);
            Assert.AreEqual(2, orderByList.Count);
        }

        [TestMethod]
        public void ParseUri06()
        {
            var parser = new EdmModelBuilder()
                .AddEntityType<Person>()
                .AddEntityKey<Person>("Id")
                .AddEntitySet<Person>("persons")
                .Build()
                .CreateUriParser("api/persons");

            Assert.IsNotNull(parser);

            var filter = parser.ParseFilter();
            Assert.IsNull(filter);
        }

        [TestMethod]
        public void ParseUri07()
        {
            var parser = new EdmModelBuilder()
                .AddEntityType<Person>("Id")
                .CreateUriParser<Person>(new Uri("https://app.host.com/api/organizations/1234/departments/5678/persons?$filter=dateOfBirth lt 1980-01-01"));

            var filter = parser.ParseFilter();
            Assert.IsNotNull(filter);
        }

        [TestMethod]
        public void ParseUri08()
        {
            var parser = new EdmModelBuilder()
                .AddEntityType<Person>("Id")
                .CreateUriParser<Person>("https://app.host.com/api/organizations/1234/departments/5678/persons?$filter=dateOfBirth ge 1971-01-01");

            var filter = parser.ParseFilter();
            Assert.IsNotNull(filter);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException), AllowDerivedTypes = false)]
        public void ParseUri09()
        {
            var parser = new EdmModelBuilder()
                .CreateUriParser<Person>("/");
        }

        [TestMethod]
        public void ParseUri10()
        {
            var parser = new EdmModelBuilder()
                .AddEntityType<Person>("Id")
                .CreateUriParser<Person>("/persons?$top=10&$skip=5");

            var top = parser.ParseTop();
            var skip = parser.ParseSkip();

            Assert.AreEqual(10, top);
            Assert.AreEqual(5, skip);
        }
    }
}