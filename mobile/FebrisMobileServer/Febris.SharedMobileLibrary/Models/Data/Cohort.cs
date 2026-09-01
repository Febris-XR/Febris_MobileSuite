// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Collections.Generic;
using System.Text;

namespace Febris.SharedMobileLibrary.Models.Data
{
    public class Cohort:BaseModel
    {
        public bool Archive { get; set; }
        public bool LockMembers { get; set; }
        public string Name { get; set; }        
        public string Description { get; set; }

        public Guid InstructorId { get; set; }

        //need to link to institution and potentially location
    }

    public class CohortMember : BaseModel
    {
        public Guid UserId { get; set; }

        public Cohort Cohort { get; set; }
        public Guid CohortUUID { get; set; }

    }

   
}
