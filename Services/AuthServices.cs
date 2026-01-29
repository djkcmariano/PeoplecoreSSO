using AuthServer.Helpers;
using AuthServer.Models;
using Azure.Core;
using clsBase;
using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using static AuthServer.Helpers.SQLHelp;

namespace AuthServer.Services
{
    public class AuthService
    {
        private readonly JwtHelper _jwtHelper;

        public AuthService(JwtHelper jwtHelper, IHttpContextAccessor httpContextAccessor)
        {
            _jwtHelper = jwtHelper;
            _httpContextAccessor = httpContextAccessor; 
        }
        private readonly IHttpContextAccessor _httpContextAccessor;

        private string GetServerBaseUrl()
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request == null)
                return "https://localhost:7077"; // fallback if context is missing

            return $"{request.Scheme}://{request.Host}";
        }
        public string GetAudienceFromSystemID(string systemID)
        {
            var dt = SQLHelp.ExecuteDataTable("EGetValidAudience", new SqlParameter("@URL", Generic.ToStr(systemID)));
            if (dt != null && dt.Rows.Count > 0)
                return Generic.ToStr(dt.Rows[0]["SystemUrl"]);

            throw new InvalidOperationException($"Audience not registered for SystemID: {systemID}");
        }
        //public LoginResponseModel Authenticate(UserModel login, string callerUrl)
        public LoginResponseModel Authenticate( string username, string password, int payLocNo, string callerUrl )
        {

            

            var dt = SQLHelp.ExecuteDataTable("SUser_WebLoginAPI", new SqlParameter("@username", Generic.ToStr(username)),new SqlParameter("@xPayLocNo", Generic.ToInt(payLocNo)));
            if (dt == null || dt.Rows.Count == 0)
                return new LoginResponseModel { Success = false, XMessage = "Invalid username or password." };

            var row = dt.Rows[0];
            string decryptedPassword = PeopleCoreCrypt.Decrypt(Generic.ToStr(row["Password"]));

            var response = new LoginResponseModel
            {
                Retval = Generic.ToInt(row["Retval"]),
                OnlineUserNo = Generic.ToInt(row["OnlineUserNo"]),
                EmployeeNo = Generic.ToInt(row["EmployeeNo"]),
                Fullname = Generic.ToStr(row["Fullname"]),
                IsLock = Generic.ToBol(row["IsLock"]),
                PwdStatus = Generic.ToInt(row["PwdStatus"]),
                XMessage = Generic.ToStr(row["xMessage"]),
                EmployeeNumber = Generic.ToStr(row["EmployeeNumber"]),
                CompanyCode = payLocNo
            };

            if (!string.IsNullOrEmpty(response.XMessage))
                return new LoginResponseModel { Success = false, XMessage = response.XMessage };

            if (response.IsLock)
                return new LoginResponseModel { Success = false, XMessage = "Account is locked." };

            if (password != decryptedPassword)
                return new LoginResponseModel { Success = false, XMessage = "Invalid password." };

            string issuer = GetServerBaseUrl();
            string audience = callerUrl;

            response.Token = _jwtHelper.GenerateToken(
                new ClaimsIdentity(new[]
                {
            new Claim(ClaimTypes.NameIdentifier, response.OnlineUserNo.ToString()),
            new Claim(ClaimTypes.Name, response.Fullname ?? ""),
            new Claim("EmployeeNumber", response.EmployeeNumber ?? "")
                }),
                issuer,
                audience
            );

            SaveToken(response.OnlineUserNo.ToString(), response.Token, DateTime.UtcNow.AddHours(1), audience);
            response.Success = true;

            return response;
        }

        private void SaveToken(string userId, string token, DateTime expiry, string audience)
        {
            SQLHelp.ExecuteDataSet("API_UserToken_WebSave", userId, token, expiry, audience);
        }

        public void RevokeToken(string token)
        {
            SQLHelp.ExecuteDataSet("API_UserToken_Revoke", token);
        }
    }
}
