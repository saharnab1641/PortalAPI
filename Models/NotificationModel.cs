using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PortalAPI.Models
{
    public class NotificationModel
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "message")]
        public string Message { get; set; }

        [JsonProperty(PropertyName = "issuefrom")]
        public DateTime IssueFrom { get; set; }

        [JsonProperty(PropertyName = "issuetill")]
        public DateTime IssueTill { get; set; }

        [JsonProperty(PropertyName = "exam")]
        public string Exam { get; set; }

        [JsonProperty(PropertyName = "etype")]
        public string ExamType { get; set; }

        [JsonProperty(PropertyName = "eserial")]
        public string ExamSerial { get; set; }
    }
}
