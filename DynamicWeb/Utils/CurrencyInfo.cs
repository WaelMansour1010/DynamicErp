using MyERP.Models;
using System;
using System.Linq;

namespace MyERP
{
    public class CurrencyInfo
    {
        //public enum Currencies { Syria = 0, UAE , SaudiArabia, Tunisia , Gold , Egy , USD , EUR , CNY};
        private MySoftERPEntity db = new MySoftERPEntity();

        #region Constructors

        //public CurrencyInfo(int _CurrencyID , string _CurrencyCode , string _EnglishCurrencyName , string _EnglishCurrencyPartName , string _Arabic1CurrencyName , string _Arabic1CurrencyPartName , byte _PartPrecision)
        //{
        //    CurrencyID = _CurrencyID;
        //    CurrencyCode = _CurrencyCode;
        //    EnglishCurrencyName = _EnglishCurrencyName;
        //    EnglishCurrencyPartName = _EnglishCurrencyPartName;
        //    Arabic1CurrencyName = _Arabic1CurrencyName;
        //    Arabic1CurrencyPartName = _Arabic1CurrencyPartName;
        //    PartPrecision = _PartPrecision;
        //}
        public CurrencyInfo(Currency currency)
        {
            //if(currency != null)
            //{
            //    CurrencyID = currency.Id;
            //    CurrencyCode = currency.Code;
            //    EnglishCurrencyName = currency.EnName;
            //    EnglishCurrencyPartName = currency.FractionEnName;
            //    Arabic1CurrencyName = currency.ArName;
            //    Arabic1CurrencyPartName = currency.FractionArName;
            //    PartPrecision = 2;
            //}
            //else
            //{
            //    Currency DefaultCurrency = db.Currencies.Where(a => a.IsActive == true && a.IsDeleted == false && a.IsDefault == true).FirstOrDefault();
            //    if (DefaultCurrency != null)
            //    {
            //        CurrencyID = DefaultCurrency.Id;
            //        CurrencyCode = DefaultCurrency.Code;
            //        EnglishCurrencyName = DefaultCurrency.EnName;
            //        EnglishCurrencyPartName = DefaultCurrency.FractionEnName;
            //        Arabic1CurrencyName = DefaultCurrency.ArName;
            //        Arabic1CurrencyPartName = DefaultCurrency.FractionArName;
            //        PartPrecision = 2;
            //    }
            //    else
            //    {
            //        Currency SystemCurrency = db.Currencies.Where(a => a.IsActive == true && a.IsDeleted == false && a.Code == "EGP").FirstOrDefault();
            //        if (SystemCurrency != null)
            //        {
            //            CurrencyID = 0;
            //            CurrencyCode = "SAR";
            //            EnglishCurrencyName = "SAR";
            //            EnglishCurrencyPartName = "halla";
            //            Arabic1CurrencyName = "ريال سعودى";
            //            Arabic1CurrencyPartName = "هلله";
            //            PartPrecision = 2;
            //        }
            //        else
            //        {
            //            CurrencyID = 0;
            //            CurrencyCode = "SAR";
            //            EnglishCurrencyName = "Rial";
            //            EnglishCurrencyPartName = "";
            //            Arabic1CurrencyName = "ريال";
            //            Arabic1CurrencyPartName = "هلله";
            //            PartPrecision = 2;
            //        }
            //    }
            //}


            //case Currencies.Egy:
            //    CurrencyID = 5;
            //    CurrencyCode = "EGP";
            //    IsCurrencyNameFeminine = false;
            //    EnglishCurrencyName = "Egyptian Pound";
            //    EnglishPluralCurrencyName = "Egyption Pounds";
            //    EnglishCurrencyPartName = "Coin";
            //    EnglishPluralCurrencyPartName = "Coins";
            //    Arabic1CurrencyName = "جنيه مصرى";
            //    Arabic2CurrencyName = "جنيهان مصريان";
            //    Arabic310CurrencyName = "جنيهات مصرية";
            //    Arabic1199CurrencyName = "جنيهاً مصرياً";
            //    Arabic1CurrencyPartName = "قرش";
            //    Arabic2CurrencyPartName = "قرشان";
            //    Arabic310CurrencyPartName = "قروش";
            //    Arabic1199CurrencyPartName = "قرشاً";
            //    PartPrecision = 6;
            //    IsCurrencyPartNameFeminine = false;
            //    break;

            CurrencyID = 2;
            CurrencyCode = "SAR";
         //   IsCurrencyNameFeminine = false;
            EnglishCurrencyName = "Saudi Riyal";
         //   EnglishPluralCurrencyName = "Saudi Riyals";
            EnglishCurrencyPartName = "Halala";
        //    EnglishPluralCurrencyPartName = "Halalas";
            Arabic1CurrencyName = "ريال سعودي";
            //Arabic2CurrencyName = "ريالان سعوديان";
            //Arabic310CurrencyName = "ريالات سعودية";
            //Arabic1199CurrencyName = "ريالاً سعودياً";
            Arabic1CurrencyPartName = "هللة";
            //Arabic2CurrencyPartName = "هللتان";
            //Arabic310CurrencyPartName = "هللات";
            //Arabic1199CurrencyPartName = "هللة";
            PartPrecision = 2;
          //  IsCurrencyPartNameFeminine = true;
        }

