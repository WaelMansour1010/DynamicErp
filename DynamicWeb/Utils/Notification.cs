using MyERP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web;

namespace MyERP
{
    class Notification
    {
        public static void GetNotification(string controllerName, string engName, string ActionName, int? SelectedId, bool? IsActive, string PageName)
        {
            try
            {
                using (MySoftERPEntity db = new MySoftERPEntity())
                {
                    int pageid = db.SystemPages.Where(x => x.ControllerName == controllerName).Select(x => x.Id).FirstOrDefault();
                    var actionId = db.PageActions.Where(c => c.Action == ActionName && c.EnName == engName && c.PageId == pageid).Select(x => x.Id).FirstOrDefault();
                    int userId = int.Parse(((ClaimsIdentity)HttpContext.Current.User.Identity).FindFirst("Id").Value);

                    var playerIds = db.UserNotificationSettings.Where(x => x.ActionId == actionId && x.ERPUser.IsActive == true && x.ERPUser.IsDeleted == false && x.ERPUser.Id != userId).Select(x => x.ERPUser.PlayerId).ToArray();
                    string message = null;
                    var UserName = HttpContext.Current.User.Identity.Name;
                    DateTime utcNow = DateTime.UtcNow;
                    TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
                    DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);
                    if (ActionName == "AddEdit" && engName == "Edit")
                    {
                        message = $"قام المستخدم {UserName} بتعديل بيانات في شاشة {PageName}  ";
                        db.Sp_OccuredNotification(actionId, message, cTime, SelectedId, userId);
                    }

                    else if (ActionName == "AddEdit" && engName == "Add")
                    {
                        message = $"قام المستخدم {UserName} بإضافة بيانات في شاشة {PageName}  ";
                        db.Sp_OccuredNotification(actionId, message, cTime, SelectedId, userId);
                    }
                    else if (ActionName == "Index" && engName == "View")
                    {
                        message = $"قام المستخدم {UserName} بفتح بيانات في شاشة {PageName}  ";
                        db.Sp_OccuredNotification(actionId, message, cTime, SelectedId, userId);
                    }

                    else if (ActionName == "Delete" && engName == "Delete")
                    {
                        message = $"قام المستخدم {UserName} بحذف بيانات في شاشة {PageName}  ";
                        db.Sp_OccuredNotification(actionId, message, cTime, SelectedId, userId);
                    }
                    else if (ActionName == "Save" && engName == "Edit")
                    {
                        message = $"قام المستخدم {UserName} بتعديل بيانات في شاشة {PageName}  ";
                        db.Sp_OccuredNotification(actionId, message, cTime, SelectedId, userId);
                    }

                    else if (ActionName == "ActivateDeactivate" && engName == "Activate/Deactivate")
                    {
                        if (IsActive == true)
                        {
                            message = $"قام المستخدم {UserName} بتنشيط بيانات في شاشة {PageName}  ";
                            db.Sp_OccuredNotification(actionId, message, cTime, SelectedId, userId);
                        }
                        else
                        {
                            message = $"قام المستخدم {UserName} بإلغاء تنشيط بيانات في شاشة {PageName}  ";
                            db.Sp_OccuredNotification(actionId, message, cTime, SelectedId, userId);
                        }
                    }

                    SendNotifications.SendToUsers(playerIds, "Genoise", message, controllerName + "/" + ActionName + "/" + (SelectedId.HasValue ? SelectedId.Value.ToString() : "")).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
