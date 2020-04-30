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
using iText.Forms;
using iText.Kernel.Pdf;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.Auth;
using System.Text;
using System.IO;
using System.Security.Cryptography;

namespace PortalAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuestionController : ControllerBase
    {
        private readonly IConfiguration _config;
        public QuestionController(IConfiguration config)
        {
            _config = config;
        }

        private static readonly string EndpointUri = "https://codesizzlercsdb.documents.azure.com:443";
        private static readonly string PrimaryKey = "gJZB3746gfqEVhwL5uFj85k28iPdbAc4prX3W9vxbSs9ChQmSy2DGtwp9GDw79UA5kMFLSQ9AFPs4TLDwCM2oQ==";
        private static readonly string AccountKey = "zdUViL51nXIZ5ILevk1zFpCqFv6qaf8wsH6sIwMvbPPMGdGHFYM37s3/7zMhQ4VE8xoAmTeAO5RlS70jKu8+4A==";
        private static readonly string AccountName = "examportaldocs";
        private static readonly string TemplateSrc = "https://examportaldocs.blob.core.windows.net/template/certificatetemplate.pdf";

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
        private readonly string QdatabaseId = "QuestionDB";
        private readonly string QcontainerId = "QuestionContainer";

        private readonly string UdatabaseId = "RegUsersDB";
        private readonly string UcontainerId = "RegUsersContainer";

        private readonly string RdatabaseId = "ReportDB";
        private readonly string RcontainerId = "ReportContainer";

        private readonly string EdatabaseId = "ExamDB";
        private readonly string EcontainerId = "ExamContainer";

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
            cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);
            Udatabase = await CreateDatabaseAsync(UdatabaseId);
            Ucontainer = await CreateContainerAsync(Udatabase, UcontainerId, "id");
            Udatabase = await CreateDatabaseAsync(UdatabaseId);
            Ucontainer = await CreateContainerAsync(Udatabase, UcontainerId, "id");

            var currentUser = HttpContext.User;
            var userId = currentUser.Claims.FirstOrDefault(c => c.Type == "id").Value;

            ItemResponse<UserModel> UserResponse = await Ucontainer.ReadItemAsync<UserModel>(userId, new PartitionKey(userId));

            return Ok(new { enrolled = UserResponse.Resource.Enrolled });
        }

        [HttpGet("GetReports")]
        [EnableCors("AllPolicy")]
        [Authorize]
        public async Task<IActionResult> GetReports()
        {
            cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);
            Udatabase = await CreateDatabaseAsync(UdatabaseId);
            Ucontainer = await CreateContainerAsync(Udatabase, UcontainerId, "id");
            Rdatabase = await CreateDatabaseAsync(RdatabaseId);
            Rcontainer = await CreateContainerAsync(Rdatabase, RcontainerId, "id");

            var currentUser = HttpContext.User;
            var userId = currentUser.Claims.FirstOrDefault(c => c.Type == "id").Value;

            ItemResponse<UserModel> UserResponse = await Ucontainer.ReadItemAsync<UserModel>(userId, new PartitionKey(userId));
            List<ReportModel> reports = new List<ReportModel>();
            foreach (string itemToRead in UserResponse.Resource.Reports)
            {
                ItemResponse<ReportModel> TransResponse = await Rcontainer.ReadItemAsync<ReportModel>(itemToRead, new PartitionKey(itemToRead));
                reports.Add(TransResponse.Resource);
            }

            return Ok(new { reports });
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
                if (UserResponse.Resource.Enrolled[exam]) throw new Exception("Exam already given.");
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
            Udatabase = await CreateDatabaseAsync(UdatabaseId);
            Ucontainer = await CreateContainerAsync(Udatabase, UcontainerId, "id");
            Edatabase = await CreateDatabaseAsync(EdatabaseId);
            Econtainer = await CreateContainerAsync(Edatabase, EcontainerId, "id");
            Rdatabase = await CreateDatabaseAsync(RdatabaseId);
            Rcontainer = await CreateContainerAsync(Rdatabase, RcontainerId, "id");
            ItemResponse<ExamModel> ExamResponse;

            try
            {
                ExamResponse = await Econtainer.ReadItemAsync<ExamModel>(examID, new PartitionKey(examID));
            }
            catch (Exception e)
            {
                return BadRequest(new { error = "No such exam found." });
            }


            var currentUser = HttpContext.User;
            var userId = currentUser.Claims.FirstOrDefault(c => c.Type == "id").Value;

            ItemResponse<UserModel> UserResponse = await Ucontainer.ReadItemAsync<UserModel>(userId, new PartitionKey(userId));

            if (!UserResponse.Resource.Enrolled.ContainsKey(examID))
            {
                return BadRequest(new { error = "User does not seem to be enrolled in course." });
            }
            if (UserResponse.Resource.Enrolled[examID])
            {
                return BadRequest(new { error = "User has already given the exam." });
            }


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
                                    else score -= wrongWeight;

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

            try
            {
                // Read the item to see if it exists.  
                ItemResponse<ReportModel> UserReport = await Rcontainer.ReadItemAsync<ReportModel>(report.Id, new PartitionKey(report.Id));
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                string fileName = "test" + GenerateRandomString(5, "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789") + "_" + buildDateTimeStamp() + ".pdf";
                MemoryStream writeStream = new MemoryStream();
                PdfWriter pdfWriter = new PdfWriter(writeStream);
                pdfWriter.SetCloseStream(false);
                PdfDocument pdfDoc = new PdfDocument(new PdfReader(TemplateSrc), pdfWriter);
                PdfAcroForm form = PdfAcroForm.GetAcroForm(pdfDoc, true);

                form.GetField("name").SetValue(UserResponse.Resource.Name).SetReadOnly(true);
                form.GetField("email").SetValue(UserResponse.Resource.Id).SetReadOnly(true);
                form.GetField("address").SetValue(UserResponse.Resource.Address).SetReadOnly(true);
                form.GetField("exam").SetValue(ExamResponse.Resource.ExamType + " - " + ExamResponse.Resource.ExamSerial).SetReadOnly(true);
                form.GetField("dateeval").SetValue(report.PassingDate).SetReadOnly(true);
                form.GetField("score").SetValue(score.ToString()).SetReadOnly(true);


                pdfDoc.Close();

                StorageCredentials storageCredentials = new StorageCredentials(AccountName, AccountKey);
                CloudStorageAccount storageAccount = new CloudStorageAccount(storageCredentials, true);
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference("certificates");
                await container.CreateIfNotExistsAsync();
                await container.SetPermissionsAsync(new BlobContainerPermissions()
                {
                    PublicAccess = BlobContainerPublicAccessType.Blob
                });
                CloudBlockBlob blob = container.GetBlockBlobReference(fileName);
                blob.Properties.ContentType = "application/pdf";
                writeStream.Position = 0;
                await blob.UploadFromStreamAsync(writeStream);
                string html = "Dear User,<br><br>Your exam has been successfully conducted. The transcript has been attached with this mail.";
                Email(html, UserResponse.Resource.Id, "Exam Report", writeStream);

                var blobUri = blob.Uri.ToString();
                report.ReportURL = blobUri;
                writeStream.Close();

                ItemResponse<ReportModel> UserReport = await Rcontainer.CreateItemAsync(report, new PartitionKey(report.Id));
                await Ucontainer.ReplaceItemAsync(UserResponse.Resource, UserResponse.Resource.Id, new PartitionKey(UserResponse.Resource.Id));
            }
            //Return report json

            return Ok(new { report });
        }

        //----------------------------Helper Methods----------------------------//

        private string GenerateRandomString(int length, string allowedChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*?=_-")
        {
            if (length < 0) throw new ArgumentOutOfRangeException("length", "length cannot be less than zero.");
            if (string.IsNullOrEmpty(allowedChars)) throw new ArgumentException("allowedChars may not be empty.");

            const int byteSize = 0x100;
            var allowedCharSet = new HashSet<char>(allowedChars).ToArray();
            if (byteSize < allowedCharSet.Length) throw new ArgumentException(string.Format("allowedChars may contain no more than {0} characters.", byteSize));

            using (var rng = RandomNumberGenerator.Create())
            {
                var result = new StringBuilder();
                var buf = new byte[128];
                while (result.Length < length)
                {
                    rng.GetBytes(buf);
                    for (var i = 0; i < buf.Length && result.Length < length; ++i)
                    {
                        var outOfRangeStart = byteSize - (byteSize % allowedCharSet.Length);
                        if (outOfRangeStart <= buf[i]) continue;
                        result.Append(allowedCharSet[buf[i] % allowedCharSet.Length]);
                    }
                }
                return result.ToString();
            }
        }

        private string buildDateTimeStamp()

        {
            StringBuilder sb = new StringBuilder();
            DateTime currentDate = DateTime.Now;
            sb.Append(currentDate.Year.ToString());
            if (currentDate.Month.ToString().Length == 1)
                sb.Append("0" + currentDate.Month.ToString());
            else
                sb.Append(currentDate.Month.ToString());
            if (currentDate.Day.ToString().Length == 1)
                sb.Append("0" + currentDate.Day.ToString());
            else
                sb.Append(currentDate.Day.ToString());
            if (currentDate.Hour.ToString().Length == 1)
                sb.Append("0" + currentDate.Hour.ToString());
            else
                sb.Append(currentDate.Hour.ToString());
            if (currentDate.Minute.ToString().Length == 1)
                sb.Append("0" + currentDate.Minute.ToString());
            else
                sb.Append(currentDate.Minute.ToString());
            if (currentDate.Second.ToString().Length == 1)

                sb.Append("0" + currentDate.Second.ToString());
            else
                sb.Append(currentDate.Second.ToString());
            return sb.ToString();
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

        public static void Email(string htmlString, string email, string subject, MemoryStream ms)
        {
            try
            {
                MailMessage message = new MailMessage();
                SmtpClient smtp = new SmtpClient();
                message.From = new MailAddress("saharnab.test@gmail.com");
                message.To.Add(new MailAddress(email));
                message.Subject = subject;
                message.IsBodyHtml = true; //to make message body as html  
                message.Body = htmlString;
                if (ms != null)
                {
                    ms.Position = 0;
                    message.Attachments.Add(new Attachment(ms, "Test.pdf", "application/pdf"));
                }
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