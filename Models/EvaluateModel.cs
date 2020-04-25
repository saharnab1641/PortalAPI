using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PortalAPI.Models
{
    public class EvaluateModel
    {
        public string questionId { get; set; }

        public string questionType { get; set; }

        public Dictionary<string, string> optionchoice { get; set; }
    }
}
