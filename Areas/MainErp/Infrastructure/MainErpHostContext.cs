using System;
using System.Web;
using System.Web.SessionState;

namespace MyERP.Areas.MainErp.Infrastructure
{
    public static class MainErpHostContext
    {
        public const string PosHostValue = "pos";
        public const string MainErpHostValue = "mainerp";

        public static bool IsPosHosted(HttpRequestBase request, HttpSessionStateBase session)
        {
            return IsExplicitPosHostedRequest(request);
        }

        public static bool IsPosHosted(HttpContext context)
        {
            if (context == null)
            {
                return false;
            }

            return IsExplicitPosHostedRequest(context.Request == null ? null : new HttpRequestWrapper(context.Request));
        }

        private static bool IsExplicitPosHostedRequest(HttpRequestBase request)
        {
            if (request == null)
            {
                return false;
            }

            if (string.Equals(request.QueryString["host"], MainErpHostValue, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.Equals(request.QueryString["host"], PosHostValue, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return string.Equals(request.QueryString["fromPos"], "1", StringComparison.OrdinalIgnoreCase);
        }
    }
}
