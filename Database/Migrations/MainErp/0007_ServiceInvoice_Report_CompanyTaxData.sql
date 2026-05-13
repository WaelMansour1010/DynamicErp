ALTER PROCEDURE [dbo].[ServiceInvoice_Get]
    @Id int
AS
BEGIN
    SELECT [ServiceInvoice].[Id],
          [DocumentNumber],
          [Department].Id as [DepartmentId],
          [Department].ArName as [Department],
          [Department].PhoneNo as [DepartmentPhoneNo],
          Department.EnName DepartmentEnName,
          Department.Address DepartmentAddress,
          [VoucherDate],
          [Currency].ArName as [Currency],
          [CurrencyEquivalent],
          [Total],
          [SalesTaxes],
          (CAST([TotalAfterTaxes] AS DECIMAL(18,1))) "TotalAfterTaxes",
          [VoucherDiscountValue],
          [VoucherDiscountPercentage],
          [NetTotal],
          [CostCenterId],
          [SystemPage].ArName as [SystemPage],
          [SelectedId],
          [TotalItemDirectExpenses],
          [IsDelivered],
          [IsAccepted],
          [ServiceInvoice].[IsLinked],
          [ServiceInvoice].[IsCompleted],
          [ServiceInvoice].[IsPosted],
          [ServiceInvoice].[UserId],
          [ServiceInvoice].[IsActive],
          [ServiceInvoice].[IsDeleted],
          [AutoCreated],
          [ServiceInvoice].[Notes],
          [ServiceInvoice].[Image],
          (select PaymentMethod.ArName from PaymentMethod where Id = (select top(1) PaymentMethodId From ServiceInvoicePaymentMethod where ServiceInvoiceId = @Id and Amount > 0)) as paymentMethod,
          (select name from ERPUser where Id = (select UserId from ServiceInvoice where Id = @Id)) SalesManName,
          (select name from ERPUser where Id = (select UserId from ServiceInvoice where Id = @Id)) CashierName,
          ci.ArName CustomerArCity,
          Co.ArName CustomerArCountry,
          ci.EnName CustomerEnCity,
          Co.EnName CustomerEnCountry,
          [CustomerId],
          Customer.ArName CustomerArName,
          Customer.EnName CustomerEnName,
          Customer.BuildingNo CustomerBuildingNo,
          Customer.Address CustomerAddress,
          Customer.PostOfficeBox CustomerPostOfficeBox,
          Customer.RegistrationNo CustomerRegistrationNo,
          COALESCE(NULLIF(Customer.VATNumber, ''), NULLIF(Customer.TaxNumber, '')) CustomerTaxNumber,
          Customer.Mobile CustomerMobile,
          Customer.PhoneNo CustomerPhoneNo,
          COALESCE(NULLIF(Company.InsuranceNumber, ''), SystemSetting.CommercialRegistrationNo) CommercialRegistrationNo,
          COALESCE(NULLIF(Company.TaxNumber, ''), SystemSetting.TaxCardNumber) TaxCardNumber,
          COALESCE(NULLIF(Company.ArName, ''), SystemSetting.CompanyArName) CompanyArName,
          COALESCE(NULLIF(Company.EnName, ''), SystemSetting.CompanyEnName) CompanyEnName,
          SystemSetting.StreetArName,
          SystemSetting.StreetEnName,
          (select c.ArName from Country c where c.Id = SystemSetting.CountryId) CountryArName,
          (select c.EnName from Country c where c.Id = SystemSetting.CountryId) CountryEnName,
          (select c.ArName from City c where c.Id = SystemSetting.CityId) CityArName,
          (select c.EnName from City c where c.Id = SystemSetting.CityId) CityEnName
    FROM [dbo].[ServiceInvoice]
    LEFT JOIN Department on Department.Id = [ServiceInvoice].DepartmentId
    LEFT JOIN Company on Company.Id = Department.CompanyId and Company.IsDeleted = 0
    CROSS JOIN (SELECT TOP (1) * FROM SystemSetting) SystemSetting
    LEFT JOIN Currency on Currency.Id = [ServiceInvoice].CurrencyId
    LEFT JOIN SystemPage on SystemPage.Id = [ServiceInvoice].SystemPageId
    LEFT JOIN Customer on Customer.Id = [ServiceInvoice].CustomerId
    LEFT JOIN City ci on Customer.CityId = ci.Id
    LEFT JOIN Country co on Customer.CountryId = co.Id
    WHERE ((@Id is null or ServiceInvoice.Id = @Id)
      and ServiceInvoice.IsActive = 1
      and ServiceInvoice.IsDeleted = 0)
END
