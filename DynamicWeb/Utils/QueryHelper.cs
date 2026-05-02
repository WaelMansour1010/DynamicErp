using System;
using System.Collections.Generic;
using System.Linq;
using MyERP.Models;
using System.Data.Entity.Infrastructure;
using System.Web;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Net;
using System.IO;
using System.Globalization;

namespace MyERP
{
  public   class QueryHelper
    {
        private static readonly MySoftERPEntity db = new MySoftERPEntity();
        public static string ConvertToHijri(DateTime gregorianDate)
        {
            // Create an instance of HijriCalendar
            HijriCalendar hijriCalendar = new HijriCalendar();

            // Extract the year, month, and day in the Hijri calendar
            int hijriYear = hijriCalendar.GetYear(gregorianDate);
            int hijriMonth = hijriCalendar.GetMonth(gregorianDate);
            int hijriDay = hijriCalendar.GetDayOfMonth(gregorianDate);

            // Format the Hijri date as a string
            return $"{hijriYear}/{hijriMonth}/{hijriDay}";
        }
        public static double DocLastNum(int? depId, string table)
        {
            // var docNo = db.Database.SqlQuery<decimal>($"select ISNULL( max(convert(decimal, [DocumentNumber])),0) from [{ table}] where [DepartmentId] = " + depId/* + " order by [Id] desc"*/);
            var docNo = db.Database.SqlQuery<string>($"select isnull((select top(1) DocumentNumber from [{ table}] where [DepartmentId] = " + depId + "order by  [Id] desc),0)");
            //if (docNo.FirstOrDefault() == 0)
            //{
            //    return 0;
            //}

            //SystemSetting systemSetting = db.SystemSettings.Any() ? db.SystemSettings.FirstOrDefault() : new SystemSetting();
            //if (table == "SalesInvoice" /*&& ( (systemSetting.Separator != null&& docNo.FirstOrDefault().ToString().Contains(systemSetting.Separator)|| (systemSetting.FixedPart != null && docNo.FirstOrDefault().ToString().Contains(systemSetting.FixedPart))))*/)
            //{
            //    var x = docNo.FirstOrDefault().ToString().Substring(docNo.FirstOrDefault().ToString().Length- (int)systemSetting.NoOfDigits);
            //    //var separator = char.Parse(systemSetting.Separator);
            //    //var x = docNo.FirstOrDefault().ToString().Split(separator).Last();
            //    return double.Parse(x);
            //}

            return double.Parse(docNo.FirstOrDefault().ToString());
        }

        public static double CodeLastNum(string table)
        {
            var code = db.Database.SqlQuery<string>($"select top(1)[Code] from [{table}] order by [Id] desc");
            if (code.FirstOrDefault() == null)
            {
                return 0;
            }
            return double.Parse(code.FirstOrDefault().ToString());
        }
        public static double OrderLastNum(string table)
        {
            var code = db.Database.SqlQuery<string>($"select top(1)[OrderNumber] from [{table}] order by [Id] desc");
            if (code.FirstOrDefault() == null)
            {
                return 0;
            }
            return double.Parse(code.FirstOrDefault().ToString());
        }
        public static int SourcePageId(string table)
        {
            var id = db.Database.SqlQuery<int>($"select [Id] from [SystemPage] where [TableName]= '{table}'");
            return id.First();
        }
        public static List<int?> CashTransferJournalIds(int? posReceiptVoucherId, int? cashTransferPageId)
        {
            int sysPageId = QueryHelper.SourcePageId("PosReceiptVoucher");
            //  List<int?> cashTransferIds = db.CashTransfers.Where(c => c.SystemPageId == sysPageId && c.SelectedId == posReceiptVoucherId).Select(s => s.Id).ToList();

            var cashTransferIds = db.Database.SqlQuery<int?>($"select [Id] from [CashTransfer] where [SystemPageId]= '{sysPageId}' and  [SelectedId] = '{posReceiptVoucherId}'  ");

            return cashTransferIds.ToList();
        }
        public static string GetSystemPageDocNo(int? systemPageId, int? selectedId)
        {
            var tableName = db.SystemPages.Where(s => s.Id == systemPageId).FirstOrDefault().TableName;
            var DocNo = db.Database.SqlQuery<string>($"select DocumentNumber from " + tableName + " where Id=" + selectedId).FirstOrDefault();
            return DocNo;
        }
        public static int? Previous(int id, string table)
        {
            var per = db.Database.SqlQuery<int?>($"select Top(1)Id from [{table}]  where Id in(select Max(Id)from [{table}]  where Id < " + id + ") and [IsDeleted]=0").FirstOrDefault();
            return per;
        }
        public static int? Next(int id, string table)
        {
            var next = db.Database.SqlQuery<int?>($"select Top(1)Id from [{table}] where Id in(select Min(Id)from [{table}] where Id >" + id + ") and [IsDeleted]=0").FirstOrDefault();
            return next;
        }

        public static int? GetLast(string table)
        {
            var last = db.Database.SqlQuery<int?>($" select max(Id)from [{table}] where [IsDeleted]=0").FirstOrDefault();
            return last;
        }
        public static int? GetFirst(string table)
        {
            var first = db.Database.SqlQuery<int?>($" select min(Id)from [{table}] where [IsDeleted]=0").FirstOrDefault();
            return first;
        }
        public static List<DocumentNumberDate> DeletedTransactions(string table, DateTime? from, DateTime? to)
        {
            if (from == null && to == null)
            {
                try
                {
                    return db.Database.SqlQuery<DocumentNumberDate>($"select [Id], [DocumentNumber], [Date] from [{table}] where [IsDeleted]=1").ToList();
                }
                catch (System.Data.SqlClient.SqlException)
                {
                    return db.Database.SqlQuery<DocumentNumberDate>($"select [Id], [DocumentNumber], [VoucherDate] [Date] from [{table}] where [IsDeleted]=1").ToList();
                }

            }
            else if (from != null && to == null)
            {
                try
                {
                    return db.Database.SqlQuery<DocumentNumberDate>($"select [Id], [DocumentNumber], [Date] from [{table}] where [IsDeleted]=1 and ([Date] >= '{from.Value.ToString("yyyy-MM-dd HH:mm:ss.fff")}')").ToList();
                }
                catch (System.Data.SqlClient.SqlException)
                {
                    return db.Database.SqlQuery<DocumentNumberDate>($"select [Id], [DocumentNumber], [VoucherDate] [Date] from [{table}] where [IsDeleted]=1 and ([VoucherDate] >= '{from.Value.ToString("yyyy-MM-dd HH:mm:ss.fff")}')").ToList();
                }

            }
            else if (from == null && to != null)
            {
                try
                {
                    return db.Database.SqlQuery<DocumentNumberDate>($"select [Id], [DocumentNumber], [Date] from [{table}] where [IsDeleted]=1 and ([Date] <= '{to.Value.ToString("yyyy-MM-dd HH:mm:ss.fff")}')").ToList();
                }
                catch (System.Data.SqlClient.SqlException)
                {
                    return db.Database.SqlQuery<DocumentNumberDate>($"select [Id], [DocumentNumber], [VoucherDate] [Date] from [{table}] where [IsDeleted]=1 and ([VoucherDate] <= '{to.Value.ToString("yyyy-MM-dd HH:mm:ss.fff")}')").ToList();
                }

            }
            else
            {
                try
                {
                    return db.Database.SqlQuery<DocumentNumberDate>($"select [Id], [DocumentNumber], [Date] from [{table}] where [IsDeleted]=1 and ([Date] >= '{from.Value.ToString("yyyy-MM-dd HH:mm:ss.fff")}' and [Date] <= '{to.Value.ToString("yyyy-MM-dd HH:mm:ss.fff")}')").ToList();

                }
                catch (System.Data.SqlClient.SqlException)
                {
                    return db.Database.SqlQuery<DocumentNumberDate>($"select [Id], [DocumentNumber], [VoucherDate] [Date] from [{table}] where [IsDeleted]=1 and ([VoucherDate] >= '{from.Value.ToString("yyyy-MM-dd HH:mm:ss.fff")}' and [VoucherDate] <= '{to.Value.ToString("yyyy-MM-dd HH:mm:ss.fff")}')").ToList();
                }

            }

        }
        public static DbRawSqlQuery<DocumentNumberDate> UpdatedTransactions(string table, DateTime? from, DateTime? to)
        {
            int systemPageId = db.Database.SqlQuery<int>($"select [Id] from [SystemPage] where [TableName]='{table}'").FirstOrDefault();

            if (from == null && to == null)
            {
                return db.Database.SqlQuery<DocumentNumberDate>($"select [Id], [DocumentNumber], [VoucherDate] from [UpdatedTransaction] where [SystemPageId]={systemPageId}");
            }
            else if (from != null && to == null)
            {
                return db.Database.SqlQuery<DocumentNumberDate>($"select [Id], [DocumentNumber], [VoucherDate] from [UpdatedTransaction] where [SystemPageId]={systemPageId} and ([VoucherDate] >= '{from.Value.ToString("yyyy-MM-dd HH:mm:ss.fff")}')");
            }
            else if (from == null && to != null)
            {
                return db.Database.SqlQuery<DocumentNumberDate>($"select [Id], [DocumentNumber], [VoucherDate] from [UpdatedTransaction] where [SystemPageId]={systemPageId} and ([VoucherDate] <= '{to.Value.ToString("yyyy-MM-dd HH:mm:ss.fff")}')");
            }
            else
            {
                return db.Database.SqlQuery<DocumentNumberDate>($"select [Id], [DocumentNumber], [VoucherDate] from [UpdatedTransaction] where [SystemPageId]={systemPageId} and ([VoucherDate] >= '{from.Value.ToString("yyyy-MM-dd HH:mm:ss.fff")}' and [Date] <= '{to.Value.ToString("yyyy-MM-dd HH:mm:ss.fff")}')");
            }

        }
        public static void AddLog(MyLog log)
        {
            DateTime utcNow = DateTime.UtcNow;
            TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);
            log.LogDate = cTime;
            db.MyLogs.Add(log);
            db.SaveChanges();
        }

