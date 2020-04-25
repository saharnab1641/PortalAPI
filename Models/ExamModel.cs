using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PortalAPI.Models
{
    public class ExamModel
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "etype")]
        public string ExamType { get; set; }

        [JsonProperty(PropertyName = "eserial")]
        public string ExamSerial { get; set; }

        [JsonProperty(PropertyName = "trackdetails")]
        public List<TrackModel> TrackDetails { get; set; } = new List<TrackModel>();

        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "pricing")]
        public string Pricing { get; set; }
    }

    public class TrackModel
    {
        [JsonProperty(PropertyName = "topic")]
        public string Topic { get; set; }

        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "hours")]
        public int Hours { get; set; }
    }
}
