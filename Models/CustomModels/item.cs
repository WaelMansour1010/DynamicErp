using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;

namespace MyERP.Models
{
    public partial class Item
    {
        public List<string> ItemURLImages { get; set; }

        public String CurrentLangName
        {
            get
            {

                switch (Thread.CurrentThread.CurrentCulture.Name.ToLower())
                {
                    case "ar":
                        return this.ArName;

                    case "en":
                        return this.EnName;

                    case "kr":
                        return this.KrName;

                    default:
                        return this.EnName;
                }
            }
        }
    }
}