namespace TruckServices.EmailSender
{
    public class EmailSettings
    {
        public string FromEmail { get; set; }
        public string FromName { get; set; }
        public string SmtpHost { get; set; }          // e.g. "smtp.gmail.com"
        public int SmtpPort { get; set; }             // e.g. 587
        public string SmtpUsername { get; set; }      // Gmail email address
        public string SmtpPassword { get; set; }      // Gmail app password or account password
        public string CcEmail { get; set; }           // optional
    }

}
