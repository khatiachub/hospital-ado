using MimeKit;

namespace hospital.models
{
    public class EmailConfiguration
    {
        public string Subject { get; set; }
        public string Body { get; set; }
        public string TextPart {get;set;}
        public string To { get; set; }
        public string Name { get; set; }
    }
}
