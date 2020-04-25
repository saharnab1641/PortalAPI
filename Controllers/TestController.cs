/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using PortalAPI.Models;
using System.Text.Json;
using Newtonsoft.Json;

namespace PortalAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private static readonly string EndpointUri = "https://codesizzlercsdb.documents.azure.com:443";
        private static readonly string PrimaryKey = "gJZB3746gfqEVhwL5uFj85k28iPdbAc4prX3W9vxbSs9ChQmSy2DGtwp9GDw79UA5kMFLSQ9AFPs4TLDwCM2oQ==";
        private CosmosClient cosmosClient;
        private Database Qdatabase;
        private Container Qcontainer;

        // The name of the database and container we will create
        private string QdatabaseId = "QuestionDB";
        private string QcontainerId = "QuestionContainer";

        [HttpGet("TestRoute")]
        public async Task<IActionResult> TestRoute()
        {
            cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);
            Qdatabase = await CreateDatabaseAsync(QdatabaseId);
            Qcontainer = await CreateContainerAsync(Qdatabase, QcontainerId, "id");

            QueryDefinition query = new QueryDefinition("select * from questions where questions.type=\"mcq\"");
            FeedIterator<dynamic> resultSet = Qcontainer.GetItemQueryIterator<dynamic>(
            query,
            requestOptions: new QueryRequestOptions()
            {
                MaxItemCount = 10
            });

            List<dynamic> questions = new List<dynamic>();
            while (resultSet.HasMoreResults)
            {
                FeedResponse<dynamic> response = await resultSet.ReadNextAsync();
                dynamic question = response.First();
                if (response.Diagnostics != null)
                {
                    Console.WriteLine($" Diagnostics {response.Diagnostics.ToString()}");
                }

                foreach (var item in response)
                {
                    questions.Add(item);
                }
            }
            MCQModel mcq = JsonConvert.DeserializeObject<MCQModel>(JsonConvert.SerializeObject(questions.ElementAt(0)));


            return Ok(new { mcq });
        }

        private async Task<Database> CreateDatabaseAsync(string dbId)
        {
            // Create a new database
            Database db = await cosmosClient.CreateDatabaseIfNotExistsAsync(dbId);
            Console.WriteLine("Created Database: {0}\n", db.Id);
            return db;
        }

        private async Task<Container> CreateContainerAsync(Database db, string cId, string partitionString)
        {
            // Create a new container
            Container cnt = await db.CreateContainerIfNotExistsAsync(cId, "/" + partitionString);
            Console.WriteLine("Created Container: {0}\n", cnt.Id);
            return cnt;
        }
    }
}*/