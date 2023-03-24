using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace webApi.EmailServices
{
    public class EmailService
    {
        public static bool SendEmail(string toMail ,MailMessage message)
        {
            message.To.Add(toMail);
            message.From = new MailAddress("info@ossdoy.com");
            message.IsBodyHtml = true;
            
            SmtpClient smtp = new SmtpClient();
            smtp.Credentials = new NetworkCredential("info@ossdoy.com", "emre.Akbas1804");
            smtp.Port = 587;
            smtp.Host = "rd-prime-win.guzelhosting.com";
            smtp.EnableSsl = false;
            try
            {
                smtp.Send(message);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
            
        }
        
    }
}