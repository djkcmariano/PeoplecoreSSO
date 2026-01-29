namespace AuthServer.Models
{
    public class LoginResponseModel
    {
        public bool Success { get; set; }
        public int Retval { get; set; }
        public int OnlineUserNo { get; set; }
        public int EmployeeNo { get; set; }
        public string EmployeeNumber { get; set; }
        public string? Fullname { get; set; }
        public string? Password { get; set; }
        public bool IsLock { get; set; }
        public int PwdStatus { get; set; }
        public string? XMessage { get; set; }
        public string? Token { get; set; }
        public int CompanyCode { get; set; }
    }
}
