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

        [JsonProperty(PropertyName = "category")]
        public string Category { get; set; }

        [JsonProperty(PropertyName = "track")]
        public string Track { get; set; }

        [JsonProperty(PropertyName = "prerequisites")]
        public List<string> Prerequisites { get; set; }

        [JsonProperty(PropertyName = "target")]
        public List<string> Target { get; set; }

        [JsonProperty(PropertyName = "outcome")]
        public List<string> Outcome { get; set; }

        [JsonProperty(PropertyName = "thumbdesc")] //short description under thumbnail
        public string ThumbDescription { get; set; }

        //Description ad division of course modules and importance percentages

        [JsonProperty(PropertyName = "trackdetails")]
        public List<TrackModel> TrackDetails { get; set; } = new List<TrackModel>(); //see below for TrackModel definition

        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "examimage")]
        public string ExamImage { get; set; }

        [JsonProperty(PropertyName = "pricing")]
        public string Pricing { get; set; }
    }

    public class TrackModel
    {
        [JsonProperty(PropertyName = "topic")]
        public string Topic { get; set; }

        [JsonProperty(PropertyName = "upperperc")]
        public string UpperPerc { get; set; }

        [JsonProperty(PropertyName = "lowerperc")]
        public string LowerPerc { get; set; }

        [JsonProperty(PropertyName = "divisions")]
        public List<string> Divisions { get; set; }
    }
}
