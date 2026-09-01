// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Febris.SharedMobileLibrary.Interfaces
{
    public interface ISharedFileSystem
    {
        Task<string> GetBaseDirectoryPath();
        Task<List<string>> GetDirectoryContentNames(string filePath);


        Task<bool> FileExists(string filePath);

        Task<(bool, string)> CreateDirectory(string directoryName, string expectedUri);
        //Task<(bool, string)> CreateFile(string directoryPath);
        Task<string> CreateFile(string directoryPath);

        Task<bool> FileMover(string sourceFilePath, string targetDirectory);

        Task<string> ReadTextFile(string filePath);
        Task<bool> WriteTextFile(string filePath, string content);
        void WriteAllBytes(string zippedPath, byte[] body);
        Task<bool> FileUnzipper(string zipFilePath, string unzipFolderPath, string linkName);
        string GetDirectoryApkFiles(string filePath);
        bool DeleteFile(string zippedPath);
        Task<byte[]> OutgoingFileData(string tempFilePath);

    }
}
