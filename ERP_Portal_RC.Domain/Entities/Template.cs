using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities
{
    using System.Text.Json.Serialization;

    public class Template
    {
        public string? SampleId { get; init; }
        public string? DocType { get; init; }
        public string? XsltContent { get; init; }
        public string? XmlContent { get; init; }
        public string? SampleNameEx { get; init; }
        public DateTime? CrtDate { get; init; }
        public DateTime? ChgDate { get; init; }
        public string? LogoBase64 { get; init; }
        public string? GovSampleSign { get; init; }
        public string? GovSampleSignName { get; init; }
        public string? FactorId { get; init; }
        public string? Fname { get; init; }
        public string? UrlDownload { get; init; }
        public string? Oid { get; init; }
        public string? RegisTypeId { get; init; }
        public string? Descript { get; init; }
        public string? UseFactorId { get; init; }
        public string? SampleName { get; init; }

        [JsonIgnore]
        public string Id => FactorId ?? string.Empty;

        [JsonIgnore]
        public string Text => Fname ?? string.Empty;
    }
}
