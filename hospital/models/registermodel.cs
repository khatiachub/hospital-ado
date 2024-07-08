namespace hospital.models
{
    public class registermodel
    {
        public int id { get; set; }
        public string name { get; set; }
        public string lastname { get; set; }
        public string email { get; set; }
        public string password { get; set; }
        public int emailconfirmed { get; set; }
        public int twofactorenabled { get; set; }
        public string role { get; set; }
        public string privatenumber { get; set; }
    }
}
