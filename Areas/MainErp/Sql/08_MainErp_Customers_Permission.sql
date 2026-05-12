/*
    MainErp FrmCustemers permission/menu registration.
    SQL Server 2012 compatible.
*/

IF NOT EXISTS (SELECT 1 FROM dbo.Screens WHERE ScreenName = N'FrmCustemers')
BEGIN
    INSERT INTO dbo.Screens
    (
        ScreenName,
        ScreenCaption,
        ScreenTitleEng,
        ScreenType,
        ScreenOrder,
        ScreenVisible,
        FlgShow
    )
    VALUES
    (
        N'FrmCustemers',
        N'بيانات العملاء',
        N'Customers Data',
        1,
        6,
        1,
        1
    );
END

SELECT ScreenName, ScreenCaption, ScreenTitleEng, ScreenVisible, FlgShow
FROM dbo.Screens
WHERE ScreenName = N'FrmCustemers';
