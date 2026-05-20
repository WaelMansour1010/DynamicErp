using MyERP.Models.Branding;
using System;
using System.Web;
using System.Web.Mvc;

namespace MyERP.Services.Branding
{
    public static class LoginBrandingResolver
    {
        public static LoginBrandingContext ResolveMainErp(HttpServerUtilityBase server)
        {
            return new LoginBrandingContext
            {
                SystemKey = "MainErp",
                DisplayName = "DynamicErp MainErp",
                Title = "Main ERP Login",
                LogoPath = FirstExisting(server,
                    "~/assets/images/logo-dark.png",
                    "~/assets/images/logo-light.png"),
                ThemeCssVersion = "20260520-main-branding"
            };
        }

        public static LoginBrandingContext ResolvePos(HttpServerUtilityBase server)
        {
            return new LoginBrandingContext
            {
                SystemKey = "KishnyPos",
                DisplayName = "Kishny POS",
                Title = "POS Login",
                LogoPath = FirstExisting(server,
                    "~/Areas/Pos/assets/images/company-logo.png",
                    "~/Areas/Pos/assets/images/company-logo-1.png",
                    "~/Areas/Pos/assets/images/company-logo-2.png"),
                ThemeCssVersion = "20260520-pos-branding"
            };
        }

        public static LoginBrandingContext ResolveDefault(HttpServerUtilityBase server)
        {
            return new LoginBrandingContext
            {
                SystemKey = "MyErp",
                DisplayName = "DynamicErp",
                Title = "ERP Login",
                LogoPath = FirstExisting(server,
                    "~/assets/images/logo-light.png",
                    "~/assets/images/logo-dark.png"),
                ThemeCssVersion = "20260520-default-branding"
            };
        }

        public static LoginBrandingContext ResolveForRequest(ControllerContext context, string returnUrl)
        {
            var server = context != null && context.HttpContext != null ? context.HttpContext.Server : null;
            var path = context != null && context.HttpContext != null && context.HttpContext.Request != null
                ? context.HttpContext.Request.Path
                : string.Empty;

            if (ContainsPath(path, "/Pos") || ContainsPath(returnUrl, "/Pos"))
            {
                return ResolvePos(server);
            }

            if (ContainsPath(path, "/MainErp") || ContainsPath(returnUrl, "/MainErp"))
            {
                return ResolveMainErp(server);
            }

            return ResolveDefault(server);
        }

        private static bool ContainsPath(string value, string marker)
        {
            return !string.IsNullOrWhiteSpace(value) && value.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string FirstExisting(HttpServerUtilityBase server, params string[] candidates)
        {
            foreach (var candidate in candidates)
            {
                if (string.IsNullOrWhiteSpace(candidate))
                {
                    continue;
                }

                if (server == null)
                {
                    return candidate;
                }

                try
                {
                    if (System.IO.File.Exists(server.MapPath(candidate)))
                    {
                        return candidate;
                    }
                }
                catch
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
