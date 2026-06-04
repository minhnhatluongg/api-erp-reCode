using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.DTOs.InvoiceTemplate;
using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Domain.Entities;
using ERP_Portal_RC.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.Services
{
    /// <summary>
    /// Quản lý mẫu hóa đơn (InvoiceTemplate) - decode/encode XSLT, apply rule theo config.
    /// </summary>
    public class InvoiceTemplateService : IInvoiceTemplateService
    {
        private readonly ITemplateRepository _templates;
        private readonly IRuleRepository _rules;

        public InvoiceTemplateService(
            ITemplateRepository templates,
            IRuleRepository rules)
        {
            _templates = templates;
            _rules = rules;
        }

        #region Encode / Decode helpers

        /// <summary>
        /// DB lưu: gzip(base64(utf8(xslt))) → trả lại xslt utf8 thuần.
        /// </summary>
        private static string? DecodeFromDb(string? encoded)
        {
            if (string.IsNullOrEmpty(encoded))
                return null;

            var gzipBytes = Convert.FromBase64String(encoded);

            using var input = new MemoryStream(gzipBytes);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var outMs = new MemoryStream();
            gzip.CopyTo(outMs);

            string base64 = Encoding.UTF8.GetString(outMs.ToArray());
            byte[] raw = Convert.FromBase64String(base64);
            return Encoding.UTF8.GetString(raw);
        }

        private static string? EncodeToDb(string? raw)
        {
            if (string.IsNullOrEmpty(raw))
                return null;

            string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
            byte[] input = Encoding.UTF8.GetBytes(base64);

            using var ms = new MemoryStream();
            using (var gzip = new GZipStream(ms, CompressionMode.Compress, true))
            {
                gzip.Write(input, 0, input.Length);
            }
            return Convert.ToBase64String(ms.ToArray());
        }

        #endregion

        public async Task<IEnumerable<InvoiceTemplateListItemDto>> GetAllTemplatesAsync()
        {
            var list = await _templates.GetListAsync();
            return list.Select(t => new InvoiceTemplateListItemDto
            {
                TemplateID = t.TemplateID,
                TemplateCode = t.TemplateCode,
                TemplateName = t.TemplateName
            });
        }

        public async Task<string?> GetRawXsltAsync(int templateId)
        {
            var tpl = await _templates.GetByIdAsync(templateId);
            if (tpl == null) return null;

            return DecodeFromDb(tpl.InvoiceContent);
        }

        public async Task<InvoiceTemplateXsltDto?> GetTemplateXsltAsync(int templateId)
        {
            var tpl = await _templates.GetByIdAsync(templateId);
            if (tpl == null) return null;

            var raw = DecodeFromDb(tpl.InvoiceContent);
            return new InvoiceTemplateXsltDto
            {
                TemplateID = tpl.TemplateID,
                TemplateCode = tpl.TemplateCode,
                TemplateName = tpl.TemplateName,
                RawXslt = raw,
                DetectedConfig = InvoiceXsltConfigurator.Detect(raw)
            };
        }

        public AdjustConfigDto DetectConfig(string? rawXslt) => InvoiceXsltConfigurator.Detect(rawXslt);

        public async Task<InvoiceTemplateXsltDto?> GetTemplateByCodeAsync(string templateCode)
        {
            if (string.IsNullOrWhiteSpace(templateCode)) return null;

            var tpl = await _templates.GetByCodeAsync(templateCode);
            if (tpl == null) return null;

            var raw = DecodeFromDb(tpl.InvoiceContent);
            return new InvoiceTemplateXsltDto
            {
                TemplateID = tpl.TemplateID,
                TemplateCode = tpl.TemplateCode,
                TemplateName = tpl.TemplateName,
                RawXslt = raw,
                DetectedConfig = InvoiceXsltConfigurator.Detect(raw)
            };
        }

        public async Task<bool> SaveTemplateAsync(InvoiceTemplate model)
        {
            model.InvoiceContent = EncodeToDb(model.InvoiceContent);
            return await _templates.InsertTemplateAsync(model);
        }

        public async Task<bool> UpdateTemplateContentAsync(string templateCode, string xsltContent)
        {
            var tpl = await _templates.GetByCodeAsync(templateCode);
            if (tpl == null) return false;

            string? zipped = EncodeToDb(xsltContent);
            if (zipped == null) return false;

            return await _templates.UpdateTemplateContentAsync(tpl.TemplateID, zipped);
        }

        public async Task<string?> BuildXsltWithRulesAsync(string xsltRaw, InvoiceConfigDto options)
        {
            if (string.IsNullOrEmpty(xsltRaw)) return null;
            if (options == null) return xsltRaw;

            string result = xsltRaw;

            var ruleResults = await _rules.GetListAsync();
            var ruleDict = new Dictionary<string, string>();
            foreach (var r in ruleResults)
            {
                var decoded = DecodeFromDb(r.RuleContent);
                if (!string.IsNullOrEmpty(decoded) && !string.IsNullOrEmpty(r.RuleCode))
                    ruleDict[r.RuleCode] = decoded!;
            }

            void Apply(string code, bool enabled)
            {
                if (!enabled) return;
                if (ruleDict.TryGetValue(code, out var fragment))
                {
                    result = result.Replace($"<!--RULE:{code}-->", fragment);
                }
            }

            Apply("cksToKhai", options.TokhaiApproved);
            Apply("cksVCNB", options.IsVCNB);
            Apply("cksTemVe", options.IsTemVe);
            Apply("cksBH", options.IsHDBH);
            Apply("cksVAT", options.IsHDVAT);
            Apply("cksSignLocal", options.SignAtClient);
            Apply("ckDTS", options.IsMultiVat);
            Apply("cskNumber", options.GenerateNumberOnSign);
            Apply("cksEmailSV", options.SendMailAtServer);
            Apply("cksOtherVAT", options.PriceBeforeVat);
            Apply("cksIsSignServerProcess", options.HasFee);
            Apply("isCTT", options.IsTaxDocument);
            Apply("cksData", options.UseSampleData);

            return result;
        }

        public async Task<string?> ConvertXsltAsync(int templateId, InvoiceConfigDto config)
        {
            var tpl = await _templates.GetByIdAsync(templateId);
            if (tpl == null) return null;

            var raw = DecodeFromDb(tpl.InvoiceContent);
            if (raw == null) return null;

            return await BuildXsltWithRulesAsync(raw, config);
        }
    }
}
