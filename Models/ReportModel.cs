using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PortalAPI.Models
{
    public class ReportModel
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "email")]
        public string Email { get; set; }

        [JsonProperty(PropertyName = "address")]
        public string Address { get; set; }

        [JsonProperty(PropertyName = "examid")]
        public string ExamID { get; set; }

        [JsonProperty(PropertyName = "passingdate")]
        public string PassingDate { get; set; }

        [JsonProperty(PropertyName = "scored")]
        public int Scored { get; set; }

        [JsonProperty(PropertyName = "tnc")]
        public string TnC { get; set; } = "Terms and Conditions";

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