        public static void RemoveDefaultCurrency(string table)
        {
            int noOfRowUpdated = db.Database.ExecuteSqlCommand($" update [{table}] set [IsDefault]=0");
            db.SaveChanges();
        }

        public static void AddDBChange(DBChange dBChange)
        {
            db.DBChanges.Add(dBChange);
            db.SaveChanges();
        }

        public static T GetOnlineChange<T>(string tablePath, string tableNameStr, int SelectedId)
        {
            //var dbChangeTable = tablePath + "DBChange";
            //var Change = db.Database.SqlQuery<ERPUser>($"select * from {tablePath + tableNameStr} OnlineTable inner join {dbChangeTable} on SelectedId= OnlineTable.id  where OnlineTable.id in (select SelectedId from {dbChangeTable} where TableName = '{ tableNameStr}' ) and TableName = '{tableNameStr}'").ToList();


            var dbChangeTable = tablePath + "DBChange";
            object Change = db.Database.SqlQuery<T>($"select * from {tablePath + tableNameStr} OnlineTable where Id = {SelectedId}").FirstOrDefault();
            return (T)Change;
        }

        public static bool CopyTableData(string onlineTable)
        {


            db.Database.CommandTimeout = 36000;

            // SystemPageInsert
            var systemPageInsert = $" declare @maxId int set @maxId = (select max(Id)from SystemPage) SET IDENTITY_INSERT dbo.SystemPage ON  insert into SystemPage([Id],[Code],[ArName],[EnName],[IsMasterFile],[TableName],[ControllerName],[IsTransaction],[IsUpdated],[IsReport],[ParentId],[IsActive],[IsDeleted],[IsModule],[PageCode],[ShowInReportPage]) select *from {onlineTable}SystemPage where Id > @maxId SET IDENTITY_INSERT dbo.SystemPage OFF";

            // update sys sitting

            // PageAction Insert
            var pageActionInsert = $" set @maxId = (select max(Id)from PageAction) SET IDENTITY_INSERT dbo.PageAction ON  insert into PageAction([Id],[PageId],[Action],[EnName],[ArName],[IsActive]) select *from {onlineTable}PageAction where Id > @maxId  SET IDENTITY_INSERT dbo.PageAction OFF ";

            //ERPUser Insert
            var erpUserInsert = $" SET IDENTITY_INSERT dbo.ERPUser ON INSERT INTO dbo.ERPUser ([Id],[Name],[UserName],[Password],[Email],[EmployeeId],[RoleId],[SystemAdmin],[UserId],[IsDeleted],[IsActive],[PlayerId],[IsPasswordReset],[IsCashier],[CustodyBoxId]) select onlineERPUser.[Id],[Name],[UserName],[Password],[Email],[EmployeeId],[RoleId],[SystemAdmin],[UserId],[IsDeleted],[IsActive],[PlayerId],[IsPasswordReset],[IsCashier],[CustodyBoxId] from {onlineTable}ERPUser onlineERPUser inner join {onlineTable}DBChange onlineDBChange on onlineDBChange.SelectedId = onlineERPUser.id  where onlineERPUser.Id in (select SelectedId from {onlineTable}DBChange where TableName = 'ErpUser') and onlineDBChange.TableName = 'ErpUser' and onlineDBChange.IsNew=1 SET IDENTITY_INSERT dbo.ERPUser OFF";


            //ERPUser Update
            var erpUserUpdate = $" update LocERPUser SET LocERPUser.[Name] = OnERPUser.[Name],LocERPUser.[UserName] = OnERPUser.[UserName],LocERPUser.[Password] = OnERPUser.[Password],LocERPUser.[Email] = OnERPUser.[Email],LocERPUser.[EmployeeId] = OnERPUser.[EmployeeId],LocERPUser.[RoleId] = OnERPUser.[RoleId],LocERPUser.[SystemAdmin] = OnERPUser.[SystemAdmin],LocERPUser.[UserId] = OnERPUser.[UserId],LocERPUser.[IsDeleted] = OnERPUser.[IsDeleted],LocERPUser.[IsActive] = OnERPUser.[IsActive],LocERPUser.[PlayerId] = OnERPUser.[PlayerId],LocERPUser.[IsPasswordReset] = OnERPUser.[IsPasswordReset],LocERPUser.[IsCashier] = OnERPUser.[IsCashier],LocERPUser.[CustodyBoxId] = OnERPUser.[CustodyBoxId] FROM ERPUser AS LocERPUser INNER JOIN {onlineTable}ERPUser AS OnERPUser ON LocERPUser.Id = OnERPUser.Id inner join {onlineTable}DBChange on SelectedId = OnERPUser.id  where OnERPUser.Id in (select SelectedId from {onlineTable}DBChange where TableName = 'ErpUser' and IsNew = 0) and TableName = 'ErpUser'";

            // UserWarehouse Insert
            var userWarehouseInsert = $" delete from UserWarehouse  insert into UserWarehouse (UserId,WarehouseId,Privilege) select UserId,WarehouseId,Privilege from {onlineTable}UserWarehouse";

            // UserDepartment Insert
            var userDepartmentInsert = $"   delete from UserDepartment  insert into UserDepartment (UserId,DepartmentId,Privilege) select UserId,DepartmentId,Privilege from {onlineTable}UserDepartment";

            // UserPosInsert
            var userPosInsert = $"  delete from UserPos  insert into UserPos (UserId,PosId,Privilege) select UserId,PosId,Privilege from {onlineTable}UserPos";

            // UserCashBoxInsert 
            var userCashBoxInsert = $"   delete from UserCashBox  insert into UserCashBox (UserId,CashBoxId,Privilege) select UserId,CashBoxId,Privilege from {onlineTable}UserCashBox";

            // RolePrivilege Insert
            var rolePrivilegeInsert = $" delete from RolePrivilege  insert into RolePrivilege (RoleId,PageId,ActionId,Privileged) select RoleId,PageId,ActionId,Privileged from {onlineTable}RolePrivilege";

            // UserPrivilege Insert
            var userPrivilegeInsert = $" delete from UserPrivilege  insert into UserPrivilege (UserId,PageId,ActionId,Privileged) select UserId,PageId,ActionId,Privileged from {onlineTable}UserPrivilege ";



            // ShiftEmployee Insert
            var shiftEmployeeInsert = $"  delete from ShiftEmployee  insert into ShiftEmployee ([ShiftId],[EmployeeId]) select [ShiftId],[EmployeeId] from {onlineTable}ShiftEmployee";

            //Item Insert
            var itemInsert = $" SET IDENTITY_INSERT dbo.Item ON INSERT INTO dbo.Item ([Id],[Code],[ArName],[EnName],[KrName],[OrderLimit],[ItemGroupId],[ItemTypeId],[HasWarranty],[HasExpiry],[WarrantyDays],[WarrantyMonths],[WarrantyYears],[IsActive],[IsDeleted],[IsPosted],[IsLinked],[UserId],[Notes],[ArNotes],[KrNotes],[Image],[IdOnWebSite],[ProductNo],[VirtualQuantity],[EnDetails],[ArDetails],[KrDetails]) select onlineItem.[Id],[Code],[ArName],[EnName],[KrName],[OrderLimit],[ItemGroupId],[ItemTypeId],[HasWarranty],[HasExpiry],[WarrantyDays],[WarrantyMonths],[WarrantyYears],[IsActive],[IsDeleted],[IsPosted],[IsLinked],[UserId],[Notes],[ArNotes],[KrNotes],[Image],[IdOnWebSite],[ProductNo],[VirtualQuantity],[EnDetails],[ArDetails],[KrDetails] from {onlineTable}Item onlineItem inner join {onlineTable}DBChange onlineDBChange on onlineDBChange.SelectedId = onlineItem.id  where onlineItem.Id in (select SelectedId from {onlineTable}DBChange where TableName = 'Item') and onlineDBChange.TableName = 'Item' and onlineDBChange.IsNew=1 SET IDENTITY_INSERT dbo.Item OFF";

            //Item Update
            var itemUpdate = $" update LocItem SET LocItem.[Code] = OnItem.[Code],LocItem.[ArName] = OnItem.[ArName],LocItem.[EnName] = OnItem.[EnName],LocItem.[KrName] = OnItem.[KrName],LocItem.[OrderLimit] = OnItem.[OrderLimit],LocItem.[ItemGroupId] = OnItem.[ItemGroupId],LocItem.[ItemTypeId] = OnItem.[ItemTypeId],LocItem.[HasWarranty] = OnItem.[HasWarranty],LocItem.[HasExpiry] = OnItem.[HasExpiry],LocItem.[WarrantyDays] = OnItem.[WarrantyDays],LocItem.[WarrantyMonths] = OnItem.[WarrantyMonths],LocItem.[WarrantyYears] = OnItem.[WarrantyYears],LocItem.[IsActive] = OnItem.[IsActive],LocItem.[IsDeleted] = OnItem.[IsDeleted],LocItem.[IsPosted] = OnItem.[IsPosted],LocItem.[IsLinked] = OnItem.[IsLinked],LocItem.[UserId] = OnItem.[UserId],LocItem.[Notes] = OnItem.[Notes],LocItem.[ArNotes] = OnItem.[ArNotes],LocItem.[KrNotes] = OnItem.[KrNotes],LocItem.[Image] = OnItem.[Image],LocItem.[IdOnWebSite] = OnItem.[IdOnWebSite],LocItem.[ProductNo] = OnItem.[ProductNo],LocItem.[VirtualQuantity] = OnItem.[VirtualQuantity],LocItem.[EnDetails] = OnItem.[EnDetails],LocItem.[ArDetails] = OnItem.[ArDetails],LocItem.[KrDetails] = OnItem.[KrDetails] FROM Item AS LocItem INNER JOIN {onlineTable}Item AS OnItem ON LocItem.Id = OnItem.Id inner join {onlineTable}DBChange on SelectedId = OnItem.id  where OnItem.Id in (select SelectedId from {onlineTable}DBChange where TableName = 'Item' and IsNew = 0) and TableName = 'Item'";

            //ItemGroup Insert
            var itemGroupInsert = $" SET IDENTITY_INSERT dbo.ItemGroup ON INSERT INTO dbo.ItemGroup ( [Id],[Code],[ArName],[EnName],[IsDeleted],[IsActive],[UserId],[IsInAllDepartments],[IsInPos],[Photo],[ParentItemGroupId],[Image],[KrName],[ShowInMenu],[ShowInMainPage],[MenuOrder],[MainPageOrder]) select onlineItemGroup.[Id],[Code],[ArName],[EnName],[IsDeleted],[IsActive],[UserId],[IsInAllDepartments],[IsInPos],[Photo],[ParentItemGroupId],[Image],[KrName],[ShowInMenu],[ShowInMainPage],[MenuOrder],[MainPageOrder] from {onlineTable}ItemGroup onlineItemGroup inner join {onlineTable}DBChange onlineDBChange on onlineDBChange.SelectedId = onlineItemGroup.id  where onlineItemGroup.Id in (select SelectedId from {onlineTable}DBChange where TableName = 'ItemGroup') and onlineDBChange.TableName = 'ItemGroup' and onlineDBChange.IsNew=1 SET IDENTITY_INSERT dbo.ItemGroup OFF";


            //ItemGroup Update
            var itemGroupUpdate = $" update LocItemGroup SET LocItemGroup.[Code] = OnItemGroup.[Code],LocItemGroup.[ArName] = OnItemGroup.[ArName],LocItemGroup.[EnName] = OnItemGroup.[EnName],LocItemGroup.[IsDeleted] = OnItemGroup.[IsDeleted],LocItemGroup.[IsActive] = OnItemGroup.[IsActive],LocItemGroup.[UserId] = OnItemGroup.[UserId],LocItemGroup.[IsInAllDepartments] = OnItemGroup.[IsInAllDepartments],LocItemGroup.[IsInPos] = OnItemGroup.[IsInPos],LocItemGroup.[Photo] = OnItemGroup.[Photo],LocItemGroup.[ParentItemGroupId] = OnItemGroup.[ParentItemGroupId],LocItemGroup.[Image] = OnItemGroup.[Image],LocItemGroup.[KrName] = OnItemGroup.[KrName],LocItemGroup.[ShowInMenu] = OnItemGroup.[ShowInMenu],LocItemGroup.[ShowInMainPage] = OnItemGroup.[ShowInMainPage],LocItemGroup.[MenuOrder] = OnItemGroup.[MenuOrder],LocItemGroup.[MainPageOrder] = OnItemGroup.[MainPageOrder] FROM ItemGroup AS LocItemGroup INNER JOIN {onlineTable}ItemGroup AS OnItemGroup ON LocItemGroup.Id = OnItemGroup.Id inner join {onlineTable}DBChange on SelectedId = OnItemGroup.id  where OnItemGroup.Id in (select SelectedId from {onlineTable}DBChange where TableName = 'ItemGroup' and IsNew = 0) and TableName = 'ItemGroup'";

            //ItemUnit Insert
            var itemUnitInsert = $" SET IDENTITY_INSERT dbo.ItemUnit ON INSERT INTO dbo.ItemUnit ([Id],[Code],[ArName],[EnName],[ParentId],[Equivalent],[IsDeleted],[IsActive],[UserId],[Image],[Notes]) select onlineItemUnit.[Id],[Code],[ArName],[EnName],[ParentId],[Equivalent],[IsDeleted],[IsActive],[UserId],[Image],[Notes] from {onlineTable}ItemUnit onlineItemUnit inner join {onlineTable}DBChange onlineDBChange on onlineDBChange.SelectedId = onlineItemUnit.id  where onlineItemUnit.Id in (select SelectedId from {onlineTable}DBChange where TableName = 'ItemUnit') and onlineDBChange.TableName = 'ItemUnit' and onlineDBChange.IsNew=1 SET IDENTITY_INSERT dbo.ItemUnit OFF";




            //ItemUnit Update
            var itemUnitUpdate = $" update LocItemUnit SET LocItemUnit.[Code] = OnItemUnit.[Code], LocItemUnit.[ArName] = OnItemUnit.[ArName], LocItemUnit.[EnName] = OnItemUnit.[EnName], LocItemUnit.[ParentId] = OnItemUnit.[ParentId], LocItemUnit.[Equivalent] = OnItemUnit.[Equivalent], LocItemUnit.[IsDeleted] = OnItemUnit.[IsDeleted], LocItemUnit.[IsActive] = OnItemUnit.[IsActive], LocItemUnit.[UserId] = OnItemUnit.[UserId], LocItemUnit.[Image] = OnItemUnit.[Image], LocItemUnit.[Notes] = OnItemUnit.[Notes] FROM ItemUnit AS LocItemUnit INNER JOIN {onlineTable}ItemUnit AS OnItemUnit ON LocItemUnit.Id = OnItemUnit.Id inner join {onlineTable}DBChange on SelectedId = OnItemUnit.id  where OnItemUnit.Id in (select SelectedId from {onlineTable}DBChange where TableName = 'ItemUnit' and IsNew = 0) and TableName = 'ItemUnit'";

            //ItemPrice Insert
            var itemPriceInsert = $" SET IDENTITY_INSERT dbo.ItemPrice ON Insert Into ItemPrice ([Id],[Barcode],[ItemId],[ItemUnitId],[Price],[Equivalent],[CustomerGroupId],[IsDefault],[IsActive],[IsDeleted],[IsPosted],[IsLinked],[UserId],[Image],[Notes],[ToleranceQty],[FactoryPrice],[VendorId],[PriceBeforeDiscount]) SELECT OnItemPrice.[Id],[Barcode],[ItemId],[ItemUnitId],[Price],[Equivalent],[CustomerGroupId],[IsDefault],[IsActive],[IsDeleted],[IsPosted],[IsLinked],[UserId],[Image],[Notes],[ToleranceQty],[FactoryPrice],[VendorId],[PriceBeforeDiscount] FROM {onlineTable}[ItemPrice] OnItemPrice inner join {onlineTable}DBChange OnDBChange On OnDBChange.SelectedId = OnItemPrice.ItemId where OnDBChange.IsNew = 1 AND OnDBChange.TableName = 'Item' AND OnItemPrice.Id NOT IN (SELECT Id from dbo.[ItemPrice]) SET IDENTITY_INSERT dbo.ItemPrice OFF ";

            //ItemPrice Update
            var itemPriceUpdate = $" UPDATE LocItemPrice SET LocItemPrice.[Barcode] = OnItemPrice.[Barcode], LocItemPrice.[ItemId] = OnItemPrice.[ItemId], LocItemPrice.[ItemUnitId] = OnItemPrice.[ItemUnitId], LocItemPrice.[Price] = OnItemPrice.[Price], LocItemPrice.[Equivalent] = OnItemPrice.[Equivalent], LocItemPrice.[CustomerGroupId] = OnItemPrice.[CustomerGroupId], LocItemPrice.[IsDefault] = OnItemPrice.[IsDefault], LocItemPrice.[IsActive] = OnItemPrice.[IsActive], LocItemPrice.[IsDeleted] = OnItemPrice.[IsDeleted], LocItemPrice.[IsPosted] = OnItemPrice.[IsPosted], LocItemPrice.[IsLinked] = OnItemPrice.[IsLinked], LocItemPrice.[UserId] = OnItemPrice.[UserId], LocItemPrice.[Image] = OnItemPrice.[Image], LocItemPrice.[Notes] = OnItemPrice.[Notes], LocItemPrice.[ToleranceQty] = OnItemPrice.[ToleranceQty], LocItemPrice.[FactoryPrice] = OnItemPrice.[FactoryPrice], LocItemPrice.[VendorId] = OnItemPrice.[VendorId], LocItemPrice.[PriceBeforeDiscount] = OnItemPrice.[PriceBeforeDiscount] FROM ItemPrice AS LocItemPrice INNER JOIN {onlineTable}ItemPrice AS OnItemPrice ON LocItemPrice.Id = OnItemPrice.Id inner join {onlineTable}DBChange on SelectedId = OnItemPrice.ItemId   where OnItemPrice.ItemId  in (select SelectedId from {onlineTable}DBChange where TableName = 'Item' and IsNew = 0) and TableName = 'Item'";

            //POS Insert
            var posInsert = $" SET IDENTITY_INSERT dbo.Pos ON INSERT INTO dbo.Pos ([Id],[Code],[ArName],[EnName],[PosManagerId],[DepartmentId],[PosStatusId],[CurrentCashierUserId],[IsActive],[IsDeleted],[UserId],[Notes],[Image],[CurrentShiftId]) select onlinePos.[Id],[Code],[ArName],[EnName],[PosManagerId],[DepartmentId],[PosStatusId],[CurrentCashierUserId],[IsActive],[IsDeleted],[UserId],[Notes],[Image],[CurrentShiftId] from {onlineTable}Pos onlinePos inner join {onlineTable}DBChange onlineDBChange on onlineDBChange.SelectedId = onlinePos.id  where onlinePos.Id in (select SelectedId from {onlineTable}DBChange where TableName = 'Pos') and onlineDBChange.TableName = 'Pos' and onlineDBChange.IsNew=1 SET IDENTITY_INSERT dbo.Pos OFF";


            //Pos Update
            var posUpdate = $" update LocPos SET LocPos.[Code] = OnPos.[Code], LocPos.[ArName] = OnPos.[ArName], LocPos.[EnName] = OnPos.[EnName], LocPos.[PosManagerId] = OnPos.[PosManagerId], LocPos.[DepartmentId] = OnPos.[DepartmentId], LocPos.[PosStatusId] = OnPos.[PosStatusId], LocPos.[CurrentCashierUserId] = OnPos.[CurrentCashierUserId], LocPos.[IsActive] = OnPos.[IsActive], LocPos.[IsDeleted] = OnPos.[IsDeleted], LocPos.[UserId] = OnPos.[UserId], LocPos.[Notes] = OnPos.[Notes], LocPos.[Image] = OnPos.[Image], LocPos.[CurrentShiftId] = OnPos.[CurrentShiftId]  FROM Pos AS LocPos INNER JOIN {onlineTable}Pos AS OnPos ON LocPos.Id = OnPos.Id inner join {onlineTable}DBChange on SelectedId = OnPos.id  where OnPos.Id in (select SelectedId from {onlineTable}DBChange where TableName = 'Pos' and IsNew = 0) and TableName = 'Pos'";

            //Shift Insert
            var shiftInsert = $" SET IDENTITY_INSERT dbo.Shift ON INSERT INTO dbo.Shift ([Id],[Code],[ArName],[EnName],[DepartmentId],[IsActive],[IsDeleted],[UserId],[Notes],[Image]) select onlineShift.[Id],[Code],[ArName],[EnName],onlineShift.[DepartmentId],[IsActive],[IsDeleted],[UserId],[Notes],[Image] from {onlineTable}Shift onlineShift inner join {onlineTable}DBChange onlineDBChange on onlineDBChange.SelectedId = onlineShift.id  where onlineShift.Id in (select SelectedId from {onlineTable}DBChange where TableName = 'Shift') and onlineDBChange.TableName = 'Shift' and onlineDBChange.IsNew=1 SET IDENTITY_INSERT dbo.Shift OFF";

            //Shift Update
            var shiftUpdate = $" update LocShift SET LocShift.[Code] = OnShift.[Code],LocShift.[ArName] = OnShift.[ArName],LocShift.[EnName] = OnShift.[EnName],LocShift.[DepartmentId] = OnShift.[DepartmentId],LocShift.[IsActive] = OnShift.[IsActive],LocShift.[IsDeleted] = OnShift.[IsDeleted],LocShift.[UserId] = OnShift.[UserId],LocShift.[Notes] = OnShift.[Notes],LocShift.[Image] = OnShift.[Image] FROM Shift AS LocShift INNER JOIN {onlineTable}Shift AS OnShift ON LocShift.Id = OnShift.Id inner join {onlineTable}DBChange on SelectedId = OnShift.id  where OnShift.Id in (select SelectedId from {onlineTable}DBChange where TableName = 'Shift' and IsNew = 0) and TableName = 'Shift'";



            //PaymentMethod Insert
            var paymentMethodInsert = $" SET IDENTITY_INSERT dbo.PaymentMethod ON INSERT INTO dbo.PaymentMethod ([Id],[Code],[ArName],[EnName],[ForPos],[Commission],[IsActive],[IsDeleted],[AccountId],[CommissionAccountId]) select onlinePaymentMethod.[Id],[Code],[ArName],[EnName],[ForPos],[Commission],[IsActive],[IsDeleted],[AccountId],[CommissionAccountId] from {onlineTable}PaymentMethod onlinePaymentMethod inner join {onlineTable}DBChange onlineDBChange on onlineDBChange.SelectedId = onlinePaymentMethod.id  where onlinePaymentMethod.Id in (select SelectedId from {onlineTable}DBChange where TableName = 'PaymentMethod') and onlineDBChange.TableName = 'PaymentMethod' and onlineDBChange.IsNew=1 SET IDENTITY_INSERT dbo.PaymentMethod OFF";


            //PaymentMethod Update
            var paymentMethodUpdate = $" update LocPaymentMethod SET LocPaymentMethod.[Code] = OnPaymentMethod.[Code],LocPaymentMethod.[ArName] = OnPaymentMethod.[ArName],LocPaymentMethod.[EnName] = OnPaymentMethod.[EnName],LocPaymentMethod.[ForPos] = OnPaymentMethod.[ForPos],LocPaymentMethod.[Commission] = OnPaymentMethod.[Commission],LocPaymentMethod.[IsActive] = OnPaymentMethod.[IsActive],LocPaymentMethod.[IsDeleted] = OnPaymentMethod.[IsDeleted],LocPaymentMethod.[AccountId] = OnPaymentMethod.[AccountId],LocPaymentMethod.[CommissionAccountId] = OnPaymentMethod.[CommissionAccountId] FROM PaymentMethod AS LocPaymentMethod INNER JOIN {onlineTable}PaymentMethod AS OnPaymentMethod ON LocPaymentMethod.Id = OnPaymentMethod.Id inner join {onlineTable}DBChange on SelectedId = OnPaymentMethod.id  where OnPaymentMethod.Id in (select SelectedId from {onlineTable}DBChange where TableName = 'PaymentMethod' and IsNew = 0) and TableName = 'PaymentMethod'";



            //CashIssueVoucher Insert
            var cashIssueVoucherInsert = $" SET IDENTITY_INSERT dbo.CashIssueVoucher ON INSERT INTO dbo.CashIssueVoucher ([Id],[DocumentNumber],[BranchId],[MoneyAmount],[SourceTypeId],[DirectExpensesId],[OtherSourceName],[Date],[CurrencyId],[AccountId],[IsLinked],[IsPosted],[IsActive],[IsDeleted],[UserId],[Notes],[Image],[CustomerId],[VendorId],[EmployeeId],[CurrencyEquivalent],[DepartmentId],[CashBoxId],[ShareholderId],[TechnicianId],[CostCenterId],[PosId],[CashierUserId],[ShiftId],[IsCollected],[IsClosed]) select onlineCashIssueVoucher.[Id],[DocumentNumber],[BranchId],[MoneyAmount],[SourceTypeId],[DirectExpensesId],[OtherSourceName],[Date],[CurrencyId],[AccountId],[IsLinked],[IsPosted],[IsActive],[IsDeleted],[UserId],[Notes],[Image],[CustomerId],[VendorId],[EmployeeId],[CurrencyEquivalent],onlineCashIssueVoucher.[DepartmentId],[CashBoxId],[ShareholderId],[TechnicianId],[CostCenterId],[PosId],[CashierUserId],[ShiftId],[IsCollected],[IsClosed] from {onlineTable}CashIssueVoucher onlineCashIssueVoucher inner join {onlineTable}DBChange onlineDBChange on onlineDBChange.SelectedId = onlineCashIssueVoucher.id AND onlineDBChange.DepartmentId = onlineCashIssueVoucher.DepartmentId where onlineCashIssueVoucher.Id in (select SelectedId from {onlineTable}DBChange where TableName = 'CashIssueVoucher') and onlineDBChange.TableName = 'CashIssueVoucher' and onlineDBChange.IsNew=1 SET IDENTITY_INSERT dbo.CashIssueVoucher OFF";


            //CashIssueVoucher Update
            var cashIssueVoucherUpdate = $" update LocCashIssueVoucher SET LocCashIssueVoucher.[DocumentNumber] = OnCashIssueVoucher.[DocumentNumber],LocCashIssueVoucher.[BranchId] = OnCashIssueVoucher.[BranchId],LocCashIssueVoucher.[MoneyAmount] = OnCashIssueVoucher.[MoneyAmount],LocCashIssueVoucher.[SourceTypeId] = OnCashIssueVoucher.[SourceTypeId],LocCashIssueVoucher.[DirectExpensesId] = OnCashIssueVoucher.[DirectExpensesId],LocCashIssueVoucher.[OtherSourceName] = OnCashIssueVoucher.[OtherSourceName],LocCashIssueVoucher.[Date] = OnCashIssueVoucher.[Date],LocCashIssueVoucher.[CurrencyId] = OnCashIssueVoucher.[CurrencyId],LocCashIssueVoucher.[AccountId] = OnCashIssueVoucher.[AccountId],LocCashIssueVoucher.[IsLinked] = OnCashIssueVoucher.[IsLinked],LocCashIssueVoucher.[IsPosted] = OnCashIssueVoucher.[IsPosted],LocCashIssueVoucher.[IsActive] = OnCashIssueVoucher.[IsActive],LocCashIssueVoucher.[IsDeleted] = OnCashIssueVoucher.[IsDeleted],LocCashIssueVoucher.[UserId] = OnCashIssueVoucher.[UserId],LocCashIssueVoucher.[Notes] = OnCashIssueVoucher.[Notes],LocCashIssueVoucher.[Image] = OnCashIssueVoucher.[Image],LocCashIssueVoucher.[CustomerId] = OnCashIssueVoucher.[CustomerId],LocCashIssueVoucher.[VendorId] = OnCashIssueVoucher.[VendorId],LocCashIssueVoucher.[EmployeeId] = OnCashIssueVoucher.[EmployeeId],LocCashIssueVoucher.[CurrencyEquivalent] = OnCashIssueVoucher.[CurrencyEquivalent],LocCashIssueVoucher.[DepartmentId] = OnCashIssueVoucher.[DepartmentId],LocCashIssueVoucher.[CashBoxId] = OnCashIssueVoucher.[CashBoxId],LocCashIssueVoucher.[ShareholderId] = OnCashIssueVoucher.[ShareholderId],LocCashIssueVoucher.[TechnicianId] = OnCashIssueVoucher.[TechnicianId],LocCashIssueVoucher.[CostCenterId] = OnCashIssueVoucher.[CostCenterId],LocCashIssueVoucher.[PosId] = OnCashIssueVoucher.[PosId],LocCashIssueVoucher.[CashierUserId] = OnCashIssueVoucher.[CashierUserId],LocCashIssueVoucher.[ShiftId] = OnCashIssueVoucher.[ShiftId],LocCashIssueVoucher.[IsCollected] = OnCashIssueVoucher.[IsCollected],LocCashIssueVoucher.[IsClosed] = OnCashIssueVoucher.[IsClosed] FROM CashIssueVoucher AS LocCashIssueVoucher INNER JOIN {onlineTable}CashIssueVoucher AS OnCashIssueVoucher ON LocCashIssueVoucher.Id = OnCashIssueVoucher.Id inner join {onlineTable}DBChange onlineDBChange on onlineDBChange.SelectedId = OnCashIssueVoucher.id AND onlineDBChange.DepartmentId = OnCashIssueVoucher.DepartmentId  where OnCashIssueVoucher.Id in (select SelectedId from {onlineTable}DBChange where TableName = 'CashIssueVoucher' and IsNew = 0) and TableName = 'CashIssueVoucher'";



            // Department Insert
            var departmentInsert = $" SET IDENTITY_INSERT dbo.Department ON INSERT INTO dbo.Department ([Id],[ArName],[EnName],[Code],[IsDeleted],[IsActive],[Notes],[SalesAccountId],[InventoryAccountId],[CostOfSalesAccountId],[PurchaseAccountId],[BankAccountId],[DiscountAllowedAccountId],[AcquiredDeductionAccountId],[IsLinked],[IsPosted],[UserId],[Image],[GeneralExpensesAccountId],[TaxAccountId],[IdOnWebSite],[FixedAssetsAccountId],[CurrentAssetsAccountId],[ShareHolderAccountId],[CurrentShareHolderAccountId],[FixedAssetsDepreciationExpensesAccountId],[IdOnWebsite2],[CustomersAccountId],[VendorsAccountId],[ExpensesAccountId],[RevenueAccountId],[CommercialRevenueTaxAccountId],[InstallmentRevenueAccountId],[VisaAccountId],[VisaCommissionAccountId],[DirectExpensesAccountId],[AccruedSalariesAccountId],[PhoneNo],[EmpVacationAccumulatedAccountId],[TravelingTicketsAccumulatedAccountId],[EndOfServiceAccumulatedAccountId],[PaidAllowancesAccumulatedAccountId],[EmpVacationExpensesAccountId],[TravelingTicketsExpensesAccountId],[EndOfServiceExpensesAccountId],[PaidAllowancesExpensesAccountId],[SalesReturnsAccountId],[ReservationPaymentAccountId],[DepartmentCurrentAccountId],[DepartmentsInventoryTransferAccountId],[EmployeeReceivableAccountId],[IdOnWebsite3],[DueSalariesAccountId],[RefundableInsuranceAccountId]) select onlineDepartment.[Id],[ArName],[EnName],[Code],[IsDeleted],[IsActive],[Notes],[SalesAccountId],[InventoryAccountId],[CostOfSalesAccountId],[PurchaseAccountId],[BankAccountId],[DiscountAllowedAccountId],[AcquiredDeductionAccountId],[IsLinked],[IsPosted],[UserId],[Image],[GeneralExpensesAccountId],[TaxAccountId],[IdOnWebSite],[FixedAssetsAccountId],[CurrentAssetsAccountId],[ShareHolderAccountId],[CurrentShareHolderAccountId],[FixedAssetsDepreciationExpensesAccountId],[IdOnWebsite2],[CustomersAccountId],[VendorsAccountId],[ExpensesAccountId],[RevenueAccountId],[CommercialRevenueTaxAccountId],[InstallmentRevenueAccountId],[VisaAccountId],[VisaCommissionAccountId],[DirectExpensesAccountId],[AccruedSalariesAccountId],[PhoneNo],[EmpVacationAccumulatedAccountId],[TravelingTicketsAccumulatedAccountId],[EndOfServiceAccumulatedAccountId],[PaidAllowancesAccumulatedAccountId],[EmpVacationExpensesAccountId],[TravelingTicketsExpensesAccountId],[EndOfServiceExpensesAccountId],[PaidAllowancesExpensesAccountId],[SalesReturnsAccountId],[ReservationPaymentAccountId],[DepartmentCurrentAccountId],[DepartmentsInventoryTransferAccountId],[EmployeeReceivableAccountId],[IdOnWebsite3],[DueSalariesAccountId],[RefundableInsuranceAccountId] from {onlineTable}Department onlineDepartment inner join {onlineTable}DBChange onlineDBChange on onlineDBChange.SelectedId = onlineDepartment.id  where onlineDepartment.Id in (select SelectedId from {onlineTable}DBChange where TableName = 'Department') and onlineDBChange.TableName = 'Department' and onlineDBChange.IsNew=1 SET IDENTITY_INSERT dbo.Department OFF";


            //Department Update
            var departmentUpdate = $" update LocDepartment SET LocDepartment.[ArName] = OnDepartment.[ArName],LocDepartment.[EnName] = OnDepartment.[EnName],LocDepartment.[Code] = OnDepartment.[Code],LocDepartment.[IsDeleted] = OnDepartment.[IsDeleted],LocDepartment.[IsActive] = OnDepartment.[IsActive],LocDepartment.[Notes] = OnDepartment.[Notes],LocDepartment.[SalesAccountId] = OnDepartment.[SalesAccountId],LocDepartment.[InventoryAccountId] = OnDepartment.[InventoryAccountId],LocDepartment.[CostOfSalesAccountId] = OnDepartment.[CostOfSalesAccountId],LocDepartment.[PurchaseAccountId] = OnDepartment.[PurchaseAccountId],LocDepartment.[BankAccountId] = OnDepartment.[BankAccountId],LocDepartment.[DiscountAllowedAccountId] = OnDepartment.[DiscountAllowedAccountId],LocDepartment.[AcquiredDeductionAccountId] = OnDepartment.[AcquiredDeductionAccountId],LocDepartment.[IsLinked] = OnDepartment.[IsLinked],LocDepartment.[IsPosted] = OnDepartment.[IsPosted],LocDepartment.[UserId] = OnDepartment.[UserId],LocDepartment.[Image] = OnDepartment.[Image],LocDepartment.[GeneralExpensesAccountId] = OnDepartment.[GeneralExpensesAccountId],LocDepartment.[TaxAccountId] = OnDepartment.[TaxAccountId],LocDepartment.[IdOnWebSite] = OnDepartment.[IdOnWebSite],LocDepartment.[FixedAssetsAccountId] = OnDepartment.[FixedAssetsAccountId],LocDepartment.[CurrentAssetsAccountId] = OnDepartment.[CurrentAssetsAccountId],LocDepartment.[ShareHolderAccountId] = OnDepartment.[ShareHolderAccountId],LocDepartment.[CurrentShareHolderAccountId] = OnDepartment.[CurrentShareHolderAccountId],LocDepartment.[FixedAssetsDepreciationExpensesAccountId] = OnDepartment.[FixedAssetsDepreciationExpensesAccountId],LocDepartment.[IdOnWebsite2] = OnDepartment.[IdOnWebsite2],LocDepartment.[CustomersAccountId] = OnDepartment.[CustomersAccountId],LocDepartment.[VendorsAccountId] = OnDepartment.[VendorsAccountId],LocDepartment.[ExpensesAccountId] = OnDepartment.[ExpensesAccountId],LocDepartment.[RevenueAccountId] = OnDepartment.[RevenueAccountId],LocDepartment.[CommercialRevenueTaxAccountId] = OnDepartment.[CommercialRevenueTaxAccountId],LocDepartment.[InstallmentRevenueAccountId] = OnDepartment.[InstallmentRevenueAccountId],LocDepartment.[VisaAccountId] = OnDepartment.[VisaAccountId],LocDepartment.[VisaCommissionAccountId] = OnDepartment.[VisaCommissionAccountId],LocDepartment.[DirectExpensesAccountId] = OnDepartment.[DirectExpensesAccountId],LocDepartment.[AccruedSalariesAccountId] = OnDepartment.[AccruedSalariesAccountId],LocDepartment.[PhoneNo] = OnDepartment.[PhoneNo],LocDepartment.[EmpVacationAccumulatedAccountId] = OnDepartment.[EmpVacationAccumulatedAccountId],LocDepartment.[TravelingTicketsAccumulatedAccountId] = OnDepartment.[TravelingTicketsAccumulatedAccountId],LocDepartment.[EndOfServiceAccumulatedAccountId] = OnDepartment.[EndOfServiceAccumulatedAccountId],LocDepartment.[PaidAllowancesAccumulatedAccountId] = OnDepartment.[PaidAllowancesAccumulatedAccountId],LocDepartment.[EmpVacationExpensesAccountId] = OnDepartment.[EmpVacationExpensesAccountId],LocDepartment.[TravelingTicketsExpensesAccountId] = OnDepartment.[TravelingTicketsExpensesAccountId],LocDepartment.[EndOfServiceExpensesAccountId] = OnDepartment.[EndOfServiceExpensesAccountId],LocDepartment.[PaidAllowancesExpensesAccountId] = OnDepartment.[PaidAllowancesExpensesAccountId],LocDepartment.[SalesReturnsAccountId] = OnDepartment.[SalesReturnsAccountId],LocDepartment.[ReservationPaymentAccountId] = OnDepartment.[ReservationPaymentAccountId],LocDepartment.[DepartmentCurrentAccountId] = OnDepartment.[DepartmentCurrentAccountId],LocDepartment.[DepartmentsInventoryTransferAccountId] = OnDepartment.[DepartmentsInventoryTransferAccountId],LocDepartment.[EmployeeReceivableAccountId] = OnDepartment.[EmployeeReceivableAccountId],LocDepartment.[IdOnWebsite3] = OnDepartment.[IdOnWebsite3],LocDepartment.[DueSalariesAccountId] = OnDepartment.[DueSalariesAccountId],LocDepartment.[RefundableInsuranceAccountId] = OnDepartment.[RefundableInsuranceAccountId] FROM Department AS LocDepartment INNER JOIN {onlineTable}Department AS OnDepartment ON LocDepartment.Id = OnDepartment.Id inner join {onlineTable}DBChange on SelectedId = OnDepartment.id  where OnDepartment.Id in (select SelectedId from {onlineTable}DBChange where TableName = 'Department' and IsNew = 0) and TableName = 'Department'";



            //Warehouse Insert
            var warehouseInsert = $" SET IDENTITY_INSERT dbo.Warehouse ON INSERT INTO dbo.Warehouse ([Id],[Code],[ArName],[EnName],[ResponsibleEmpId],[Phone],[Location],[Notes],[IsActive],[IsDeleted],[UserId],[Image],[IsPosted],[IsLinked],[DepartmentId]) select onlineWarehouse.[Id],[Code],[ArName],[EnName],[ResponsibleEmpId],[Phone],[Location],[Notes],[IsActive],[IsDeleted],[UserId],[Image],[IsPosted],[IsLinked],onlineWarehouse.[DepartmentId] from {onlineTable}Warehouse onlineWarehouse inner join {onlineTable}DBChange onlineDBChange on onlineDBChange.SelectedId = onlineWarehouse.id  where onlineWarehouse.Id in (select SelectedId from {onlineTable}DBChange where TableName = 'Warehouse') and onlineDBChange.TableName = 'Warehouse' and onlineDBChange.IsNew=1 SET IDENTITY_INSERT dbo.Warehouse OFF";


            //Warehouse Update
            var warehouseUpdate = $" update LocWarehouse SET LocWarehouse.[Code] = OnWarehouse.[Code],LocWarehouse.[ArName] = OnWarehouse.[ArName],LocWarehouse.[EnName] = OnWarehouse.[EnName],LocWarehouse.[ResponsibleEmpId] = OnWarehouse.[ResponsibleEmpId],LocWarehouse.[Phone] = OnWarehouse.[Phone],LocWarehouse.[Location] = OnWarehouse.[Location],LocWarehouse.[Notes] = OnWarehouse.[Notes],LocWarehouse.[IsActive] = OnWarehouse.[IsActive],LocWarehouse.[IsDeleted] = OnWarehouse.[IsDeleted],LocWarehouse.[UserId] = OnWarehouse.[UserId],LocWarehouse.[Image] = OnWarehouse.[Image],LocWarehouse.[IsPosted] = OnWarehouse.[IsPosted],LocWarehouse.[IsLinked] = OnWarehouse.[IsLinked],LocWarehouse.[DepartmentId] = OnWarehouse.[DepartmentId] FROM Warehouse AS LocWarehouse INNER JOIN {onlineTable}Warehouse AS OnWarehouse ON LocWarehouse.Id = OnWarehouse.Id inner join {onlineTable}DBChange on SelectedId = OnWarehouse.id  where OnWarehouse.Id in (select SelectedId from {onlineTable}DBChange where TableName = 'Warehouse' and IsNew = 0) and TableName = 'Warehouse'";



            //PosCustomer Insert
            var posCustomerInsert = $" SET IDENTITY_INSERT dbo.PosCustomer ON INSERT INTO dbo.PosCustomer ([Id],[ArName],[EnName],[Code],[Mobile1],[Mobile2],[Address],[Notes],[Image],[IsActive],[IsDeleted]) select onlinePosCustomer.[Id],[ArName],[EnName],[Code],[Mobile1],[Mobile2],[Address],[Notes],[Image],[IsActive],[IsDeleted] from {onlineTable}PosCustomer onlinePosCustomer inner join {onlineTable}DBChange onlineDBChange on onlineDBChange.SelectedId = onlinePosCustomer.id  where onlinePosCustomer.Id in (select SelectedId from {onlineTable}DBChange where TableName = 'PosCustomer') and onlineDBChange.TableName = 'PosCustomer' and onlineDBChange.IsNew=1 SET IDENTITY_INSERT dbo.PosCustomer OFF";


            //PosCustomer Update
            var posCustomerUpdate = $" update LocPosCustomer SET LocPosCustomer.[ArName] = OnPosCustomer.[ArName],LocPosCustomer.[EnName] = OnPosCustomer.[EnName],LocPosCustomer.[Code] = OnPosCustomer.[Code],LocPosCustomer.[Mobile1] = OnPosCustomer.[Mobile1],LocPosCustomer.[Mobile2] = OnPosCustomer.[Mobile2],LocPosCustomer.[Address] = OnPosCustomer.[Address],LocPosCustomer.[Notes] = OnPosCustomer.[Notes],LocPosCustomer.[Image] = OnPosCustomer.[Image],LocPosCustomer.[IsActive] = OnPosCustomer.[IsActive],LocPosCustomer.[IsDeleted] = OnPosCustomer.[IsDeleted] FROM PosCustomer AS LocPosCustomer INNER JOIN {onlineTable}PosCustomer AS OnPosCustomer ON LocPosCustomer.Id = OnPosCustomer.Id inner join {onlineTable}DBChange on SelectedId = OnPosCustomer.id  where OnPosCustomer.Id in (select SelectedId from {onlineTable}DBChange where TableName = 'PosCustomer' and IsNew = 0) and TableName = 'PosCustomer'";



            //Employee Insert
            var employeeInsert = $" SET IDENTITY_INSERT dbo.Employee ON INSERT INTO dbo.Employee ([Id],[Code],[ArName],[EnName],[IsActive],[IsDeleted],[Notes],[Image],[UserId],[AccountId],[NationalId],[Birthdate],[HireDate],[DepartmentId],[Email],[JobGradeId],[GenderId],[MaritalStatusId],[ReligionId],[WorkStatusId],[PhoneNumber],[MobileNumber],[EmpCodeOnFingerPrint],[ContractsTypeId],[SalaryProtection],[EmployeeTypeId],[HasOverTime],[NationalityId],[LocationId],[WorkTeamId],[AreaId],[HrDepartmentId],[AdministrativeDepartmentId],[JobId],[DirectManagerId],[AlternativeEmployeeId],[LivingAddress],[NationalJobId],[WorkLicenseId],[DrivingLicenseId],[WorkOwnerId],[WorkOwnerName],[WorkOwnerNum],[WorkOwnerAddress],[IdentityTypeId],[PassportId],[PassportJobId],[PassportReleaseAddressId],[ResidenceReleaseDate],[ResidenceExpiryDate],[NationalExpiryDate],[IndustrialSafetyExpiryDate],[WorkLicenseExpiryDate],[WorkLicenseReleaseDate],[DrivingLicenseExpiryDate],[DrivingLicenseReleaseDate],[PassportReleaseDate],[PassportExpiryDate],[IsChef]) select onlineEmployee.[Id],[Code],[ArName],[EnName],[IsActive],[IsDeleted],[Notes],[Image],[UserId],[AccountId],[NationalId],[Birthdate],[HireDate],[DepartmentId],[Email],[JobGradeId],[GenderId],[MaritalStatusId],[ReligionId],[WorkStatusId],[PhoneNumber],[MobileNumber],[EmpCodeOnFingerPrint],[ContractsTypeId],[SalaryProtection],[EmployeeTypeId],[HasOverTime],[NationalityId],[LocationId],[WorkTeamId],[AreaId],[HrDepartmentId],[AdministrativeDepartmentId],[JobId],[DirectManagerId],[AlternativeEmployeeId],[LivingAddress],[NationalJobId],[WorkLicenseId],[DrivingLicenseId],[WorkOwnerId],[WorkOwnerName],[WorkOwnerNum],[WorkOwnerAddress],[IdentityTypeId],[PassportId],[PassportJobId],[PassportReleaseAddressId],[ResidenceReleaseDate],[ResidenceExpiryDate],[NationalExpiryDate],[IndustrialSafetyExpiryDate],[WorkLicenseExpiryDate],[WorkLicenseReleaseDate],[DrivingLicenseExpiryDate],[DrivingLicenseReleaseDate],[PassportReleaseDate],[PassportExpiryDate],[IsChef] from {onlineTable}Employee onlineEmployee inner join {onlineTable}DBChange onlineDBChange on onlineDBChange.SelectedId = onlineEmployee.id  where onlineEmployee.Id in (select SelectedId from {onlineTable}DBChange where TableName = 'Employee') and onlineDBChange.TableName = 'Employee' and onlineDBChange.IsNew=1 SET IDENTITY_INSERT dbo.Employee OFF";


            //Employee Update
            var employeeUpdate = $" update LocEmployee SET LocEmployee.[Code] = OnEmployee.[Code],LocEmployee.[ArName] = OnEmployee.[ArName],LocEmployee.[EnName] = OnEmployee.[EnName],LocEmployee.[IsActive] = OnEmployee.[IsActive],LocEmployee.[IsDeleted] = OnEmployee.[IsDeleted],LocEmployee.[Notes] = OnEmployee.[Notes],LocEmployee.[Image] = OnEmployee.[Image],LocEmployee.[UserId] = OnEmployee.[UserId],LocEmployee.[AccountId] = OnEmployee.[AccountId],LocEmployee.[NationalId] = OnEmployee.[NationalId],LocEmployee.[Birthdate] = OnEmployee.[Birthdate],LocEmployee.[HireDate] = OnEmployee.[HireDate],LocEmployee.[DepartmentId] = OnEmployee.[DepartmentId],LocEmployee.[Email] = OnEmployee.[Email],LocEmployee.[JobGradeId] = OnEmployee.[JobGradeId],LocEmployee.[GenderId] = OnEmployee.[GenderId],LocEmployee.[MaritalStatusId] = OnEmployee.[MaritalStatusId],LocEmployee.[ReligionId] = OnEmployee.[ReligionId],LocEmployee.[WorkStatusId] = OnEmployee.[WorkStatusId],LocEmployee.[PhoneNumber] = OnEmployee.[PhoneNumber],LocEmployee.[MobileNumber] = OnEmployee.[MobileNumber],LocEmployee.[EmpCodeOnFingerPrint] = OnEmployee.[EmpCodeOnFingerPrint],LocEmployee.[ContractsTypeId] = OnEmployee.[ContractsTypeId],LocEmployee.[SalaryProtection] = OnEmployee.[SalaryProtection],LocEmployee.[EmployeeTypeId] = OnEmployee.[EmployeeTypeId],LocEmployee.[HasOverTime] = OnEmployee.[HasOverTime],LocEmployee.[NationalityId] = OnEmployee.[NationalityId],LocEmployee.[LocationId] = OnEmployee.[LocationId],LocEmployee.[WorkTeamId] = OnEmployee.[WorkTeamId],LocEmployee.[AreaId] = OnEmployee.[AreaId],LocEmployee.[HrDepartmentId] = OnEmployee.[HrDepartmentId],LocEmployee.[AdministrativeDepartmentId] = OnEmployee.[AdministrativeDepartmentId],LocEmployee.[JobId] = OnEmployee.[JobId],LocEmployee.[DirectManagerId] = OnEmployee.[DirectManagerId],LocEmployee.[AlternativeEmployeeId] = OnEmployee.[AlternativeEmployeeId],LocEmployee.[LivingAddress] = OnEmployee.[LivingAddress],LocEmployee.[NationalJobId] = OnEmployee.[NationalJobId],LocEmployee.[WorkLicenseId] = OnEmployee.[WorkLicenseId],LocEmployee.[DrivingLicenseId] = OnEmployee.[DrivingLicenseId],LocEmployee.[WorkOwnerId] = OnEmployee.[WorkOwnerId],LocEmployee.[WorkOwnerName] = OnEmployee.[WorkOwnerName],LocEmployee.[WorkOwnerNum] = OnEmployee.[WorkOwnerNum],LocEmployee.[WorkOwnerAddress] = OnEmployee.[WorkOwnerAddress],LocEmployee.[IdentityTypeId] = OnEmployee.[IdentityTypeId],LocEmployee.[PassportId] = OnEmployee.[PassportId],LocEmployee.[PassportJobId] = OnEmployee.[PassportJobId],LocEmployee.[PassportReleaseAddressId] = OnEmployee.[PassportReleaseAddressId],LocEmployee.[ResidenceReleaseDate] = OnEmployee.[ResidenceReleaseDate],LocEmployee.[ResidenceExpiryDate] = OnEmployee.[ResidenceExpiryDate],LocEmployee.[NationalExpiryDate] = OnEmployee.[NationalExpiryDate],LocEmployee.[IndustrialSafetyExpiryDate] = OnEmployee.[IndustrialSafetyExpiryDate],LocEmployee.[WorkLicenseExpiryDate] = OnEmployee.[WorkLicenseExpiryDate],LocEmployee.[WorkLicenseReleaseDate] = OnEmployee.[WorkLicenseReleaseDate],LocEmployee.[DrivingLicenseExpiryDate] = OnEmployee.[DrivingLicenseExpiryDate],LocEmployee.[DrivingLicenseReleaseDate] = OnEmployee.[DrivingLicenseReleaseDate],LocEmployee.[PassportReleaseDate] = OnEmployee.[PassportReleaseDate],LocEmployee.[PassportExpiryDate] = OnEmployee.[PassportExpiryDate],LocEmployee.[IsChef] = OnEmployee.[IsChef] FROM Employee AS LocEmployee INNER JOIN {onlineTable}Employee AS OnEmployee ON LocEmployee.Id = OnEmployee.Id inner join {onlineTable}DBChange on SelectedId = OnEmployee.id  where OnEmployee.Id in (select SelectedId from {onlineTable}DBChange where TableName = 'Employee' and IsNew = 0) and TableName = 'Employee'";



            //Table Insert
            var tableInsert = $" SET IDENTITY_INSERT dbo.Table ON INSERT INTO dbo.Table ([Id],[Code],[ArName],[EnName],[WaiterId],[HallId],[IsActive],[IsDeleted],[UserId],[Notes],[Image],[IsReserved],[ReservationDateTime]) select onlineTable.[Id],[Code],[ArName],[EnName],[WaiterId],[HallId],[IsActive],[IsDeleted],[UserId],[Notes],[Image],[IsReserved],[ReservationDateTime] from {onlineTable}Table onlineTable inner join {onlineTable}DBChange onlineDBChange on onlineDBChange.SelectedId = onlineTable.id  where onlineTable.Id in (select SelectedId from {onlineTable}DBChange where TableName = 'Table') and onlineDBChange.TableName = 'Table' and onlineDBChange.IsNew=1 SET IDENTITY_INSERT dbo.Table OFF";


            //Table Update
            var tableUpdate = $" update LocTable SET LocTable.[Code] = OnTable.[Code],LocTable.[ArName] = OnTable.[ArName],LocTable.[EnName] = OnTable.[EnName],LocTable.[WaiterId] = OnTable.[WaiterId],LocTable.[HallId] = OnTable.[HallId],LocTable.[IsActive] = OnTable.[IsActive],LocTable.[IsDeleted] = OnTable.[IsDeleted],LocTable.[UserId] = OnTable.[UserId],LocTable.[Notes] = OnTable.[Notes],LocTable.[Image] = OnTable.[Image],LocTable.[IsReserved] = OnTable.[IsReserved],LocTable.[ReservationDateTime] = OnTable.[ReservationDateTime] FROM Table AS LocTable INNER JOIN {onlineTable}Table AS OnTable ON LocTable.Id = OnTable.Id inner join {onlineTable}DBChange on SelectedId = OnTable.id  where OnTable.Id in (select SelectedId from {onlineTable}DBChange where TableName = 'Table' and IsNew = 0) and TableName = 'Table'";



            //Hall Insert
            var hallInsert = $" SET IDENTITY_INSERT dbo.Hall ON INSERT INTO dbo.Hall ([Id],[Code],[ArName],[EnName],[DepartmentId],[IsActive],[IsDeleted],[UserId],[Notes],[Image]) select onlineHall.[Id],[Code],[ArName],[EnName],[DepartmentId],[IsActive],[IsDeleted],[UserId],[Notes],[Image] from {onlineTable}Hall onlineHall inner join {onlineTable}DBChange onlineDBChange on onlineDBChange.SelectedId = onlineHall.id  where onlineHall.Id in (select SelectedId from {onlineTable}DBChange where TableName = 'Hall') and onlineDBChange.TableName = 'Hall' and onlineDBChange.IsNew=1 SET IDENTITY_INSERT dbo.Hall OFF";


            //Hall Update
            var hallUpdate = $" update LocHall SET LocHall.[Code] = OnHall.[Code],LocHall.[ArName] = OnHall.[ArName],LocHall.[EnName] = OnHall.[EnName],LocHall.[DepartmentId] = OnHall.[DepartmentId],LocHall.[IsActive] = OnHall.[IsActive],LocHall.[IsDeleted] = OnHall.[IsDeleted],LocHall.[UserId] = OnHall.[UserId],LocHall.[Notes] = OnHall.[Notes],LocHall.[Image] = OnHall.[Image] FROM Hall AS LocHall INNER JOIN {onlineTable}Hall AS OnHall ON LocHall.Id = OnHall.Id inner join {onlineTable}DBChange on SelectedId = OnHall.id  where OnHall.Id in (select SelectedId from {onlineTable}DBChange where TableName = 'Hall' and IsNew = 0) and TableName = 'Hall'";



            //CashBox Insert
            var cashBoxInsert = $" SET IDENTITY_INSERT dbo.CashBox ON INSERT INTO dbo.CashBox ([Id],[Code],[ArName],[EnName],[AccountId],[IsActive],[IsDeleted],[UserId],[Notes],[Image],[EmpId],[OpeningBalance],[DepartmentId],[TypeId]) select onlineCashBox.[Id],[Code],[ArName],[EnName],[AccountId],[IsActive],[IsDeleted],[UserId],[Notes],[Image],[EmpId],[OpeningBalance],[DepartmentId],[TypeId] from {onlineTable}CashBox onlineCashBox inner join {onlineTable}DBChange onlineDBChange on onlineDBChange.SelectedId = onlineCashBox.id  where onlineCashBox.Id in (select SelectedId from {onlineTable}DBChange where TableName = 'CashBox') and onlineDBChange.TableName = 'CashBox' and onlineDBChange.IsNew=1 SET IDENTITY_INSERT dbo.CashBox OFF";


            //CashBox Update
            var cashBoxUpdate = $" update LocCashBox SET LocCashBox.[Code] = OnCashBox.[Code],LocCashBox.[ArName] = OnCashBox.[ArName],LocCashBox.[EnName] = OnCashBox.[EnName],LocCashBox.[AccountId] = OnCashBox.[AccountId],LocCashBox.[IsActive] = OnCashBox.[IsActive],LocCashBox.[IsDeleted] = OnCashBox.[IsDeleted],LocCashBox.[UserId] = OnCashBox.[UserId],LocCashBox.[Notes] = OnCashBox.[Notes],LocCashBox.[Image] = OnCashBox.[Image],LocCashBox.[EmpId] = OnCashBox.[EmpId],LocCashBox.[OpeningBalance] = OnCashBox.[OpeningBalance],LocCashBox.[DepartmentId] = OnCashBox.[DepartmentId],LocCashBox.[TypeId] = OnCashBox.[TypeId] FROM CashBox AS LocCashBox INNER JOIN {onlineTable}CashBox AS OnCashBox ON LocCashBox.Id = OnCashBox.Id inner join {onlineTable}DBChange on SelectedId = OnCashBox.id  where OnCashBox.Id in (select SelectedId from {onlineTable}DBChange where TableName = 'CashBox' and IsNew = 0) and TableName = 'CashBox'";



            //AdditionalItem Insert
            var additionalItemInsert = $" SET IDENTITY_INSERT dbo.AdditionalItem ON INSERT INTO dbo.AdditionalItem ([Id],[Code],[ArName],[EnName],[IsActive],[IsDeleted],[UserId],[Notes],[Image],[ItemId],[ItemUnitId],[Equivalent],[DoublePrice],[ExtraPrice],[ShortagePrice],[ExtraQuantity],[DoubleQuantity],[ShortageQuantity],[ItemPriceId]) select onlineAdditionalItem.[Id],[Code],[ArName],[EnName],[IsActive],[IsDeleted],[UserId],[Notes],[Image],[ItemId],[ItemUnitId],[Equivalent],[DoublePrice],[ExtraPrice],[ShortagePrice],[ExtraQuantity],[DoubleQuantity],[ShortageQuantity],[ItemPriceId] from {onlineTable}AdditionalItem onlineAdditionalItem inner join {onlineTable}DBChange onlineDBChange on onlineDBChange.SelectedId = onlineAdditionalItem.id  where onlineAdditionalItem.Id in (select SelectedId from {onlineTable}DBChange where TableName = 'AdditionalItem') and onlineDBChange.TableName = 'AdditionalItem' and onlineDBChange.IsNew=1 SET IDENTITY_INSERT dbo.AdditionalItem OFF";


            //AdditionalItem Update
            var additionalItemUpdate = $" update LocAdditionalItem SET LocAdditionalItem.[Code] = OnAdditionalItem.[Code],LocAdditionalItem.[ArName] = OnAdditionalItem.[ArName],LocAdditionalItem.[EnName] = OnAdditionalItem.[EnName],LocAdditionalItem.[IsActive] = OnAdditionalItem.[IsActive],LocAdditionalItem.[IsDeleted] = OnAdditionalItem.[IsDeleted],LocAdditionalItem.[UserId] = OnAdditionalItem.[UserId],LocAdditionalItem.[Notes] = OnAdditionalItem.[Notes],LocAdditionalItem.[Image] = OnAdditionalItem.[Image],LocAdditionalItem.[ItemId] = OnAdditionalItem.[ItemId],LocAdditionalItem.[ItemUnitId] = OnAdditionalItem.[ItemUnitId],LocAdditionalItem.[Equivalent] = OnAdditionalItem.[Equivalent],LocAdditionalItem.[DoublePrice] = OnAdditionalItem.[DoublePrice],LocAdditionalItem.[ExtraPrice] = OnAdditionalItem.[ExtraPrice],LocAdditionalItem.[ShortagePrice] = OnAdditionalItem.[ShortagePrice],LocAdditionalItem.[ExtraQuantity] = OnAdditionalItem.[ExtraQuantity],LocAdditionalItem.[DoubleQuantity] = OnAdditionalItem.[DoubleQuantity],LocAdditionalItem.[ShortageQuantity] = OnAdditionalItem.[ShortageQuantity],LocAdditionalItem.[ItemPriceId] = OnAdditionalItem.[ItemPriceId] FROM AdditionalItem AS LocAdditionalItem INNER JOIN {onlineTable}AdditionalItem AS OnAdditionalItem ON LocAdditionalItem.Id = OnAdditionalItem.Id inner join {onlineTable}DBChange on SelectedId = OnAdditionalItem.id  where OnAdditionalItem.Id in (select SelectedId from {onlineTable}DBChange where TableName = 'AdditionalItem' and IsNew = 0) and TableName = 'AdditionalItem'";

            // ItemCostPrice Insert
            var itemCostPriceInsert = $" Declare @LocItemCostPriceId int SET @LocItemCostPriceId = (SELECT MAX(Id) FROM dbo.ItemCostPrice) SET IDENTITY_INSERT dbo.ItemCostPrice ON INSERT INTO dbo.ItemCostPrice ([Id],[ItemId],[Date],[QtyBefore],[QtyAfter],[CostBefore],[CostAfter],[SystemPageId],[SelectedId],[IsDelivered],[IsAccepted],[IsLinked],[IsCompleted],[IsPosted],[UserId],[IsActive],[IsDeleted],[AutoCreated],[Notes],[DepartmentId]) SELECT * FROM {onlineTable}ItemCostPrice WHERE Id > @LocItemCostPriceId SET IDENTITY_INSERT dbo.ItemCostPrice OFF";



            //DepartmentItem Insert
            var departmentItemInsert = $" delete from DepartmentItem  insert into DepartmentItem ([ItemId],[DepartmentId]) select [ItemId],[DepartmentId] from {onlineTable}DepartmentItem";

            //object Change = db.Database.ExecuteSqlCommand(systemPageInsert + pageActionInsert + erpUserInsert + erpUserUpdate + userWarehouseInsert + userDepartmentInsert + userPosInsert + userCashBoxInsert + rolePrivilegeInsert + userPrivilegeInsert +itemGroupInsert + itemGroupUpdate + itemUnitInsert + itemUnitUpdate + itemPriceInsert + itemPriceUpdate);

            object Change = db.Database.ExecuteSqlCommand(shiftInsert + shiftUpdate + paymentMethodInsert + paymentMethodUpdate + cashIssueVoucherInsert + cashIssueVoucherUpdate + departmentInsert + departmentUpdate + warehouseInsert + warehouseUpdate + posCustomerInsert + posCustomerUpdate + itemCostPriceInsert + departmentItemInsert);
            //return exception and execute all commands inside transaction and commit or rollback
            // item cost price - item department - pos customer (hndef el byanat - men online lel offline  )نضيف جداول 
            return true;
        }

        public static List<DBChange> GetOnlineDBChange(string tablePath)
        {
            var onlineDBChangeTable = tablePath + "DBChange";
            var DBChanges = db.Database.SqlQuery<DBChange>($"select * from {onlineDBChangeTable}").ToList();

            return DBChanges;
        }
        public static string FillsWithZeros(int? noOfDigits, string Code)
        {
            var DocNo = Code.ToString();
            var diff = noOfDigits - Code.ToString().Length;
            var ii = Code.ToString();
            if (diff > 0)
            {
                for (var a = 0; a < diff; a++)
                {
                    DocNo = DocNo.Insert(0, "0");
                    ii = DocNo;
                }
            }
            DocNo = ii;
            return DocNo;
        }

    }



    public class DocumentNumberDate
    {
        public int Id { get; set; }
        public string DocumentNumber { get; set; }
        public DateTime Date { get; set; }
    }


}