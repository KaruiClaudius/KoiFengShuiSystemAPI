using KoiFengShuiSystem.Modules.Identity.Application.Abstractions;
using KoiFengShuiSystem.Shared.Helpers;

namespace KoiFengShuiSystem.Modules.Identity.Infrastructure.Email;

public class LegacyIdentityEmailSender : IIdentityEmailSender
{
    private readonly EmailService _emailService;

    public LegacyIdentityEmailSender(EmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task<bool> SendPasswordResetEmailAsync(string email, string fullName, string resetLink)
    {
        var mailData = new MailData
        {
            EmailToId = email,
            EmailToName = fullName,
            EmailBody = $@"
<div style=""max-width: 400px; margin: 50px auto; padding: 30px; text-align: center; font-size: 120%; background-color: #f9f9f9; border-radius: 10px; box-shadow: 0 0 20px rgba(0, 0, 0, 0.1); position: relative;"">
    <img src=""https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTRDn7YDq7gsgIdHOEP2_Mng6Ym3OzmvfUQvQ&usqp=CAU"" alt=""Noto Image"" style=""max-width: 100px; height: auto; display: block; margin: 0 auto; border-radius: 50%;"">
    <h2 style=""text-transform: uppercase; color: #3498db; margin-top: 20px; font-size: 28px; font-weight: bold;"">Password Reset</h2>
    <p>We received a request to reset your password.</p>
    <p><a href=""{resetLink}"" style=""display: inline-block; padding: 12px 24px; background-color: #3498db; color: #ffffff; text-decoration: none; border-radius: 5px; font-weight: bold;"">Reset your password</a></p>
    <p>This link is valid for 15 minutes. If you did not request a password reset, you can safely ignore this email.</p>
    <p style=""color: #888; font-size: 14px;"">Powered by KoiFengShui</p>
</div>",
            EmailSubject = "Password Reset"
        };

        return await _emailService.SendEmailAsync(mailData);
    }
}
