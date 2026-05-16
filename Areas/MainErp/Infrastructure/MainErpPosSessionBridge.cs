using MyERP.Areas.MainErp.Models.Security;
using MyERP.Areas.MainErp.Security;
using MyERP.Areas.MainErp.Services.Security;
using MyERP.Areas.Pos.Controllers;
using MyERP.Areas.Pos.Data;
using MyERP.Areas.Pos.Models;
using System;
using System.Web;
using System.Web.SessionState;

namespace MyERP.Areas.MainErp.Infrastructure
{
    public static class MainErpPosSessionBridge
    {
        public const string SourceSessionKey = "MainErp.AuthBridge.Source";
        public const string PosUserIdSessionKey = "MainErp.AuthBridge.PosUserId";
        public const string PosUserNameSessionKey = "MainErp.AuthBridge.PosUserName";
        public const string SourcePos = "POS";

        public static MainErpPosBridgeResult TryRestore(HttpRequestBase request, HttpSessionStateBase session)
        {
            if (session == null)
            {
                return MainErpPosBridgeResult.NotAttempted();
            }

            var posContext = RestorePosContext(request, session);
            if (posContext == null)
            {
                return MainErpPosBridgeResult.NotAttempted();
            }

            var mainErpContext = new MainErpLoginService().GetUserDefaults(posContext.UserId);
            if (mainErpContext == null)
            {
                return MainErpPosBridgeResult.Forbidden(
                    "تعذر فتح شاشة ERP المشتركة من POS لأن مستخدم POS الحالي غير مربوط بمستخدم MainErp نشط بنفس الرقم.");
            }

            ApplyMainErpSession(session, mainErpContext, posContext);
            return MainErpPosBridgeResult.Authenticated(mainErpContext);
        }

        public static bool IsExplicitPosNavigation(HttpRequestBase request)
        {
            if (request == null)
            {
                return false;
            }

            return string.Equals(request.QueryString["fromPos"], "1", StringComparison.OrdinalIgnoreCase);
        }

        private static PosUserContext RestorePosContext(HttpRequestBase request, HttpSessionStateBase session)
        {
            return PosLoginController.RestorePosContext(request, session, new PosSqlRepository());
        }

        private static void ApplyMainErpSession(HttpSessionStateBase session, MainErpUserContext mainErpContext, PosUserContext posContext)
        {
            session[MainErpSessionKeys.Context] = mainErpContext;
            session[MainErpSessionKeys.UserId] = mainErpContext.UserId;
            session[MainErpSessionKeys.UserName] = mainErpContext.UserName;
            session[MainErpSessionKeys.EmpId] = mainErpContext.EmpId;
            session[MainErpSessionKeys.BranchId] = mainErpContext.BranchId;
            session[MainErpSessionKeys.StoreId] = mainErpContext.StoreId;
            session[MainErpSessionKeys.BoxId] = mainErpContext.BoxId;
            session[SourceSessionKey] = SourcePos;
            session[PosUserIdSessionKey] = posContext.UserId;
            session[PosUserNameSessionKey] = posContext.UserName;
        }
    }

    public class MainErpPosBridgeResult
    {
        private MainErpPosBridgeResult()
        {
        }

        public bool Success { get; private set; }
        public bool PosAuthenticated { get; private set; }
        public string ErrorMessage { get; private set; }
        public MainErpUserContext Context { get; private set; }

        public static MainErpPosBridgeResult NotAttempted()
        {
            return new MainErpPosBridgeResult();
        }

        public static MainErpPosBridgeResult Forbidden(string message)
        {
            return new MainErpPosBridgeResult
            {
                PosAuthenticated = true,
                ErrorMessage = message
            };
        }

        public static MainErpPosBridgeResult Authenticated(MainErpUserContext context)
        {
            return new MainErpPosBridgeResult
            {
                Success = true,
                PosAuthenticated = true,
                Context = context
            };
        }
    }
}
