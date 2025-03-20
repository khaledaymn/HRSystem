using HRSystem.DTO;
using HRSystem.Settings;
using MailKit.Security;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Net.Mail;
using System.Text;
using System.Web;

namespace HRSystem.Services.EmailServices
{
    public class EmailServices : IEmailServices
    {
        private readonly EmailConfiguration _configuration;
        public EmailServices(IOptions<EmailConfiguration> configuration)
        {
            _configuration = configuration.Value;
        }

        public async Task<string> SendEmailAsync(string Name, string Email,string token)
        {

            using var client = new MailKit.Net.Smtp.SmtpClient();
            try
            {
                // Create an email message based on the provided authentication details
                var mailMessage = CreateEmailMessage(Name,Email,token);

                // Connect to the SMTP server using the configured settings
                await client.ConnectAsync(_configuration.SmtpServer, _configuration.Port, true);

                // Remove the XOAUTH2 authentication mechanism
                client.AuthenticationMechanisms.Remove("XOAUTH2");

                // Authenticate with the SMTP server using the provided credentials
                await client.AuthenticateAsync(_configuration.UserName, _configuration.Password);

                // Send the email message
                await client.SendAsync(mailMessage);

                // If the email is sent successfully, return "success"
                return "success";
            }
            catch (SmtpException ex)
            {
                // Return a meaningful error message
                return "An SMTP error occurred while sending an email. Please try again later.";
            }
            catch (Exception ex)
            {
                // Return a generic error message
                return "An error occurred while sending an email. Please try again later.";
            }
            finally
            {
                // Disconnect and dispose the client
                await client.DisconnectAsync(true);
                client.Dispose();
            }
        }
        private MimeMessage CreateEmailMessage(string name, string email, string token)
        {
            // Validate inputs
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
                throw new ArgumentException("Name, email, and token cannot be null or empty.");

           
            var link = $"{_configuration.PasswordResetLink}?email={email}&token={token}";

            // Build HTML email content
            var content = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ 
            font-family: 'Tajawal', Arial, sans-serif; 
            color: #333; 
            direction: rtl; 
            text-align: right; 
            background-color: #f0f4f8; 
            margin: 0; 
            padding: 20px; 
        }}
        .container {{ 
            max-width: 600px; 
            margin: 0 auto; 
            padding: 30px; 
            background-color: white; 
            border-radius: 15px; 
            box-shadow: 0 4px 10px rgba(0, 0, 0, 0.1); 
        }}
        .header {{ 
            text-align: center; 
            margin-bottom: 30px; 
        }}
        .header h2 {{ 
            font-size: 28px; 
            color: #008d7f; 
            margin: 0; 
            font-weight: bold; 
        }}
        .header p {{ 
            font-size: 14px; 
            color: #666; 
            margin: 5px 0 0; 
        }}
        .content p {{ 
            font-size: 16px; 
            line-height: 1.8; 
            margin: 15px 0; 
            color: #444; 
        }}
        .button {{ 
            display: inline-block; 
            padding: 12px 40px; 
            background-color: #008d7f; 
            color: white !important; 
            text-decoration: none; 
            border-radius: 7px; 
            font-size: 18px; 
            font-weight: bold; 
            box-shadow: 0 2px 5px rgba(0, 0, 0, 0.2); 
            transition: background-color 0.3s ease; 
        }}
        .button:hover {{ 
            background-color: #008d7fd8; 
        }}
        .footer {{ 
            margin-top: 30px; 
            font-size: 14px; 
            color: #555; 
            text-align: center; 
            border-top: 1px solid #eee; 
            padding-top: 15px; 
        }}
        .footer a {{ 
            color: #008d7f; 
            text-decoration: none; 
            font-weight: bold; 
        }}
        .footer a:hover {{ 
            color: #008d7f; 
        }}
    </style>
    <link href='https://fonts.googleapis.com/css2?family=Tajawal:wght@400;700&display=swap' rel='stylesheet'>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>أعد تعيين كلمة المرور</h2>
            <p>اضغط على الرابط لإعادة تعيين كلمة المرور الخاصة بك</p>
        </div>
        <div class='content'>
            <p>مرحبًا {name}،</p>
            <p>لقد تلقينا طلبًا لإعادة تعيين كلمة المرور لحسابك في <strong>صيدلية البلسم الطبية</strong>.</p>
            <p>إذا كنت أنت من قدم هذا الطلب، يرجى النقر على الزر التالي:</p>
            <p><a href='{link}' class='button'>إعادة تعيين كلمة المرور</a></p>
            <p>إذا لم تطلب ذلك، يمكنك تجاهل هذه الرسالة بأمان، وستظل كلمة المرور الخاصة بك دون تغيير.</p>
        </div>
        <div class='footer'>
            <p>شكرًا لثقتك بنا،</p>
            <p>فريق صيدلية البلسم الطبية</p>
            <p><a href='mailto:{_configuration.From}'>تواصلوا معنا</a></p>
        </div>
    </div>
</body>
</html>";

            // Create email message
            var mailMessage = new MimeMessage();
            mailMessage.From.Add(new MailboxAddress("صيدلية البلسم الطبية", _configuration.From));
            mailMessage.To.Add(new MailboxAddress(name, email));
            mailMessage.Subject = "إعادة تعيين كلمة المرور";
            mailMessage.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = content };

            return mailMessage;
        }
    }
}
