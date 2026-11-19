using System;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace OSR4Rights.Web
{
    public static class Helper
    {
        public static int GetLoginIdAsInt(HttpContext httpContext)
        {
            var loginIdString = httpContext.User.Claims.FirstOrDefault(x => x.Type == "LoginId")?.Value;

            var result = int.TryParse(loginIdString, out int loginId);

            if (result == false)
                throw new ApplicationException("Can't get loginId");

            if (loginId == 0)
                throw new ApplicationException("LoginId is 0");

            return loginId;
        }
    }
}
