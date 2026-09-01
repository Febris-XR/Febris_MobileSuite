// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileCompanionV3.DataLogic;
using Febris.MobileCompanionV3.Resources;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Febris.MobileCompanionV3.BusinessLogic
{
    public class VideoLogic
    {
        private readonly VideoFileContext _context;
        public VideoLogic()
        {
            _context = new VideoFileContext();
        }

        ///Gather video files
        public async Task<List<string>> GetUnsentNameList()
        {
            List<string> output = new List<string>();
            try
            {
                output = await _context.GetUnsentNameList();
                return output;
            }
            catch (Exception ex)
            {
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = ex.Message;
                throw;
            }
        }

        public async Task<List<string>> GetSentNameList()
        {
            List<string> output = new List<string>();
            try
            {
                output = await _context.GetSentNameList();
                return output;
            }
            catch (Exception ex)
            {
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = ex.Message;
                throw;
            }
        }

        internal async Task<List<string>> GetUploadedMediaDirectoryIndex()
        {
            List<string> output = new List<string>();
            try
            {
                output = await _context.GetSentNameList();
                return output;
            }
            catch (Exception ex)
            {
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = ex.Message;
                throw;
            }
        }

        internal async Task<List<string>> GetUnuploadedMediaDirectoryIndex()
        {
            List<string> output = new List<string>();
            try
            {
                output = await _context.GetUnsentNameList();
                return output;
            }
            catch (Exception ex)
            {
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = ex.Message;
                throw;
            }
        }

        internal async Task<bool> DeleteOldVideos()
        {
            bool output = false;
            try
            {
                output = await _context.DeleteOldList();
                return output;
            }
            catch (Exception ex)
            {
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = ex.Message;
                throw;
            }
            //throw new NotImplementedException();
        }

        //internal async Task<List<string>> GetUploadedMediaDirectoryIndex()
        //{
        //    List<string> output = new List<string>();
        //    try
        //    {
        //        List<MediaModel> itemList = await _logicContext.GetMediaList();
        //        output = itemList.Where(i => i.Uploaded == true).Select(i => i.Name).ToList();
        //        return output;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.StackTrace);
        //        throw;
        //    }
        //}

        //internal async Task<List<string>> GetUnuploadedMediaDirectoryIndex()
        //{
        //    List<string> output = new List<string>();
        //    try
        //    {
        //        List<MediaModel> itemList = await _logicContext.GetMediaList();
        //        output = itemList.Where(i => i.Uploaded == false).Select(i => i.Name).ToList();
        //        return output;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.StackTrace);
        //        throw;
        //    }
        //}
    }
}
