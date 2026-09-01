// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.ModelLibrary.Models.XApiModels;
using Febris.SharedMobileLibrary.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Febris.MobileCompanionV3.DataLogic
{
    public class StatementDBContext
    {
        LocalDatabase _context = App.DbContext;

        internal async Task<RawStatement> Post(RawStatement storage)
        {
            try
            {
                RawStatement output = default;
                output = await _context.Create<RawStatement>(storage);
                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }            
        }

        internal async Task<RawStatement> Update(RawStatement storage)
        {
            try
            {
                RawStatement output = default;
                output = await _context.Update<RawStatement>(storage);
                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        internal async Task<RawStatement> Get(long referenceId)
        {
            try
            {
                RawStatement output = default;
                //output = await _context.Get<RawStatement>(i => i.ExternalReferance == referenceId);
                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        internal async Task<RawStatement> Get(Guid input)
        {
            try
            {
                RawStatement output = default;
                output = await _context.Get<RawStatement>(i => i.UUID == input);
                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        internal async Task<List<RawStatement>> Get()
        {
            try
            {
                List<RawStatement> output = default;
                output = await _context.GetList<RawStatement>();
                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        internal async Task<RawStatement> GetByRef(Guid reference)
        {
            try
            {
                RawStatement output = default;
                output = await _context.Get<RawStatement>(i => i.ExternalReferance == reference);
                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        internal async Task<bool> Delete(RawStatement input)
        {
            try
            {
                bool output = default;
                output = await _context.Delete(input);
                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }
    }
}
