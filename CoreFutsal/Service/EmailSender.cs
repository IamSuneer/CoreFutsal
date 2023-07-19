//using CoreFutsal.Models;
//using Microsoft.AspNetCore.Identity.UI.Services;
//using Microsoft.Extensions.Options;
//using SendGrid;
//using SendGrid.Helpers.Mail;

//namespace CoreFutsal.Service
//{
//    public class EmailSender : IEmailSender
//    {
//        private readonly ILogger logger;

//        public EmailSender(IOptions<AuthMessageSenderOptions> optionsAccessor,
//                       ILogger<EmailSender> logger)
//        {
//            Options = optionsAccessor.Value;
//            this.logger = logger;
//        }
//        public AuthMessageSenderOptions Options { get; }

//        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
//        {
//            if (string.IsNullOrEmpty(Options.SendGridKey))
//            {
//                throw new Exception("Null SendGridKey");
//            }
//            await Execute(Options.SendGridKey, subject, htmlMessage, email);
//        }

//        public async Task Execute(string apiKey, string subject, string htmlMessage, string email)
//        {
//            var client = new SendGridClient(apiKey);
//            var msg = new SendGridMessage()
//            {
//                From = new EmailAddress("Joe@contoso.com", "Password Recovery"),
//                Subject = subject,
//                PlainTextContent = htmlMessage,
//                HtmlContent = htmlMessage
//            };
//            msg.AddTo(new EmailAddress(email));

//            // Disable click tracking.
//            // See https://sendgrid.com/docs/User_Guide/Settings/tracking.html
//            msg.SetClickTracking(false, false);
//            var response = await client.SendEmailAsync(msg);
//            this.logger.LogInformation(response.IsSuccessStatusCode
//                                   ? $"Email to {email} queued successfully!"
//                                   : $"Failure Email to {email}");
//        }
//    }
//}
