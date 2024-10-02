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
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Denomica.OData.Tests
{
    [TestClass]
    public class CosmosTests
    {
        private static ContainerProxy Proxy = null!;
        private static Container Container = null!;

        [ClassInitialize]
        public static async Task ClassInit(TestContext context)
        {
            var connectionString = $"{context.Properties["connectionString"]}";
            var databaseId = $"{context.Properties["databaseId"]}";
            var containerId = $"{context.Properties["containerId"]}";

            var client = new CosmosClient(connectionString, new CosmosClientOptions
            {
                SerializerOptions = new CosmosSerializationOptions { PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase }
            });
            var database = client.GetDatabase(databaseId);
            var container = database.GetContainer(containerId);

            try
            {
                var deleteResponse = await container.DeleteContainerAsync();
            }
            catch { }

            var containerResponse = await database.CreateContainerAsync(new ContainerProperties { Id = containerId, PartitionKeyPath = "/partition" });
            Container = database.GetContainer(containerId);
            Proxy = new ContainerProxy(database.GetContainer(containerId), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }

        [TestInitialize]
        public async Task TestInit()
        {
            var items = await Proxy.QueryItemsAsync<Document>(new QueryDefinition("select * from c")).ToListAsync();
            foreach(var itm in items)
            {
                await Proxy.DeleteItemAsync(itm.Id, itm.Partition);
            }
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

        [TestMethod]
        public void CreateQueryDefinition09()
        {
            var builder = new EdmModelBuilder()
                .AddEntityType<Person>("Id", "persons")
                .Build();

            var q1 = builder
                .CreateUriParser("/persons?$filter=dateOfBirth lt 1980-01-01 and hometown eq 'Helsinki' or hometown eq 'Tampere'")
                .CreateQueryDefinition();

            var q2 = builder
                .CreateUriParser("/persons?$filter=dateOfBirth lt 1980-01-01 and (hometown eq 'Helsinki' or hometown eq 'Tampere')")
                .CreateQueryDefinition();

            Assert.AreNotEqual(q1.QueryText, q2.QueryText);
        }

        [TestMethod]
        public void CreateQueryDefinition10()
        {
            var builder = new EdmModelBuilder()
                .AddEntityType<Person>("Id", "persons")
                .Build();

            var q1 = builder
                .CreateUriParser("/persons?$filter=dateOfBirth lt 1980-01-01 and hometown eq 'Helsinki' or hometown eq 'Tampere' or hometown eq 'Turku'")
                .CreateQueryDefinition();

            var q2 = builder
                .CreateUriParser("/persons?$filter=dateOfBirth lt 1980-01-01 and (hometown eq 'Helsinki' or hometown eq 'Tampere' or hometown eq 'Turku')")
                .CreateQueryDefinition();

            Assert.AreNotEqual(q1.QueryText, q2.QueryText);
        }

        [TestMethod]
        public async Task CreateQueryDefinition11()
        {
            var p1 = await Proxy.UpsertItemAsync(new Person { LastName = "Smith", DateOfBirth = new DateTime(1979, 9, 30), Hometown = "Helsinki" });
            var p2 = await Proxy.UpsertItemAsync(new Person { LastName = "Spencer", DateOfBirth = new DateTime(1980, 1, 4), Hometown = "Helsinki" });
            var p3 = await Proxy.UpsertItemAsync(new Person { LastName = "Smith", DateOfBirth = new DateTime(1975, 5, 31), Hometown = "Tampere" });
            var p4 = await Proxy.UpsertItemAsync(new Person { LastName = "Spencer", DateOfBirth = new DateTime(1990, 7, 15), Hometown = "Tampere" });
            var p5 = await Proxy.UpsertItemAsync(new Person { DateOfBirth = new DateTime(1977, 6, 14), Hometown = "Turku" });

            var query = new EdmModelBuilder()
                .AddEntityType<Person>("Id", "persons")
                .Build()
                .CreateUriParser("/persons?$filter=dateOfBirth lt 1980-01-01 and (hometown eq 'Helsinki' or hometown eq 'Tampere')")
                .CreateQueryDefinition();

            var results = await this.RunQueryAsync<Person>(query).ToListAsync();
            Assert.AreEqual(2, results.Count);
            Assert.IsTrue(results.Any(x => x.Id == p1.Id));
            Assert.IsTrue(results.Any(x => x.Id == p3.Id));
            Assert.IsFalse(results.Any(x => x.Id == p2.Id));
            Assert.IsFalse(results.Any(x => x.Id == p4.Id));
            Assert.IsFalse(results.Any(x => x.Id == p5.Id));
        }

        [TestMethod]
        public async Task CreateQueryDefinition12()
        {
            var e1 = await Proxy.UpsertItemAsync(new Employee { DailyWorkingTime = TimeSpan.FromHours(7.5) });
            var e2 = await Proxy.UpsertItemAsync(new Employee { DailyWorkingTime = TimeSpan.FromHours(4) });

            var query = new EdmModelBuilder()
                .AddEntityType<Employee>("Id", "employees")
                .Build()
                .CreateUriParser("/employees?$filter=dailyWorkingTime ge duration'PT07H30M00S'")
                .CreateQueryDefinition();

            var results = await this.RunQueryAsync<Employee>(query).ToListAsync();
            Assert.AreEqual(1, results.Count);
            Assert.IsTrue(results.Any(x => x.Id == e1.Id));
            Assert.IsFalse(results.Any(x => x.Id == e2.Id));
        }

        [TestMethod]
        public void CreateQueryDefinition13()
        {
            // Create a query definition using a URI parser that filters on a property definied in a base class of the entity type specified on the URI parser.
            var uriParser = new EdmModelBuilder()
                .AddEntityType<Employee>("Id", "employees")
                .Build()
                .CreateUriParser("/employees?$filter=hometown ne 'Helsinki'");

            var queryDef = uriParser.CreateQueryDefinition();
        }

        [TestMethod]
        public void CreateQueryDefinition14()
        {
            var uriParser = new EdmModelBuilder()
                .AddEntityType<Employee>("Id", "employees")
                .Build()
                .CreateUriParser("/employees?$filter=gender eq -1");

            var queryDef = uriParser.CreateQueryDefinition();
        }

        [TestMethod]
        public void CreateQueryDefinition15()
        {
            var uriParser = new EdmModelBuilder()
                .AddEntityType<Person>("Id", "persons")
                .Build()
                .CreateUriParser("/persons?$filter=gender eq -1");

            var queryDef = uriParser.CreateQueryDefinition();
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

        private IAsyncEnumerable<T> RunQueryAsync<T>(QueryDefinition query)
        {
            Assert.IsNotNull(query);

            try
            {
                var results = Proxy.QueryItemsAsync<T>(query);
                return results;
            }
            catch(Exception ex)
            {
                throw new Exception($"The query '{query.QueryText}' threw an exception when executing against Cosmos DB.", ex);
            }
        }
    }
}
