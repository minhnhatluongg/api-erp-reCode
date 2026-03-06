using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace ERP_Portal_RC.Application.Services
{
    public class MailService : IMailService
    {
        private readonly SmtpSettings _smtp;
        private readonly ILogger<MailService> _logger;

        public MailService(IOptions<SmtpSettings> smtpOptions, ILogger<MailService> logger)
        {
            _smtp = smtpOptions.Value;
            _logger = logger;
        }

        public async Task SendApproveNotificationAsync(EmailUserDept dept, EContractMaster master, string oid, string factorId)
        {
            try
            {
                string subject = "";
                string body = "";
                string mailTo = dept.Email;
                string mailCC = dept.Email_CC;
                string frWebsite = "www.winerp.org";

                // Logic phân loại nội dung Mail theo code cũ
                if (factorId == "JOB_00001") 
                {
                    subject = $"ERP - YÊU CẦU TẠO MẪU - {master.CusTax} - {master.CusName}";
                    body = $"Kính gửi Anh/chị: {dept.User_position}\n\n" +
                           $"Hợp đồng {oid} có yêu cầu tạo mẫu\n" +
                           $"MST: {master.CusTax}\n" +
                           $"Công ty: {master.CusName}\n" +
                           $"Kinh doanh: {master.SaleFullName}.\n\n" +
                           $"P/S: Email này được gửi từ website {frWebsite} , anh/chị vui lòng không reply lại mail này.\n" +
                           $"Chân thành cảm ơn!!";
                }
                else if (factorId == "JOB_00002") 
                {
                    mailTo = "hoainv@win-tech.vn";
                    mailCC = "hoainv@win-tech.vn";
                    frWebsite = "www.winerp.org";


                    subject = $"ERP - YÊU CẦU PHÁT HÀNH HÓA ĐƠN – {master.CusTax} - {master.CusName}";
                    body = $"Kính gửi Anh/chị kỹ thuật!\n\n" +
                           $"Hợp đồng {oid} có yêu cầu kích hoạt phát hành hóa đơn\n" +
                           $"MST: {master.CusTax}\n" +
                           $"Công ty: {master.CusName}\n" +
                           $"Kinh doanh: {master.SaleFullName}\n\n" +
                           $"P/S: Email này được gửi từ website {frWebsite} , anh/chị vui lòng không reply lại mail này.\n" +
                           $"Chân thành cảm ơn!!";
                }

                if (string.IsNullOrEmpty(subject)) return;
                using var mail = new MailMessage();
                mail.From = new MailAddress(_smtp.SenderEmail, _smtp.SenderDisplayName);
                mail.To.Add(mailTo);

                if (!string.IsNullOrEmpty(mailCC))
                {
                    mail.CC.Add(new MailAddress(mailCC));
                }

                mail.Subject = subject;
                mail.Body = body;
                mail.IsBodyHtml = false;

                using var smtpClient = new SmtpClient(_smtp.Host, _smtp.Port)
                {
                    Credentials = new NetworkCredential(_smtp.SenderEmail, _smtp.SenderPassword),
                    EnableSsl = _smtp.EnableSsl,
                    Timeout = 50000
                };

                await smtpClient.SendMailAsync(mail);
                _logger.LogInformation("[MailService] Đã gửi mail duyệt thành công cho OID: {OID}", oid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MailService] Gửi mail duyệt thất bại cho OID: {OID}", oid);
            }
        }

        public async Task SendProposeSignNotificationAsync(
            string oid,
            string cusTax,
            string cusName,
            string saleFullName,
            string ktName)
        {
            try
            {
                var contractLink = $"{_smtp.PortalBaseUrl}/in-process/edit?oid={oid}";

                var body =
                    $"Kính gửi Anh/Chị!\n\n" +
                    $"Hợp đồng: {oid}\n" +
                    $"MST: {cusTax}\n" +
                    $"Công ty: {cusName}\n" +
                    $"Kinh doanh: {saleFullName}\n" +
                    $"Kế toán kiểm tra: {ktName}\n\n" +
                    $"Anh/Chị vui lòng vào link bên dưới để phê duyệt hợp đồng:\n" +
                    $"{contractLink}\n\n" +
                    $"P/S: Email này được gửi tự động từ hệ thống ERP Portal (www.winerp.org). " +
                    $"Vui lòng không reply lại email này.\n\n" +
                    $"Chân thành cảm ơn!";

                var subject = $"ERP – ĐỀ XUẤT DUYỆT HỢP ĐỒNG – {cusTax} - {cusName}";

                using var mail = new MailMessage();
                mail.From = new MailAddress(_smtp.SenderEmail, _smtp.SenderDisplayName);
                mail.To.Add(_smtp.ReceiverKeToan);
                mail.Subject = subject;
                mail.Body = body;
                mail.IsBodyHtml = false;

                using var smtpClient = new SmtpClient(_smtp.Host, _smtp.Port)
                {
                    Credentials = new NetworkCredential(_smtp.SenderEmail, _smtp.SenderPassword),
                    EnableSsl = _smtp.EnableSsl,
                    Timeout = 50000
                };

                await smtpClient.SendMailAsync(mail);
                _logger.LogInformation("[MailService] Đã gửi email trình ký hợp đồng {OID} tới {To}", oid, _smtp.ReceiverKeToan);
            }
            catch (Exception ex)
            {
                // Fire-and-forget: không ném exception để nghiệp vụ chính không bị ảnh hưởng
                _logger.LogWarning("[MailService] Gửi mail thất bại cho OID={OID}: {Error}", oid, ex.Message);
            }
        }
    }
}
