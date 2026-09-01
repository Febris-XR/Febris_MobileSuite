// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;

namespace Febris.SharedMobileLibrary.Models
{
    /// <summary>
    /// Mobile-only flat persistence wrapper for an xAPI statement (the sqlite-net entity form:
    /// nested POCOs cannot be sqlite-net columns, so the statement is stored as its raw JSON here).
    /// Relocated out of the xAPI POCO set when mobile converged onto the shared
    /// Febris.XApi.Models contract package -- RawStatement is not part of the xAPI spec shapes,
    /// so it stays mobile-owned beside <see cref="BaseModel"/> rather than in the contract.
    /// </summary>
    public class RawStatement : BaseModel
    {
        public RawStatement()
        {
            UUID = Guid.NewGuid();
            TimeStamp = DateTime.UtcNow.ToString("s");
            LastUpdateTimeStamp = DateTime.UtcNow.ToString("s");
            Uploaded = false;
            JsonStatementData = string.Empty;
            ErrorsOnActivity = 0;
        }

        public RawStatement(string rawStatementJson, Guid reference)
        {
            UUID = Guid.NewGuid();
            TimeStamp = DateTime.UtcNow.ToString("s");
            LastUpdateTimeStamp = DateTime.UtcNow.ToString("s");
            Uploaded = false;
            JsonStatementData = rawStatementJson;
            ErrorsOnActivity = 0;
            ExternalReferance = reference;
        }

        public string JsonStatementData { get; set; }
        public bool Uploaded { get; set; }
        public string FilePath { get; set; }
        public int ErrorsOnActivity { get; set; }
        public Guid ExternalReferance { get; set; }
    }
}
