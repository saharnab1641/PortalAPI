using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using PortalAPI.Models;

namespace PortalAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuestionController : ControllerBase
    {
        private IConfiguration _config;
        public QuestionController(IConfiguration config)
        {
            _config = config;
        }

        private static readonly string EndpointUri = "https://codesizzlercsdb.documents.azure.com:443";
        private static readonly string PrimaryKey = "gJZB3746gfqEVhwL5uFj85k28iPdbAc4prX3W9vxbSs9ChQmSy2DGtwp9GDw79UA5kMFLSQ9AFPs4TLDwCM2oQ==";
        private CosmosClient cosmosClient;
        private Database Qdatabase;
        private Container Qcontainer;
        private Database Udatabase;
        private Container Ucontainer;
        private Database Rdatabase;
        private Container Rcontainer;
        private Database Edatabase;
        private Container Econtainer;


        // The name of the database and container we will create
        private string QdatabaseId = "QuestionDB";
        private string QcontainerId = "QuestionContainer";

        private string UdatabaseId = "RegUsersDB";
        private string UcontainerId = "RegUsersContainer";

        private string RdatabaseId = "ReportDB";
        private string RcontainerId = "ReportContainer";

        private string EdatabaseId = "ExamDB";
        private string EcontainerId = "ExamContainer";

        [HttpPost("AddExam")]
        [EnableCors("AllPolicy")]
        /*[Authorize]*/
        public async Task<IActionResult> AddExam([FromForm]string info)
        {
            cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);
            Edatabase = await CreateDatabaseAsync(EdatabaseId);
            Econtainer = await CreateContainerAsync(Edatabase, EcontainerId, "id");

            ExamModel exam = JsonConvert.DeserializeObject<ExamModel>(info);
            exam.Id = Guid.NewGuid().ToString();

            try
            {
                await Econtainer.CreateItemAsync(exam, new PartitionKey(exam.Id));
            }
            catch (Exception e)
            {
                return BadRequest(new { error = "There was an error adding to database. Maybe item already exists." });
            }
            return Ok(new { exam });
        }

        [HttpGet("GetAllExam")]
        [EnableCors("AllPolicy")]
        /*[Authorize]*/
        public async Task<IActionResult> GetAllExam()
        {
            Dictionary<string, List<ExamModel>> exams = await GetExamList();

            return Ok(new { exams });
        }

        [HttpGet("GetEnrolled")]
        [EnableCors("AllPolicy")]
        [Authorize]
        public async Task<IActionResult> GetEnrolled()
        {
            var currentUser = HttpContext.User;
            var userId = currentUser.Claims.FirstOrDefault(c => c.Type == "id").Value;

            ItemResponse<UserModel> UserResponse = await Ucontainer.ReadItemAsync<UserModel>(userId, new PartitionKey(userId));

            return Ok(new { enrolled = UserResponse.Resource.Enrolled });
        }

        [HttpPost("AddQuestion")]
        [EnableCors("AllPolicy")]
        /*[Authorize]*/
        public async Task<string> AddQuestion([FromForm]string type, [FromForm]string info)
        {
            cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);
            Qdatabase = await CreateDatabaseAsync(QdatabaseId);
            Qcontainer = await CreateContainerAsync(Qdatabase, QcontainerId, "id");

            string response;

            switch (type)
            {
                case "mcq":
                    {
                        MCQModel questionDetails = JsonConvert.DeserializeObject<MCQModel>(info);
                        questionDetails.Id = Guid.NewGuid().ToString();
                        response = await AddHelper(questionDetails);
                        break;
                    }
                case "arrange":
                    {
                        ArrangeModel questionDetails = JsonConvert.DeserializeObject<ArrangeModel>(info);
                        questionDetails.Id = Guid.NewGuid().ToString();
                        response = await AddHelper(questionDetails);
                        break;
                    }
                case "dnd":
                    {
                        DnDModel questionDetails = JsonConvert.DeserializeObject<DnDModel>(info);
                        questionDetails.Id = Guid.NewGuid().ToString();
                        response = await AddHelper(questionDetails);
                        break;
                    }
                case "select":
                    {
                        SelectModel questionDetails = JsonConvert.DeserializeObject<SelectModel>(info);
                        questionDetails.Id = Guid.NewGuid().ToString();
                        response = await AddHelper(questionDetails);
                        break;
                    }
                case "tabular":
                    {
                        TabularModel questionDetails = JsonConvert.DeserializeObject<TabularModel>(info);
                        questionDetails.Id = Guid.NewGuid().ToString();
                        response = await AddHelper(questionDetails);
                        break;
                    }
                default:
                    {
                        response = "Unable to add type.";
                        break;
                    }
            }
            return response;
        }

        [HttpPost("GetAllQuestion")]
        [EnableCors("AllPolicy")]
        [Authorize]
        public async Task<IActionResult> GetAllQuestion([FromForm]string exam)
        {
            cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);
            Edatabase = await CreateDatabaseAsync(EdatabaseId);
            Econtainer = await CreateContainerAsync(Edatabase, EcontainerId, "id");
            Udatabase = await CreateDatabaseAsync(UdatabaseId);
            Ucontainer = await CreateContainerAsync(Udatabase, UcontainerId, "id");
            var currentUser = HttpContext.User;
            var userId = currentUser.Claims.FirstOrDefault(c => c.Type == "id").Value;
            ItemResponse<UserModel> UserResponse;


            try
            {
                await Econtainer.ReadItemAsync<ExamModel>(exam, new PartitionKey(exam));
                UserResponse = await Ucontainer.ReadItemAsync<UserModel>(userId, new PartitionKey(userId));
                if (!UserResponse.Resource.Enrolled.ContainsKey(exam)) throw new Exception("User not enrolled in exam.");
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return NotFound(new { error = "Database item not found." });
            }
            catch (Exception e)
            {
                return BadRequest(new { error = e.Message });
            }


            AllModel questions = await GetQuestionList(exam, true);

            return Ok(new { questions, examID = exam });
        }

        [HttpPost("Evaluate")]
        [EnableCors("AllPolicy")]
        [Authorize]
        public async Task<IActionResult> Evaluate([FromForm]List<EvaluateModel> answers, [FromForm]string examID)
        {
            /*var currTime = DateTime.Now;
            if (currTime.Subtract(prevTime).TotalMinutes.Equals())
            {

            }*/
            cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);
            var currentUser = HttpContext.User;

            int correctWeight = 1;
            int wrongWeight = 0;
            int score = 0;

            AllModel questions = await GetQuestionList(examID, false);


            foreach (EvaluateModel answer in answers)
            {
                switch (answer.questionType)
                {
                    case "mcq":
                        {
                            foreach (MCQModel q in questions.McqType)
                            {
                                if (q.Id.Equals(answer.questionId))
                                {
                                    if (answer.optionchoice["optionchoice"].Equals(q.Solutions))
                                    {
                                        score += correctWeight;
                                    }
                                    else score -= wrongWeight;
                                    break;
                                }
                            }
                            break;
                        }
                    case "arrange":
                        {
                            foreach (ArrangeModel q in questions.ArrangeType)
                            {
                                if (q.Id.Equals(answer.questionId))
                                {
                                    HashSet<string> recSol = new HashSet<string>(answer.optionchoice.Values);
                                    HashSet<string> compSol = new HashSet<string>(q.Solutions);

                                    if (recSol.SetEquals(compSol))
                                    {
                                        score += correctWeight;
                                    }
                                    break;
                                }
                            }
                            break;
                        }
                    case "select":
                        {
                            foreach (SelectModel q in questions.SelectType)
                            {
                                if (q.Id.Equals(answer.questionId))
                                {
                                    bool corr = true;
                                    foreach (KeyValuePair<string, string> item in q.Solutions)
                                    {
                                        if (!item.Value.Equals(answer.optionchoice[item.Key]))
                                        {
                                            score -= wrongWeight;
                                            corr = false;
                                            break;
                                        }
                                    }
                                    if (corr) score += correctWeight;
                                    break;
                                }
                            }
                            break;
                        }
                    case "dnd":
                        {
                            foreach (DnDModel q in questions.DnDType)
                            {
                                if (q.Id.Equals(answer.questionId))
                                {
                                    bool corr = true;
                                    foreach (KeyValuePair<string, string> item in q.Solutions)
                                    {
                                        if (!item.Value.Equals(answer.optionchoice[item.Key]))
                                        {
                                            score -= wrongWeight;
                                            corr = false;
                                            break;
                                        }
                                    }
                                    if (corr) score += correctWeight;
                                    break;
                                }
                            }
                            break;
                        }
                    case "tabular":
                        {
                            foreach (TabularModel q in questions.TabularType)
                            {
                                if (q.Id.Equals(answer.questionId))
                                {
                                    bool corr = true;
                                    foreach (KeyValuePair<string, string> item in q.Solutions)
                                    {
                                        if (!item.Value.Equals(answer.optionchoice[item.Key]))
                                        {
                                            score -= wrongWeight;
                                            corr = false;
                                            break;
                                        }
                                    }
                                    if (corr) score += correctWeight;
                                    break;
                                }
                            }
                            break;
                        }
                }
            }

            Udatabase = await CreateDatabaseAsync(UdatabaseId);
            Ucontainer = await CreateContainerAsync(Udatabase, UcontainerId, "id");

            Edatabase = await CreateDatabaseAsync(EdatabaseId);
            Econtainer = await CreateContainerAsync(Edatabase, UcontainerId, "id");

            ItemResponse<ExamModel> ExamResponse = await Econtainer.ReadItemAsync<ExamModel>(examID, new PartitionKey(examID));

            var userId = currentUser.Claims.FirstOrDefault(c => c.Type == "id").Value;

            ItemResponse<UserModel> UserResponse = await Ucontainer.ReadItemAsync<UserModel>(userId, new PartitionKey(userId));

            if (!UserResponse.Resource.Enrolled.ContainsKey(examID))
            {
                return BadRequest(new { error = "User does not seem to be enrolled in course." });
            }

            ReportModel report = new ReportModel
            {
                Id = Guid.NewGuid().ToString(),
                Name = UserResponse.Resource.Name,
                Email = UserResponse.Resource.Id,
                Address = UserResponse.Resource.Address,
                ExamID = examID,
                PassingDate = DateTime.Today.ToString(),
                Scored = score
            };

            UserResponse.Resource.Reports.Add(report.Id);
            UserResponse.Resource.Enrolled[examID] = true;

            //Save report

            Rdatabase = await CreateDatabaseAsync(RdatabaseId);
            Rcontainer = await CreateContainerAsync(Rdatabase, RcontainerId, "id");

            try
            {
                // Read the item to see if it exists.  
                ItemResponse<ReportModel> UserReport = await Rcontainer.ReadItemAsync<ReportModel>(report.Id, new PartitionKey(report.Id));
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {

                ItemResponse<ReportModel> UserReport = await Rcontainer.CreateItemAsync(report, new PartitionKey(report.Id));
                await Ucontainer.ReplaceItemAsync(UserResponse.Resource, UserResponse.Resource.Id, new PartitionKey(UserResponse.Resource.Id));
                Console.WriteLine("Created item in database with id: {0} Operation consumed {1} RUs.\n", UserReport.Resource.Id, UserReport.RequestCharge);
                string htmlMail = "Dear User,<br><br>This is a detailed report for the exam you have taken recently.<br><br>" +
                "<b>Exam:<b> " + ExamResponse.Resource.ExamType + "-" + ExamResponse.Resource.ExamSerial +
                "<br><b>Passing Date:<b> " + report.PassingDate +
                "<br><b>Score:<b> " + report.Scored;
                Email(htmlMail, report.Email);
            }
            //Return report json

            return Ok(new { report });
        }

        //----------------------------Helper Methods----------------------------//

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

        private async Task<string> AddHelper(dynamic questionDetails)
        {
            try
            {
                await Qcontainer.CreateItemAsync(questionDetails, new PartitionKey(questionDetails.Id));
            }
            catch (Exception e)
            {
                return e.ToString();
            }
            return "Added";
        }

        public static void Email(string htmlString, string email)
        {
            try
            {
                MailMessage message = new MailMessage();
                SmtpClient smtp = new SmtpClient();
                message.From = new MailAddress("saharnab.test@gmail.com");
                message.To.Add(new MailAddress(email));
                message.Subject = "Exam Report";
                message.IsBodyHtml = true; //to make message body as html  
                message.Body = htmlString;
                smtp.Port = 587;
                smtp.Host = "smtp.gmail.com"; //for gmail host  
                smtp.EnableSsl = true;
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new NetworkCredential("saharnab.test@gmail.com", "testpassword123!");
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtp.Send(message);
            }
            catch (Exception) { }
        }

        private async Task<AllModel> GetQuestionList(string examID, bool hideSolution = true)
        {
            cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);
            Qdatabase = await CreateDatabaseAsync(QdatabaseId);
            Qcontainer = await CreateContainerAsync(Qdatabase, QcontainerId, "id");

            QueryDefinition query = new QueryDefinition("select * from questions where questions.examid=\"" + examID + "\"");
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
                    if (hideSolution) item.Solutions = null;
                    questions.Add(item);
                }
            }

            //questions.ElementAt(0).type;
            AllModel questionModel = new AllModel();
            foreach (var item in questions)
            {
                switch ((string)item.qtype)
                {
                    case "mcq":
                        {
                            MCQModel q = JsonConvert.DeserializeObject<MCQModel>(JsonConvert.SerializeObject(item));
                            questionModel.McqType.Add(q);
                            break;
                        }
                    case "arrange":
                        {
                            ArrangeModel q = JsonConvert.DeserializeObject<ArrangeModel>(JsonConvert.SerializeObject(item));
                            questionModel.ArrangeType.Add(q);
                            break;
                        }
                    case "dnd":
                        {
                            DnDModel q = JsonConvert.DeserializeObject<DnDModel>(JsonConvert.SerializeObject(item));
                            questionModel.DnDType.Add(q);
                            break;
                        }
                    case "select":
                        {
                            SelectModel q = JsonConvert.DeserializeObject<SelectModel>(JsonConvert.SerializeObject(item));
                            questionModel.SelectType.Add(q);
                            break;
                        }
                    case "tabular":
                        {
                            TabularModel q = JsonConvert.DeserializeObject<TabularModel>(JsonConvert.SerializeObject(item));
                            questionModel.TabularType.Add(q);
                            break;
                        }
                    default: break;
                }
            }

            return questionModel;
        }

        private async Task<Dictionary<string, List<ExamModel>>> GetExamList()
        {
            cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);
            Edatabase = await CreateDatabaseAsync(EdatabaseId);
            Econtainer = await CreateContainerAsync(Edatabase, EcontainerId, "id");

            QueryDefinition query = new QueryDefinition("select * from exams");
            FeedIterator<dynamic> resultSet = Econtainer.GetItemQueryIterator<dynamic>(
            query,
            requestOptions: new QueryRequestOptions()
            {
                MaxItemCount = 10
            });

            List<dynamic> exams = new List<dynamic>();
            while (resultSet.HasMoreResults)
            {
                FeedResponse<dynamic> response = await resultSet.ReadNextAsync();
                if (response.Diagnostics != null)
                {
                    Console.WriteLine($" Diagnostics {response.Diagnostics.ToString()}");
                }

                foreach (var item in response)
                {
                    exams.Add(item);
                }
            }
            Dictionary<string, List<ExamModel>> examColl = new Dictionary<string, List<ExamModel>>();
            foreach (var item in exams)
            {
                ExamModel q = JsonConvert.DeserializeObject<ExamModel>(JsonConvert.SerializeObject(item));
                if (!examColl.ContainsKey(q.ExamType))
                {
                    examColl.Add(q.ExamType, new List<ExamModel>());
                }
                examColl[q.ExamType].Add(q);
            }

            return examColl;
        }
    }
}