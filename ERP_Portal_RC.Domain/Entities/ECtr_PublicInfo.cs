using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities
{
    public class ECtr_PublicInfo
    {
        public string? CmpnID { get; set; }
        public string? CmpnKey { get; set; }
        public string? InvcSign { get; set; }
        public string? InvcCode { get; set; }

        [JsonIgnore]
        public byte[]? InvcContent { get; set; }
        [JsonIgnore]
        public byte[]? InvcContent_ByCus { get; set; }

        public bool Party_B_IsSigned { get; set; }
        public bool Party_A_IsSigned { get; set; }
        public string InvcContentStr { get; set; }
        public string InvcContent_ByCusStr { get; set; }

    }
}
