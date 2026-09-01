// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileCompanionV3.DataLogic;
using Febris.MobileCompanionV3.Resources;
using Febris.SharedMobileLibrary.Models.EventArguments;
using Febris.ModelLibrary.Models.XApiModels;
using Febris.SharedMobileLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Febris.MobileCompanionV3.BusinessLogic
{
    public class StatementLogic
    {
        private readonly StatementFileContext _fileSystemContext;
        private readonly StatementDBContext _context;
        public StatementLogic()
        {
            _fileSystemContext = new StatementFileContext();
            _context = new StatementDBContext();
        }

        #region Events from Broadcast Reciever 
        internal async Task<RawStatement> Create(RawStatement rawStatement)
        {
            RawStatement output = default;
            try
            {
                output = await _context.Post(rawStatement);
                return output;
            }
            catch (Exception ex)
            {
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = ex.Message;
                throw;
            }
        }

        internal async Task Update(StatementEventArgs e)
        {            
            try
            {
                RawStatement storage = default;
                if (e.ReferenceId != default)
                {
                    storage = await _context.Get(e.ReferenceId);
                }
                else if (e.ReferenceUUID != default)
                {
                    storage = await _context.GetByRef(e.ReferenceUUID);
                }
                else
                {
                    ///Come up with some other way of doing it. 
                    ///
                    Console.WriteLine("There was no reference to find the rawStatement file");
                }

                storage.JsonStatementData = e.RawStatementJson;
                storage.LastUpdateTimeStamp = DateTime.UtcNow.ToString("s");
                storage = await _context.Update(storage);                
            }
            catch (Exception ex)
            {
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = ex.Message;
                throw;
            }
        }

        internal async Task ErrorOccured(StatementEventArgs e)
        {            
            try
            {
                RawStatement output = default;
                if (e.ReferenceId != default)
                {
                    output = await _context.Get(e.ReferenceId);
                }
                else if (e.ReferenceUUID != default)
                {
                    output = await _context.GetByRef(e.ReferenceUUID);
                }
                else
                {
                    ///Come up with some other way of doing it. 
                    ///
                    Console.WriteLine("There was no reference to find the rawStatement file");
                }
                output.LastUpdateTimeStamp = DateTime.UtcNow.ToString("s");
                output.ErrorsOnActivity++;
                output = await _context.Update(output);


            }
            catch (Exception ex)
            {
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = ex.Message;
                throw;
            }
        }
        #endregion

        #region Change Over to Database 

        ///Gather video files
        public async Task<List<string>> GetUnsentNameList()
        {
            List<string> output = new List<string>();
            try
            {
                List<RawStatement> itemList = await _context.Get();
                output = itemList
                    .Where(i => i.Uploaded == false)
                    .Select(i => i.UUID.ToString())
                    .ToList() ?? default;

                // DO NOT SWITCH THIS TO ExternalReferance. The variant that used to sit here
                // commented out was not a harmless alternative: this string becomes PacketName
                // and therefore the file's name on both tiers, so changing the field renames
                // the file and desynchronises every list that shows it. Its twin in
                // GetSentNameList DID project ExternalReferance and that was the defect.
                return output;
            }
            catch (Exception ex)
            {
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = ex.Message;
                throw;
            }
        }

        /// <summary>
        /// The SENT list reported to the Server, and it must project the SAME field as
        /// <see cref="GetUnsentNameList"/>.
        ///
        /// THIS PROJECTED ExternalReferance AND WAS THE ONLY ONE OF FOUR THAT DID. UUID is
        /// what actually travels and what actually lands on disk: the upload loop reads
        /// GetUnsentNameList, puts that string in PacketName (LoopLogic.cs:339 then :372),
        /// and the Server writes the file under PacketName verbatim
        /// (WiFiP2pRequestProcessing.cs:522/531), after which MoveStatementFileToSent keeps
        /// that name forever (FileManager.cs:470-472). Reporting a DIFFERENT identifier once
        /// Uploaded flipped meant a statement silently changed its advertised name at the
        /// moment of upload, so the Server's "Files On Remote System" could never agree with
        /// the Server's own "Files On System", with the Companion's own screen, or with the
        /// bytes on either disk. Measured on hardware: one statement showed as
        /// bbbbbbbb-2222-... in the remote view and de4efd79-... everywhere else.
        ///
        /// NOT A NAMING CHANGE, A REVERSION. Before the Android 11 scoped-storage migration
        /// both lists came from the file system and therefore both returned real file names
        /// (see the commented-out _fileSystemContext pair in the File System setup region
        /// below). The migration to sqlite rewrote all four projections and only this one
        /// picked the wrong field.
        ///
        /// ExternalReferance is NOT lost and is not an alias for this: it is the caller's
        /// ReferenceUUID from the STATEMENT_CREATE broadcast, it is still persisted on the
        /// row, and it is the right key for correlating back to the training module that
        /// produced the statement. It is simply not the file's name.
        /// </summary>
        public async Task<List<string>> GetSentNameList()
        {
            List<string> output = new List<string>();
            try
            {
                List<RawStatement> list = await _context.Get();
                output = list.Where(i => i.Uploaded == true)
                    .Select(i => i.UUID.ToString())
                    .ToList();
                return output;
            }
            catch (Exception ex)
            {
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = ex.Message;
                throw;
            }
        }

        internal async Task<List<string>> GetSentStatementNameIndex()
        {
            List<string> output = new List<string>();
            try
            {
                List<RawStatement> itemList = await _context.Get();
                output = itemList
                    .Where(i => i.Uploaded == true)
                    .Select(i => i.UUID.ToString())
                    .ToList() ?? default;

                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        internal async Task<List<string>> GetUnsentStatementNameIndex()
        {
            List<string> output = new List<string>();
            try
            {
                List<RawStatement> itemList = await _context.Get();
                output = itemList
                    .Where(i => i.Uploaded == false)
                    .Select(i => i.UUID.ToString())
                    .ToList() ?? default;

                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        internal async Task<RawStatement> GetByRef(string input)
        {
            RawStatement output = default;
            try
            {
                output = await _context.GetByRef(Guid.Parse(input));
                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }
        internal async Task<RawStatement> GetByUUID(string input)
        {
            RawStatement output = default;
            try
            {
                output = await _context.Get(Guid.Parse(input));
                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        internal async Task<RawStatement> UploadComplete(RawStatement rawStatement)
        {
            RawStatement output = default;
            try
            {
                rawStatement.Uploaded = true;
                rawStatement.LastUpdateTimeStamp = DateTime.UtcNow.ToString("s");
                output = await _context.Update(rawStatement);
                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        internal async Task<bool> DeleteOldStatements()
        {
            int errorCount = 0;
            try
            {
                List<RawStatement> statementList = await GetUploadedList();
                foreach(var i in statementList)
                {
                    bool deleted = await _context.Delete(i);
                    if (!deleted)
                    {
                        errorCount++;
                    }
                }
                if (errorCount > 0)
                {
                    LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Errors deleting old statement files: " + errorCount.ToString();
                }

                return true;
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        private async Task<List<RawStatement>> GetUploadedList()
        {
            List<RawStatement> output = default;
            try
            {
                List<RawStatement> statementList = await _context.Get();
                output = statementList.Where(i => i.Uploaded == true).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                //throw;
            }
            return output;
        }

        #endregion

        #region File System setup
        ///Gather video files
        //public async Task<List<string>> GetUnsentNameList()
        //{
        //    List<string> output = new List<string>();
        //    try
        //    {
        //        output = await _fileSystemContext.GetUnsentNameList();
        //        return output;
        //    }
        //    catch (Exception ex)
        //    {
        //        LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = ex.Message;
        //        throw;
        //    }
        //}

        //public async Task<List<string>> GetSentNameList()
        //{
        //    List<string> output = new List<string>();
        //    try
        //    {
        //        output = await _fileSystemContext.GetSentNameList();
        //        return output;
        //    }
        //    catch (Exception ex)
        //    {
        //        LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = ex.Message;
        //        throw;
        //    }
        //}

        //internal async Task<List<string>> GetSentStatementNameIndex()
        //{
        //    List<string> output = new List<string>();
        //    try
        //    {
        //        List<RawStatement> itemList = await _fileSystemContext.GetStatementList();
        //        output = itemList
        //            .Where(i => i.Uploaded == true)
        //            .Select(i => i.UUID.ToString())
        //            .ToList() ?? default;

        //        return output;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.StackTrace);
        //        throw;
        //    }
        //}

        //internal async Task<List<string>> GetUnsentStatementNameIndex()
        //{
        //    List<string> output = new List<string>();
        //    try
        //    {
        //        List<RawStatement> itemList = await _fileSystemContext.GetStatementList();
        //        output = itemList
        //            .Where(i => i.Uploaded == false)
        //            .Select(i => i.UUID.ToString())
        //            .ToList() ?? default;

        //        return output;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.StackTrace);
        //        throw;
        //    }
        //}

        #endregion
    }
}
