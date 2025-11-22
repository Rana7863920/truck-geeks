using Microsoft.Extensions.Options;
using System.Net.Mail;
using System.Net;
using Microsoft.AspNetCore.Identity.UI.Services;
using System.Threading.Tasks;

namespace TruckServices.EmailSender
{
    public class EmailSender
    {
        private readonly EmailSettings _emailSettings;

        public EmailSender(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public async Task SendClientEnquiryEmailAsync(string toEmail, string clientName, string clientEmail, string subject, string message, string phone = null)
        {
            string phoneRow = string.IsNullOrWhiteSpace(phone)
                ? ""
                : $"<tr><th>Phone</th><td>{System.Net.WebUtility.HtmlEncode(phone)}</td></tr>";

            string htmlTemplate = @"
<!DOCTYPE html>
<html lang='en'>
<head>
  <meta charset='UTF-8' />
  <meta name='viewport' content='width=device-width, initial-scale=1' />
  <title>New Client Enquiry</title>
  <style>
    /* Reset and base */
    body, html {{
      margin: 0; padding: 0; background-color: #f5f7fa; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; color: #333;
    }}
    a {{
      color: #1a73e8; text-decoration: none;
    }}
    a:hover {{
      text-decoration: underline;
    }}

    /* Container */
    .email-container {{
      max-width: 600px;
      margin: 40px auto;
      background-color: #ffffff;
      border-radius: 12px;
      box-shadow: 0 8px 24px rgba(0,0,0,0.1);
      overflow: hidden;
      border-top: 6px solid #1a73e8;
    }}

    /* Header */
    .email-header {{
      background: #1a73e8;
      color: white;
      padding: 24px 30px;
      text-align: center;
      font-size: 28px;
      font-weight: 700;
      letter-spacing: 1.5px;
      text-transform: uppercase;
      box-shadow: 0 3px 6px rgba(0,0,0,0.12);
    }}

    /* Main content */
    .email-body {{
      padding: 30px;
      font-size: 16px;
      line-height: 1.6;
      color: #444444;
    }}

    /* Table */
    table {{
      width: 100%;
      border-collapse: collapse;
      margin-top: 25px;
    }}

    th, td {{
      padding: 15px 20px;
      text-align: left;
      border-bottom: 1px solid #e1e8f0;
    }}

    th {{
      background-color: #e8f0fe;
      color: #1a73e8;
      font-weight: 600;
      text-transform: uppercase;
      font-size: 14px;
      letter-spacing: 0.05em;
    }}

    td {{
      color: #555555;
      font-size: 15px;
      word-break: break-word;
    }}

    /* Footer */
    .email-footer {{
      text-align: center;
      font-size: 13px;
      padding: 20px 30px;
      color: #999999;
      border-top: 1px solid #e1e8f0;
      background-color: #fafafa;
    }}

    /* Responsive */
    @media (max-width: 480px) {{
      .email-container {{
        margin: 20px 15px;
      }}
      .email-body {{
        padding: 20px 15px;
        font-size: 14px;
      }}
      th, td {{
        padding: 12px 10px;
      }}
    }}
  </style>
</head>
<body>
  <div class='email-container'>
    <div class='email-header'>New Client Enquiry</div>
    <div class='email-body'>
      <p>Hi Team,</p>
      <p>You have received a new enquiry via your website contact form. Here are the details:</p>
      <table role='presentation'>
        <tr>
          <th>Name</th>
          <td>{0}</td>
        </tr>
        <tr>
          <th>Email</th>
          <td><a href='mailto:{1}'>{1}</a></td>
        </tr>
        <tr>
          <th>Subject</th>
          <td>{2}</td>
        </tr>
        {3}
        <tr>
          <th>Message</th>
          <td style='white-space: pre-wrap;'>{4}</td>
        </tr>
      </table>
      <p style='margin-top: 30px;'>Please respond promptly to provide excellent client service.</p>
      <p>Best regards,<br/>Truck Geeks</p>
    </div>
    <div class='email-footer'>&copy; {5} True Build Projects. All rights reserved.</div>
  </div>
</body>
</html>
";

            string formattedHtml = string.Format(htmlTemplate,
                System.Net.WebUtility.HtmlEncode(clientName),
                System.Net.WebUtility.HtmlEncode(clientEmail),
                System.Net.WebUtility.HtmlEncode(subject),
                phoneRow,
                System.Net.WebUtility.HtmlEncode(message).Replace("\n", "<br>"),
                System.DateTime.Now.Year);

            await Execute(toEmail, subject, formattedHtml);
        }


        // Base email sending method
        private async Task Execute(string toEmail, string subject, string htmlContent)
        {
            using (var client = new SmtpClient(_emailSettings.SmtpHost, _emailSettings.SmtpPort))
            {
                client.EnableSsl = true;
                client.Credentials = new NetworkCredential(_emailSettings.SmtpUsername, _emailSettings.SmtpPassword);

                using (var mailMessage = new MailMessage())
                {
                    mailMessage.From = new MailAddress(_emailSettings.FromEmail, _emailSettings.FromName);
                    mailMessage.To.Add(toEmail);
                    mailMessage.Subject = subject;
                    mailMessage.Body = htmlContent;
                    mailMessage.IsBodyHtml = true;

                    if (!string.IsNullOrEmpty(_emailSettings.CcEmail))
                    {
                        mailMessage.CC.Add(_emailSettings.CcEmail);
                    }

                    await client.SendMailAsync(mailMessage);
                }
            }
        }
    }
}
