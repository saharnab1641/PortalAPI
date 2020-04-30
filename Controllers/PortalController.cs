using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using PortalAPI.Models;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using Microsoft.IdentityModel.Logging;
using Microsoft.AspNetCore.Cors;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;

namespace PortalAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PortalController : ControllerBase
    {
        private IConfiguration _config;
        public PortalController(IConfiguration config)
        {
            _config = config;
        }

        private static readonly string EndpointUri = "https://codesizzlercsdb.documents.azure.com:443";
        private static readonly string PrimaryKey = "gJZB3746gfqEVhwL5uFj85k28iPdbAc4prX3W9vxbSs9ChQmSy2DGtwp9GDw79UA5kMFLSQ9AFPs4TLDwCM2oQ==";
        private CosmosClient cosmosClient;
        private Database Udatabase;
        private Container Ucontainer;
        private Database Pdatabase;
        private Container Pcontainer;
        private Database Ndatabase;
        private Container Ncontainer;
        private Database Tdatabase;
        private Container Tcontainer;
        private Database Edatabase;
        private Container Econtainer;

        private string UdatabaseId = "RegUsersDB";
        private string UcontainerId = "RegUsersContainer";
        private string PdatabaseId = "PasswordChangeDB";
        private string PcontainerId = "PasswordChangeContainer";
        private string NdatabaseId = "NotificationDB";
        private string NcontainerId = "NotificationContainer";
        private string TdatabaseId = "TransactionDB";
        private string TcontainerId = "TransactionContainer";
        private string EdatabaseId = "ExamDB";
        private string EcontainerId = "ExamContainer";

        [HttpPost("RegisterUser")]
        [EnableCors("AllPolicy")]
        public async Task<string> RegisterUser([FromForm] string email, [FromForm] string name, [FromForm] string address)
        {
            cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);
            Udatabase = await CreateDatabaseAsync(UdatabaseId);
            Ucontainer = await CreateContainerAsync(Udatabase, UcontainerId, "id");
            string accessKey = GenerateRandomString(length: 10);

            //Set an expiry date for the exam 
            /*string expiry = DateTime.Today.AddDays(3).ToString();*/

            //To save to DB - email, expiry, generated Access code, Attempted

            UserModel user = new UserModel
            {
                Id = email,
                /*Expiry = expiry,*/
                AccessCode = accessKey,
                Address = address,
                Name = name
            };
            try
            {
                // Read the item to see if it exists.  
                ItemResponse<UserModel> UserResponse = await Ucontainer.ReadItemAsync<UserModel>(user.Id, new PartitionKey(user.Id));
                return "Item exists";
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {

                ItemResponse<UserModel> UserResponse = await Ucontainer.CreateItemAsync(user, new PartitionKey(user.Id));
                Console.WriteLine("Created item in database with id: {0} Operation consumed {1} RUs.\n", UserResponse.Resource.Id, UserResponse.RequestCharge);

                string htmlMail = "Dear User,<br><br>You have been registered for Azure Ninjas Exams. Please check below for the details.<br><br>" +
                              "<b>Access Code:<b> " + accessKey +
                              "<br><b>Code Expiry:<b> " + (user.Expiry == null ? "None" : user.Expiry);
                Email(htmlMail, email, "Registration Successful");
            }

            return accessKey + " " + (user.Expiry == null ? "None" : user.Expiry);
        }

        [HttpPost("LoginUser")]
        [EnableCors("AllPolicy")]
        public async Task<IActionResult> LoginUser([FromForm] string email, [FromForm] string accessKey)
        {
            //check if email and access key are correct

            //SELECT c.accesscode FROM c where c.id = '<email>'

            cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);
            Udatabase = await CreateDatabaseAsync(UdatabaseId);
            Ucontainer = await CreateContainerAsync(Udatabase, UcontainerId, "id");

            ItemResponse<UserModel> UserResponse;

            try
            {
                UserResponse = await Ucontainer.ReadItemAsync<UserModel>(email, new PartitionKey(email));
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return NotFound();
            }

            if (!UserResponse.Resource.AccessCode.Equals(accessKey)) return Unauthorized();

            //check if exam has not expired

            if (UserResponse.Resource.Expiry != null) { if (DateTime.Compare(DateTime.Today, Convert.ToDateTime(UserResponse.Resource.Expiry)) > 0) return Unauthorized(); }

            //login user via jwt controller in logincontroller

            return Login(UserResponse.Resource);
        }

        [HttpPost("ForgotPassword")]
        [EnableCors("AllPolicy")]
        public async Task<IActionResult> ForgotPassword([FromForm]string email)
        {
            cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);
            Udatabase = await CreateDatabaseAsync(UdatabaseId);
            Ucontainer = await CreateContainerAsync(Udatabase, UcontainerId, "id");
            Pdatabase = await CreateDatabaseAsync(PdatabaseId);
            Pcontainer = await CreateContainerAsync(Pdatabase, PcontainerId, "id");
            string token, hashedToken;
            try
            {
                // Read the item to see if it exists.  
                ItemResponse<UserModel> User = await Ucontainer.ReadItemAsync<UserModel>(email, new PartitionKey(email));
                token = GenerateRandomString(length: 20, allowedChars: "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789.~_-");
                hashedToken = ComputeSha256Hash(token);
                ForgotPasswordModel fpm = new ForgotPasswordModel
                {
                    Id = hashedToken,
                    Email = User.Resource.Id
                };
                try
                {
                    await Pcontainer.CreateItemAsync(fpm, new PartitionKey(fpm.Id));
                    string resetURL = "http://codesizzlerexams.azurewebsites.net/reset-password/" + token;
                    string html = "Dear User,<br><br>Please click on this <a href=\"" + resetURL + "\">link</a> to reset your password.";
                    Email(html, User.Resource.Id, "Password Reset Link");
                }
                catch (Exception e)
                {
                    return BadRequest(new { error = "There was an error adding to database. Please try again later." });
                }
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return NotFound(new { error = "Email is not registered." });
            }

            return Ok(new { message = "Password change link has been sent to your email." });
        }

        [HttpPost("ResetPassword/{token}")]
        [EnableCors("AllPolicy")]
        public async Task<IActionResult> ResetPassword(string token)
        {
            cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);
            Pdatabase = await CreateDatabaseAsync(PdatabaseId);
            Pcontainer = await CreateContainerAsync(Pdatabase, PcontainerId, "id");
            Udatabase = await CreateDatabaseAsync(UdatabaseId);
            Ucontainer = await CreateContainerAsync(Udatabase, UcontainerId, "id");
            string hashedToken = ComputeSha256Hash(token);
            try
            {
                // Read the item to see if it exists.  
                ItemResponse<ForgotPasswordModel> fpm = await Pcontainer.ReadItemAsync<ForgotPasswordModel>(hashedToken, new PartitionKey(hashedToken));
                if (fpm.Resource.Used || (DateTime.Compare(DateTime.Now, fpm.Resource.Expiry) > 0)) throw new Exception("Token has expired or has already been used. Please regenerate new reset link.");
                //Update used as true
                fpm.Resource.Used = true;
                await Pcontainer.ReplaceItemAsync(fpm.Resource, fpm.Resource.Id, new PartitionKey(fpm.Resource.Id));
                //Update password
                ItemResponse<UserModel> user = await Ucontainer.ReadItemAsync<UserModel>(fpm.Resource.Email, new PartitionKey(fpm.Resource.Email));
                string newKey = GenerateRandomString(length: 10);
                user.Resource.AccessCode = newKey;
                await Ucontainer.ReplaceItemAsync(user.Resource, user.Resource.Id, new PartitionKey(user.Resource.Id));
                string htmlMail = "Dear User,<br><br>Your access key has been successfully reset. Please check below for the new access key.<br><br>" +
                              "<b>Access Code:<b> " + newKey;
                Email(htmlMail, user.Resource.Id, "Key Reset Successfully");
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return NotFound(new { error = "Invalid token." });
            }
            catch (CosmosException ex)
            {
                return BadRequest(new { error = "There were problems updating the password. Please try again later." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            return Ok();
        }

        [HttpPost("PostNotification")]
        [EnableCors("AllPolicy")]
        public async Task<IActionResult> PostNotification([FromForm]string exam, [FromForm]string dayValidity, [FromForm]string message)
        {
            cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);
            Ndatabase = await CreateDatabaseAsync(NdatabaseId);
            Ncontainer = await CreateContainerAsync(Ndatabase, NcontainerId, "id");
            Edatabase = await CreateDatabaseAsync(EdatabaseId);
            Econtainer = await CreateContainerAsync(Edatabase, EcontainerId, "id");


            NotificationModel notification = new NotificationModel
            {
                Id = Guid.NewGuid().ToString(),
                Exam = exam,
                IssueFrom = DateTime.Now,
                IssueTill = DateTime.Now.AddDays(int.Parse(dayValidity)),
                Message = message
            };

            try
            {
                // Read the item to see if it exists.  
                ItemResponse<ExamModel> ExamResponse = await Econtainer.ReadItemAsync<ExamModel>(exam, new PartitionKey(exam));
                notification.ExamType = ExamResponse.Resource.ExamType;
                notification.ExamSerial = ExamResponse.Resource.ExamSerial;
                ItemResponse<NotificationModel> NotiificationResponse = await Ncontainer.CreateItemAsync(notification, new PartitionKey(notification.Id));
            }
            catch (CosmosException ex)
            {
                return BadRequest(new { error = "Could not post notification. Please try again." });

            }

            return Ok();
        }

        [HttpGet("GetNotification")]
        [EnableCors("AllPolicy")]
        [Authorize]
        public async Task<IActionResult> GetNotification()
        {
            cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);
            Ndatabase = await CreateDatabaseAsync(NdatabaseId);
            Ncontainer = await CreateContainerAsync(Ndatabase, NcontainerId, "id");
            Udatabase = await CreateDatabaseAsync(UdatabaseId);
            Ucontainer = await CreateContainerAsync(Udatabase, UcontainerId, "id");

            var currentUser = HttpContext.User;
            var userId = currentUser.Claims.FirstOrDefault(c => c.Type == "id").Value;

            ItemResponse<UserModel> UserResponse = await Ucontainer.ReadItemAsync<UserModel>(userId, new PartitionKey(userId));
            Dictionary<string, List<NotificationModel>> notifs = await GetNotificationList(UserResponse.Resource.Enrolled);

            return Ok(new { notifications = notifs });
        }

        [HttpPost("InitiatePayment")]
        [EnableCors("AllPolicy")]
        [Authorize]
        public async Task<IActionResult> InitiatePayment([FromForm]string exam)
        {
            cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);
            Udatabase = await CreateDatabaseAsync(UdatabaseId);
            Ucontainer = await CreateContainerAsync(Udatabase, UcontainerId, "id");
            Edatabase = await CreateDatabaseAsync(EdatabaseId);
            Econtainer = await CreateContainerAsync(Edatabase, EcontainerId, "id");

            var currentUser = HttpContext.User;
            var userId = currentUser.Claims.FirstOrDefault(c => c.Type == "id").Value;

            bool check = false;

            try
            {
                // Read the item to see if it exists.  
                ItemResponse<UserModel> UserResponse = await Ucontainer.ReadItemAsync<UserModel>(userId, new PartitionKey(userId));
                await Econtainer.ReadItemAsync<ExamModel>(exam, new PartitionKey(exam));
                if (!UserResponse.Resource.Enrolled.ContainsKey(exam)) check = true;
            }
            catch (CosmosException ex)
            {
                return Ok(new { check });

            }

            return Ok(new { check });
        }


        [HttpPost("ConfirmPayment")]
        [EnableCors("AllPolicy")]
        [Authorize]
        public async Task<IActionResult> ConfirmPayment([FromForm]string exam, [FromForm]string billing, [FromForm]string amount, [FromForm]string token)
        {
            cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);
            Udatabase = await CreateDatabaseAsync(UdatabaseId);
            Ucontainer = await CreateContainerAsync(Udatabase, UcontainerId, "id");
            Tdatabase = await CreateDatabaseAsync(TdatabaseId);
            Tcontainer = await CreateContainerAsync(Tdatabase, TcontainerId, "id");
            Edatabase = await CreateDatabaseAsync(EdatabaseId);
            Econtainer = await CreateContainerAsync(Edatabase, EcontainerId, "id");

            var currentUser = HttpContext.User;
            var userId = currentUser.Claims.FirstOrDefault(c => c.Type == "id").Value;

            TransactionModel transaction = new TransactionModel
            {
                Id = Guid.NewGuid().ToString(),
                Date = DateTime.Now,
                UserID = userId,
                ExamID = exam,
                BillingAdd = billing,
                Amount = amount,
                TxnID = token
            };

            try
            {
                // Read the item to see if it exists.  
                ItemResponse<UserModel> UserResponse = await Ucontainer.ReadItemAsync<UserModel>(userId, new PartitionKey(userId));
                await Econtainer.ReadItemAsync<ExamModel>(exam, new PartitionKey(exam));
                if (UserResponse.Resource.Enrolled.ContainsKey(exam)) throw new Exception("User already enrolled in exam.");
                UserResponse.Resource.Enrolled.Add(exam, false);
                UserResponse.Resource.Transactions.Add(transaction.Id);
                await Ucontainer.ReplaceItemAsync(UserResponse.Resource, UserResponse.Resource.Id, new PartitionKey(UserResponse.Resource.Id));
                ItemResponse<TransactionModel> NotificationResponse = await Tcontainer.CreateItemAsync(transaction, new PartitionKey(transaction.Id));
            }
            catch (CosmosException ex)
            {
                return BadRequest(new { error = "Error processing transaction." });

            }
            catch (Exception e)
            {
                return BadRequest(new { error = e.Message });
            }

            return Ok();
        }

        [HttpGet("GetTransactions")]
        [EnableCors("AllPolicy")]
        [Authorize]
        public async Task<IActionResult> GetTransactions()
        {
            /*CosmosClientOptions options = new CosmosClientOptions() { AllowBulkExecution = true };*/
            cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);
            Udatabase = await CreateDatabaseAsync(UdatabaseId);
            Ucontainer = await CreateContainerAsync(Udatabase, UcontainerId, "id");
            Tdatabase = await CreateDatabaseAsync(TdatabaseId);
            Tcontainer = await CreateContainerAsync(Tdatabase, TcontainerId, "id");

            var currentUser = HttpContext.User;
            var userId = currentUser.Claims.FirstOrDefault(c => c.Type == "id").Value;
            Dictionary<string, TransactionModel> transactions = new Dictionary<string, TransactionModel>();

            try
            {
                ItemResponse<UserModel> UserResponse = await Ucontainer.ReadItemAsync<UserModel>(userId, new PartitionKey(userId));
                foreach (string itemToRead in UserResponse.Resource.Transactions)
                {
                    ItemResponse<TransactionModel> TransResponse = await Tcontainer.ReadItemAsync<TransactionModel>(itemToRead, new PartitionKey(itemToRead));
                    transactions.Add(TransResponse.Resource.ExamID, TransResponse.Resource);
                }

            }
            catch (CosmosException ex)
            {
                return BadRequest(new { error = "Error fetching details.", transactions });
            }

            return Ok(new { transactions });
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

        static string ComputeSha256Hash(string rawData)
        {
            // Create a SHA256   
            using SHA256 sha256Hash = SHA256.Create();
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }

        private async Task<Dictionary<string, List<NotificationModel>>> GetNotificationList(Dictionary<string, bool> enrolled)
        {
            cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);
            Ndatabase = await CreateDatabaseAsync(NdatabaseId);
            Ncontainer = await CreateContainerAsync(Ndatabase, NcontainerId, "id");

            List<string> finalEnroll = new List<string>();
            foreach (var item in enrolled)
            {
                finalEnroll.Add(item.Key);
            }

            QueryDefinition query = new QueryDefinition("select * from notifs");
            FeedIterator<dynamic> resultSet = Ncontainer.GetItemQueryIterator<dynamic>(
            query,
            requestOptions: new QueryRequestOptions()
            {
                MaxItemCount = 10
            });

            List<dynamic> notifications = new List<dynamic>();
            while (resultSet.HasMoreResults)
            {
                FeedResponse<dynamic> response = await resultSet.ReadNextAsync();
                if (response.Diagnostics != null)
                {
                    Console.WriteLine($" Diagnostics {response.Diagnostics.ToString()}");
                }

                foreach (var item in response)
                {
                    notifications.Add(item);
                }
            }
            Dictionary<string, List<NotificationModel>> notifColl = new Dictionary<string, List<NotificationModel>>();
            foreach (var item in notifications)
            {
                NotificationModel n = JsonConvert.DeserializeObject<NotificationModel>(JsonConvert.SerializeObject(item));
                if (finalEnroll.Contains(n.Exam))
                {
                    if (DateTime.Compare(DateTime.Now, n.IssueTill) < 0)
                    {
                        if (!notifColl.ContainsKey(n.Exam))
                        {
                            notifColl.Add(n.Exam, new List<NotificationModel>());
                        }
                        notifColl[n.Exam].Add(n);
                    }
                }
            }

            return notifColl;
        }

        public static void Email(string htmlString, string email, string subject)
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

        public IActionResult Login(UserModel user)
        {
            IActionResult response = Unauthorized();

            if (user != null)
            {
                var tokenString = GenerateJSONWebToken(user);
                response = Ok(new { token = tokenString });
            }

            return response;
        }

        private string GenerateJSONWebToken(UserModel userInfo)
        {
            IdentityModelEventSource.ShowPII = true;
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[] {
                new Claim("isadmin", userInfo.IsAdmin.ToString()),
                new Claim("id", userInfo.Id),
                new Claim("name", userInfo.Name),
                new Claim("key", userInfo.AccessCode),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(_config["Jwt:Issuer"],
              _config["Jwt:Issuer"],
              claims,
              signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}