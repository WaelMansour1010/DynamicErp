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
            if (request != null)
            {
                if (string.Equals(request.QueryString["host"], MainErpHostValue, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (string.Equals(request.QueryString["host"], PosHostValue, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (MainErpPosSessionBridge.IsExplicitPosNavigation(request))
                {
                    return true;
                }
            }

            return session != null
                && string.Equals(session[MainErpPosSessionBridge.SourceSessionKey] as string, MainErpPosSessionBridge.SourcePos, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsPosHosted(HttpContext context)
        {
            if (context == null)
            {
                return false;
            }

            if (context.Request != null)
            {
                if (string.Equals(context.Request.QueryString["host"], MainErpHostValue, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (string.Equals(context.Request.QueryString["host"], PosHostValue, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (string.Equals(context.Request.QueryString["fromPos"], "1", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return context.Session != null
                && string.Equals(context.Session[MainErpPosSessionBridge.SourceSessionKey] as string, MainErpPosSessionBridge.SourcePos, StringComparison.OrdinalIgnoreCase);
        }
    }
}
