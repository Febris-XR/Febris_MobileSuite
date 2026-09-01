// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace Febris.SharedMobileLibrary.Models
{
    public class BaseModel
    {
        //public BaseModel()
        //{
        //    //UUID = new Guid();
        //    //TimeStamp = DateTime.UtcNow.ToString("s");
        //    //LastUpdateTimeStamp = DateTime.UtcNow.ToString("s");
        //    //TimeStamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffffffzzz");//, CultureInfo.InvariantCulture);
        //    //LastUpdateTimeStamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffffffzzz");//, CultureInfo.InvariantCulture);
        //}

        [PrimaryKey,AutoIncrement]
        public long Id { get; set; }
        public Guid UUID { get; set; }

        //[Indexed]
        public string TimeStamp { get; set; }
        //[Indexed]
        public string LastUpdateTimeStamp { get; set; }
    }
}
