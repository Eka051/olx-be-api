using System.Net.Mail;

namespace olx_be_api.Helpers
{
    public interface IEmailHelper
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlMessage);
    }

    public class EmailHelper : IEmailHelper
    {
        private readonly IConfiguration _configuration;
        public EmailHelper(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            var smtpHost = _configuration["SmtpSettings:Host"];
            var smtpPort = _configuration.GetValue<int>("SmtpSettings:Port");
            var smtpUsername = _configuration["SmtpSettings:Username"];
            var smtpPassword = _configuration["SmtpSettings:Password"];
            var smtpFromAddress = _configuration["SmtpSettings:FromAddress"];
            var smtpFromName = _configuration["SmtpSettings:FromName"] ?? "OLX CLONE Indonesia";
            var enableSsl = _configuration.GetValue<bool>("SmtpSettings:EnableSsl");

            if (string.IsNullOrEmpty(smtpHost) || smtpPort == 0 || string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword))
            {
                throw new InvalidOperationException("SMTP settings are not configured properly.");
            }

            try
            {
                using (var mailMessage = new MailMessage())
                {
                    mailMessage.From = new MailAddress(smtpFromAddress!, smtpFromName);
                    mailMessage.To.Add(new MailAddress(toEmail));
                    mailMessage.Subject = subject;
                    mailMessage.Body = htmlMessage;
                    mailMessage.IsBodyHtml = true;

                    using (var smtpClient = new SmtpClient(smtpHost, smtpPort))
                    {
                        smtpClient.EnableSsl = enableSsl;
                        smtpClient.Credentials = new System.Net.NetworkCredential(smtpUsername, smtpPassword);

                        await smtpClient.SendMailAsync(mailMessage);
                    }
                }
            }
            catch (SmtpException smtpEx)
            {
                throw new InvalidOperationException("Failed to send email. SMTP error occurred.", smtpEx);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to send email.", ex);
            }
        }
    }
}
