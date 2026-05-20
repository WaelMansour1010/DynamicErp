/* Generic mapping template. Populate after Discovery; do not assume Adnan names. */
DECLARE @MigrationBatchId uniqueidentifier = '$(MigrationBatchId)';
DECLARE @CustomerCode nvarchar(50) = N'$(CustomerCode)';
SELECT 'Mapping checklist' AS Section, *
FROM (VALUES
(N'Property',N'Source property table/id/code/name -> dbo.Property'),
(N'Unit',N'Source unit table/id/property id/unit no/type -> dbo.PropertyDetail'),
(N'Tenant',N'Source renter/customer id/name/account -> dbo.PropertyRenter'),
(N'Contract',N'Source contract id/dates/renter/unit/amount -> dbo.PropertyContract'),
(N'Installment',N'Source installment id/contract/date/value -> dbo.PropertyContractBatch'),
(N'Account',N'Source account code -> dbo.ChartOfAccount'),
(N'Receipt',N'Source receipt note/voucher -> dbo.CashReceiptVoucher'),
(N'Journal',N'Source journal header/lines -> dbo.JournalEntry/JournalEntryDetail')
) v(EntityType,MappingRequirement);
