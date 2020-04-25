using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PortalAPI.Models
{
    public class TransactionModel
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "userid")]
        public string UserID { get; set; }

        [JsonProperty(PropertyName = "examid")]
        public string ExamID { get; set; }

        [JsonProperty(PropertyName = "txnid")]
        public string TxnID { get; set; }

        [JsonProperty(PropertyName = "date")]
        public DateTime Date { get; set; }

        [JsonProperty(PropertyName = "amount")]
        public string Amount { get; set; }

        [JsonProperty(PropertyName = "billingadd")]
        public string BillingAdd { get; set; }

    }
}
