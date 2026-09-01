// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileCompanionV3.BusinessLogic;
using Febris.SharedMobileLibrary.Models.EventArguments;
using Febris.ModelLibrary.Models.XApiModels;
using Febris.SharedMobileLibrary.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Febris.MobileCompanionV3.Services
{
    public class StatementRecieverProcessing
    {
        public StatementLogic _context;
        public StatementRecieverProcessing()
        {
            _context = new StatementLogic();
        }

        internal async void OnStatementUpdate(object sender, StatementEventArgs e)
        {
            try
            {
                await _context.Update(e);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                // FIX (MDM-B4): rethrow from an async void handler crashes the process (no caller to catch it). Log and swallow. See docs/MODERNIZATION/MDM_MODERNIZATION.md.
                // throw;
            }
        }

        internal async void OnStatementCreate(object sender, StatementEventArgs e)
        {
            try
            {
                RawStatement rawStatement = new RawStatement(e.RawStatementJson, e.ReferenceUUID);
                //Add to database
                rawStatement = await _context.Create(rawStatement);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                // FIX (MDM-B4): rethrow from an async void handler crashes the process (no caller to catch it). Log and swallow. See docs/MODERNIZATION/MDM_MODERNIZATION.md.
                // throw;
            }
        }

        internal async void OnStatementError(object sender, StatementEventArgs e)
        {
            try
            {
                await _context.ErrorOccured(e);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                // FIX (MDM-B4): rethrow from an async void handler crashes the process (no caller to catch it). Log and swallow. See docs/MODERNIZATION/MDM_MODERNIZATION.md.
                // throw;
            }
        }
    }
}
