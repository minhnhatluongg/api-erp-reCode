using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.EntitiesInvoice
{
    public class WinInvoiceCreateRequest
    {
        [JsonPropertyName("invName")]
        public string InvName { get; set; } = string.Empty;

        [JsonPropertyName("invSerial")]
        public string InvSerial { get; set; } = string.Empty;

        [JsonPropertyName("invNumber")]
        public string? InvNumber { get; set; }

        [JsonPropertyName("invDate")]
        public string InvDate { get; set; } = string.Empty;

        [JsonPropertyName("invCustomer")]
        public string InvCustomer { get; set; } = "0";

        [JsonPropertyName("invRef")]
        public string InvRef { get; set; } = string.Empty;

        [JsonPropertyName("invRefDate")]
        public string? InvRefDate { get; set; }

        [JsonPropertyName("buyerTax")]
        public string? BuyerTax { get; set; }

        [JsonPropertyName("buyerName")]
        public string? BuyerName { get; set; }

        [JsonPropertyName("buyerCompany")]
        public string? BuyerCompany { get; set; }

        [JsonPropertyName("buyerAddress")]
        public string? BuyerAddress { get; set; }

        [JsonPropertyName("buyerAcc")]
        public string? BuyerAcc { get; set; }

        [JsonPropertyName("buyerBank")]
        public string? BuyerBank { get; set; }

        [JsonPropertyName("buyerEmail")]
        public string? BuyerEmail { get; set; }

        [JsonPropertyName("buyerPhone")]
        public string? BuyerPhone { get; set; }

        [JsonPropertyName("invSubTotal")]
        public string InvSubTotal { get; set; } = "0";

        [JsonPropertyName("invVatRate")]
        public string InvVatRate { get; set; } = "0";

        [JsonPropertyName("invVatAmount")]
        public string InvVatAmount { get; set; } = "0";

        [JsonPropertyName("invTotalAmount")]
        public string InvTotalAmount { get; set; } = "0";

        [JsonPropertyName("invPayment")]
        public string? InvPayment { get; set; }

        [JsonPropertyName("invCurrency")]
        public string InvCurrency { get; set; } = "VND";

        [JsonPropertyName("invAutoSign")]
        public string InvAutoSign { get; set; } = "0";

        [JsonPropertyName("note")]
        public string? Note { get; set; }

        [JsonPropertyName("items")]
        public List<WinInvoiceItem> Items { get; set; } = new();
    }
}
