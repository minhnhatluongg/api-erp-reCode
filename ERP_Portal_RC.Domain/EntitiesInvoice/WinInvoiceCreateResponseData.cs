using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.EntitiesInvoice
{
    public class WinInvoiceCreateResponseData
    {
        [JsonPropertyName("oid")]
        public string? Oid { get; set; }

        /// <summary>Số hóa đơn ("0000000" nếu là nháp)</summary>
        [JsonPropertyName("invCode")]
        public string? InvCode { get; set; }

        [JsonPropertyName("invRef")]
        public string? InvRef { get; set; }

        [JsonPropertyName("invSign")]
        public string? InvSign { get; set; }

        [JsonPropertyName("invDate")]
        public string? InvDate { get; set; }

        [JsonPropertyName("invName")]
        public string? InvName { get; set; }

        /// <summary>Mã CQT cấp cho hóa đơn</summary>
        [JsonPropertyName("govCode")]
        public string? GovCode { get; set; }

        [JsonPropertyName("govTranfer")]
        [JsonConverter(typeof(FlexibleStringConverter))]
        public string? GovTranfer { get; set; }

        [JsonPropertyName("autoSign")]
        [JsonConverter(typeof(FlexibleStringConverter))]
        public string? AutoSign { get; set; }
    }
}
