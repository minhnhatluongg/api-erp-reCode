using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.EntitiesInvoice
{
    public class WinInvoiceItem
    {
        [JsonPropertyName("itemNo")]
        public string ItemNo { get; set; } = "1";

        [JsonPropertyName("itemCode")]
        public string? ItemCode { get; set; }

        [JsonPropertyName("itemName")]
        public string? ItemName { get; set; }

        [JsonPropertyName("itemUnit")]
        public string? ItemUnit { get; set; }

        [JsonPropertyName("itemQuantity")]
        public string ItemQuantity { get; set; } = "0";

        /// <summary>
        /// Đơn giá CHƯA VAT.
        /// itemPrice trong GET response đã bao gồm VAT,
        /// Service tính lại: itemPriceNoVat = itemPrice / (1 + vatRate/100)
        /// </summary>
        [JsonPropertyName("itemPrice")]
        public string ItemPrice { get; set; } = "0";

        [JsonPropertyName("itemVatRate")]
        public string ItemVatRate { get; set; } = "0";

        [JsonPropertyName("itemVatAmnt")]
        public string ItemVatAmnt { get; set; } = "0";

        [JsonPropertyName("itemAmountNoVat")]
        public string ItemAmountNoVat { get; set; } = "0";

        [JsonPropertyName("itemNote")]
        public string? ItemNote { get; set; }
    }
}
