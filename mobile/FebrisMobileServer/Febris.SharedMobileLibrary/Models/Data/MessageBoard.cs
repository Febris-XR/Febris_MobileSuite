// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Globalization;
using System.Collections.Generic;
using System.Text;

namespace Febris.SharedMobileLibrary.Models.Data
{
    public class AdminMessageBoard : BaseModel
    {
        //public long Id { get; set; }
        //public Guid UUID { get; set; } // lets use this to link? otherwise it is not stated as needed

        public string Subject { get; set; }
        public string Message { get; set; }
        //public DateTime CreationTimeStamp { get; set; }
        //public DateTime UpdateTimeStamp { get; set; }


        public Guid UserId { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }

        public bool FromFebris { get; set; }

        //public Institution Institution { get; set; }
        //public Guid? InstitutionUUID { get; set; }
        ////public Location Location { get; set; }
        ////public Guid LocationUUID { get; set; }
        //public ContentDeveloper ContentDeveloper { get; set; }
        //public Guid? ContentDeveloperUUID { get; set; }
        //public AccreditationBody AccreditationBody { get; set; }
        //public Guid? AccreditationBodyUUID { get; set; }
    }


    public class MessageBoard : BaseModel
    {
        public bool Archive { get; set; }

        public string Subject { get; set; }
        public string Message { get; set; }

        public Guid UserId { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }

        /// <summary>
        /// TimeStamp is a raw ISO-8601 string carrying microseconds, so the board rendered it as
        /// "2026-09-02T17:42:26.252568". Parsed here for display, falling back to the raw value
        /// when it will not parse, so a malformed timestamp still shows something.
        ///
        /// A computed property rather than an IValueConverter because this project defines no
        /// converters anywhere, and one converter would be a pattern with no peer.
        /// </summary>
        public string DisplayTimeStamp
        {
            get
            {
                DateTime parsed;
                if (DateTime.TryParse(TimeStamp, CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out parsed))
                {
                    return parsed.ToString("d MMM yyyy, HH:mm", CultureInfo.CurrentCulture);
                }
                return TimeStamp;
            }
        }

        /// <summary>
        /// I think these should be broken out
        /// </summary>
        ///         
        //public Institution Institution { get; set; }
        //public Guid? InstitutionUUID { get; set; }
        //public Location Location { get; set; }
        //public Guid? LocationUUID { get; set; }
        //public ContentDeveloper ContentDeveloper { get; set; }
        //public Guid? ContentDeveloperUUID { get; set; }
        //public AccreditationBody AccreditationBody { get; set; }
        //public Guid? AccreditationBodyUUID { get; set; }

    }
}