        #endregion

            #region Properties

            /// <summary>
            /// Currency ID
            /// </summary>
        public int CurrencyID { get; set; }

        /// <summary>
        /// Standard Code
        /// Syrian Pound: SYP
        /// UAE Dirham: AED
        /// </summary>
        public string CurrencyCode { get; set; }

        /// <summary>
        /// Is the currency name feminine ( Mua'anath مؤنث)
        /// ليرة سورية : مؤنث = true
        /// درهم : مذكر = false
        /// </summary>
        //public Boolean IsCurrencyNameFeminine { get; set; }

        /// <summary>
        /// English Currency Name for single use
        /// Syrian Pound
        /// UAE Dirham
        /// </summary>
        public string EnglishCurrencyName { get; set; }

        /// <summary>
        /// English Plural Currency Name for Numbers over 1
        /// Syrian Pounds
        /// UAE Dirhams
        /// </summary>
        //public string EnglishPluralCurrencyName { get; set; }

        /// <summary>
        /// Arabic Currency Name for 1 unit only
        /// ليرة سورية
        /// درهم إماراتي
        /// </summary>
        public string Arabic1CurrencyName { get; set; }

        /// <summary>
        /// Arabic Currency Name for 2 units only
        /// ليرتان سوريتان
        /// درهمان إماراتيان
        /// </summary>
        //public string Arabic2CurrencyName { get; set; }

        /// <summary>
        /// Arabic Currency Name for 3 to 10 units
        /// خمس ليرات سورية
        /// خمسة دراهم إماراتية
        /// </summary>
        //public string Arabic310CurrencyName { get; set; }

        /// <summary>
        /// Arabic Currency Name for 11 to 99 units
        /// خمس و سبعون ليرةً سوريةً
        /// خمسة و سبعون درهماً إماراتياً
        /// </summary>
        //public string Arabic1199CurrencyName { get; set; }

        /// <summary>
        /// Decimal Part Precision
        /// for Syrian Pounds: 2 ( 1 SP = 100 parts)
        /// for Tunisian Dinars: 3 ( 1 TND = 1000 parts)
        /// </summary>
        public Byte PartPrecision { get; set; }

        /// <summary>
        /// Is the currency part name feminine ( Mua'anath مؤنث)
        /// هللة : مؤنث = true
        /// قرش : مذكر = false
        /// </summary>
        //public Boolean IsCurrencyPartNameFeminine { get; set; }

        /// <summary>
        /// English Currency Part Name for single use
        /// Piaster
        /// Fils
        /// </summary>
        public string EnglishCurrencyPartName { get; set; }

        /// <summary>
        /// English Currency Part Name for Plural
        /// Piasters
        /// Fils
        /// </summary>
        //public string EnglishPluralCurrencyPartName { get; set; }

        /// <summary>
        /// Arabic Currency Part Name for 1 unit only
        /// قرش
        /// هللة
        /// </summary>
        public string Arabic1CurrencyPartName { get; set; }

        /// <summary>
        /// Arabic Currency Part Name for 2 unit only
        /// قرشان
        /// هللتان
        /// </summary>
        //public string Arabic2CurrencyPartName { get; set; }

        /// <summary>
        /// Arabic Currency Part Name for 3 to 10 units
        /// قروش
        /// هللات
        /// </summary>
        //public string Arabic310CurrencyPartName { get; set; }

        /// <summary>
        /// Arabic Currency Part Name for 11 to 99 units
        /// قرشاً
        /// هللةً
        /// </summary>
        //public string Arabic1199CurrencyPartName { get; set; }
        #endregion
    }
}
