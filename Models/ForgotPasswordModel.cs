using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PortalAPI.Models
{
    public class ForgotPasswordModel
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "email")]
        public string Email { get; set; }

        [JsonProperty(PropertyName = "expiry")]
        public DateTime Expiry { get; set; } = DateTime.Now.AddMinutes(10);

        [JsonProperty(PropertyName = "used")]
        public bool Used { get; set; } = false;
    }
}
