using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;


namespace PortalAPI.Models
{
    public class UserModel
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "isAdmin")]
        public bool IsAdmin { get; set; } = false;

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "accesscode")]
        public string AccessCode { get; set; }

        [JsonProperty(PropertyName = "address")]
        public string Address { get; set; }

        [JsonProperty(PropertyName = "state")]
        public string State { get; set; }

        [JsonProperty(PropertyName = "country")]
        public string Country { get; set; }

        [JsonProperty(PropertyName = "pincode")]
        public string Pincode { get; set; }

        [JsonProperty(PropertyName = "expiry")]
        public string Expiry { get; set; } = null;

        [JsonProperty(PropertyName = "jobtitle")]
        public string JobTitle { get; set; }

        [JsonProperty(PropertyName = "company")]
        public string Company { get; set; }

        [JsonProperty(PropertyName = "contact")]
        public string Contact { get; set; }

        // Updated enrolled definition to a dictionary 
        // containing examid key and a boolean value
        // representing if the user has taken the exam (true) or not (false).
        // Currently does not serve any function. Might be of use later. 

        [JsonProperty(PropertyName = "enrolled")]
        public Dictionary<string, bool> Enrolled { get; set; } = new Dictionary<string, bool>();

        [JsonProperty(PropertyName = "reports")]
        public List<string> Reports { get; set; } = new List<string>();

        [JsonProperty(PropertyName = "txns")]
        public List<string> Transactions { get; set; } = new List<string>();

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
