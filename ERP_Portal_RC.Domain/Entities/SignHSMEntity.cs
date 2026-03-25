using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Entities
{
    public class SignHSMEntity
    {
        public string OID { get; private set; } = "";
        public DateTime ODate { get; private set; }
        public string PartyASoCCCD { get; private set; } = "";
        public string PartyATaxcode { get; private set; } = "";
        public string PartyAName { get; private set; } = "";
        public string PartyBTaxcode { get; private set; } = "";
        public string PartyBName { get; private set; } = "";

        /// <summary>
        /// file_data từ HSM Sign API response (base64 encoded XML đã ký).
        /// Truyền thẳng vào @ECtrlContentXML của SP.
        /// </summary>
        public string SignedXmlBase64 { get; private set; } = "";

        private SignHSMEntity() { } // bắt buộc dùng factory

        /// <summary>
        /// Factory method — validate trước khi tạo.
        /// Ném ArgumentException nếu OID hoặc SignedXmlBase64 rỗng.
        /// </summary>
        public static SignHSMEntity Create(
            string oid,
            DateTime oDate,
            string partyASoCCCD,
            string partyATaxcode,
            string partyAName,
            string partyBTaxcode,
            string partyBName,
            string signedXmlBase64)
        {
            if (string.IsNullOrWhiteSpace(oid))
                throw new ArgumentException("OID không được để trống.", nameof(oid));

            if (string.IsNullOrWhiteSpace(signedXmlBase64))
                throw new ArgumentException("SignedXmlBase64 không được để trống.", nameof(signedXmlBase64));

            return new SignHSMEntity
            {
                OID = oid.Trim(),
                ODate = oDate,
                PartyASoCCCD = partyASoCCCD ?? "",
                PartyATaxcode = partyATaxcode ?? "",
                PartyAName = partyAName ?? "",
                PartyBTaxcode = partyBTaxcode ?? "",
                PartyBName = partyBName ?? "",
                SignedXmlBase64 = signedXmlBase64
            };
        }
    }

    /// <summary>Kết quả Repository trả về sau khi gọi SP — chỉ là value object</summary>
    public class SignHSMResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = "";
    }
}

