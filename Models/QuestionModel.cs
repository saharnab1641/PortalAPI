using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PortalAPI.Models
{
    public class MCQModel
    {
        public MCQModel()
        {
            Options = new List<string>();
        }
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "examid")]
        public string ExamID { get; set; }

        [JsonProperty(PropertyName = "qtype")]
        public string QType { get; set; } = "mcq";

        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "options")]
        public List<string> Options { get; set; }

        [JsonProperty(PropertyName = "solutions")]
        public string Solutions { get; set; }
    }

    public class ArrangeModel
    {
        public ArrangeModel()
        {
            Options = new List<string>();
            Solutions = new List<string>();
        }
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "examid")]
        public string ExamID { get; set; }

        [JsonProperty(PropertyName = "qtype")]
        public string QType { get; set; } = "arrange";

        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "options")]
        public List<string> Options { get; set; }

        [JsonProperty(PropertyName = "solutions")]
        public List<string> Solutions { get; set; }
    }

    public class DnDModel
    {
        public DnDModel()
        {
            Statements = new List<string>();
            Options = new List<string>();
            Solutions = new Dictionary<string, string>();
        }
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "examid")]
        public string ExamID { get; set; }

        [JsonProperty(PropertyName = "qtype")]
        public string QType { get; set; } = "dnd";

        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "statements")]
        public List<string> Statements { get; set; }

        [JsonProperty(PropertyName = "options")]
        public List<string> Options { get; set; }

        [JsonProperty(PropertyName = "solutions")]
        public Dictionary<string, string> Solutions { get; set; }
    }

    public class SelectModel
    {
        public SelectModel()
        {
            Options = new Dictionary<string, List<string>>();
            Solutions = new Dictionary<string, string>();
        }
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "examid")]
        public string ExamID { get; set; }

        [JsonProperty(PropertyName = "qtype")]
        public string QType { get; set; } = "select";

        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "options")]
        public Dictionary<string, List<string>> Options { get; set; }

        [JsonProperty(PropertyName = "solutions")]
        public Dictionary<string, string> Solutions { get; set; }
    }

    public class TabularModel
    {
        public TabularModel()
        {
            Statements = new List<string>();
            Options = new List<string>();
            Solutions = new Dictionary<string, string>();
        }
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "examid")]
        public string ExamID { get; set; }

        [JsonProperty(PropertyName = "qtype")]
        public string QType { get; set; } = "tabular";

        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "statements")]
        public List<string> Statements { get; set; }

        [JsonProperty(PropertyName = "options")]
        public List<string> Options { get; set; }

        [JsonProperty(PropertyName = "solutions")]
        public Dictionary<string, string> Solutions { get; set; }
    }

    public class AllModel
    {
        public AllModel()
        {
            McqType = new List<MCQModel>();
            ArrangeType = new List<ArrangeModel>();
            DnDType = new List<DnDModel>();
            SelectType = new List<SelectModel>();
            TabularType = new List<TabularModel>();
        }

        [JsonProperty(PropertyName = "mcqtype")]
        public List<MCQModel> McqType { get; set; }

        [JsonProperty(PropertyName = "arrangetype")]
        public List<ArrangeModel> ArrangeType { get; set; }

        [JsonProperty(PropertyName = "dndtype")]
        public List<DnDModel> DnDType { get; set; }

        [JsonProperty(PropertyName = "selecttype")]
        public List<SelectModel> SelectType { get; set; }

        [JsonProperty(PropertyName = "tabulartype")]
        public List<TabularModel> TabularType { get; set; }
    }

}
