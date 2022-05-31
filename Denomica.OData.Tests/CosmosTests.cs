using Denomica.OData.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Denomica.OData.Cosmos.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Cosmos;
using Denomica.Cosmos.Extensions;
using System.Text.Json;

namespace Denomica.OData.Tests
{
    [TestClass]
    public class CosmosTests
    {
        private static ContainerProxy Proxy = null!;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            var connectionString = $"{context.Properties["connectionString"]}";
            var databaseId = $"{context.Properties["databaseId"]}";
            var containerId = $"{context.Properties["containerId"]}";

            var client = new CosmosClient(connectionString);
            var database = client.GetDatabase(databaseId);
            Proxy = new ContainerProxy(database.GetContainer(containerId), new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }

        [TestMethod]
        public async Task CreateQueryDefinition01()
        {
            var query = new EdmModelBuilder()
                .AddEntityType<Person>(nameof(Person.Id), "persons")
                .Build()
                .CreateUriParser("/persons?$select=firstName,lastName&$filter=dateOfBirth lt 1980-01-01&$orderby=dateOfBirth desc")
                .CreateQueryDefinition()
                ;

            await this.RunQueryAsync(query);
        }

        [TestMethod]
        public async Task CreateQueryDefinition02()
        {
            var query = new EdmModelBuilder()
                .AddEntityType<Person>(nameof(Person.Id), "persons")
                .Build()
                .CreateUriParser("/persons?$filter=(dateOfBirth ge 1970-01-01 and dateOfBirth lt 2000-01-01) or firstName eq 'Peter'")
                .CreateQueryDefinition();

            await this.RunQueryAsync(query);
        }

        [TestMethod]
        public async Task CreateQueryDefinition03()
        {
            var query = new EdmModelBuilder()
                .AddEntityType<Person>("Id", "persons")
                .Build()
                .CreateUriParser("/persons")
                .CreateQueryDefinition();

            await this.RunQueryAsync(query);
        }

        [TestMethod]
        public void CreateQueryDefinition04()
        {
            var query = new EdmModelBuilder()
                .AddEntityType<Person>("Id", "persons")
                .Build()
                .CreateUriParser("/persons?$orderby=lastName,dateOfBirth desc")
                .CreateQueryDefinition();

            Assert.AreEqual("SELECT * FROM c ORDER BY c.lastName,c.dateOfBirth desc", query.QueryText);
        }

        [TestMethod]
        public async Task CreateQueryDefinition05()
        {
            var query = new EdmModelBuilder()
                .AddEntityType<Person>("Id", "persons")
                .Build()
                .CreateUriParser("/persons?$orderby=dateOfBirth desc")
                .CreateQueryDefinition();

            await this.RunQueryAsync(query);
        }

        [TestMethod]
        public async Task CreateQueryDefinition06()
        {
            var query = new EdmModelBuilder()
                .AddEntityType<Person>("Id", "persons")
                .Build()
                .CreateUriParser("/persons?$filter=")
                .CreateQueryDefinition();

            await this.RunQueryAsync(query);
        }

        [TestMethod]
        public async Task CreateQueryDefinition07()
        {
            var query = new EdmModelBuilder()
                .AddEntityType<Person>("Id", "persons")
                .Build()
                .CreateUriParser("/persons?$filter=(lastName eq 'Doe' and firstName eq 'John') or firstName eq 'Peter'")
                .CreateQueryDefinition();

            await this.RunQueryAsync(query);
        }

        [TestMethod]
        public async Task CreateQueryDefinition08()
        {
            var query = new EdmModelBuilder()
                .AddEntityType<Person>("Id", "persons")
                .Build()
                .CreateUriParser("/persons?$select=id,firstName,lastName&$filter=lastName eq 'Burton'")
                .CreateQueryDefinition();

            Assert.AreEqual("SELECT c.id,c.firstName,c.lastName FROM c WHERE c.lastName = @p0", query.QueryText);
            await this.RunQueryAsync(query);
        }



        /// <summary>
        /// Runs the given query and asserts that no exception is thrown.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        private async Task RunQueryAsync(QueryDefinition query)
        {
            Assert.IsNotNull(query);

            try
            {
                var results = Proxy.QueryItemsAsync(query);
                var list = await results.ToListAsync();
            }
            catch(Exception ex)
            {
                throw new Exception($"The query '{query.QueryText}' threw an exception when executing against Cosmos DB.", ex);
            }
        }
    }
}
