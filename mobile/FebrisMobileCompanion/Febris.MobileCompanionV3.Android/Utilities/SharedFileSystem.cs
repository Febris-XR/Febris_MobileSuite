// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.App;
using Android.Content;
using Android.Database;
using Android.OS;
using Android.Net;
using Android.OS;
using Android.Provider;
using Java.IO;
using Java.Lang;
using Android.Runtime;
using Android.Views;
using AndroidX.Core.Content;
using Febris.SharedMobileLibrary.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AndroidX.DocumentFile.Provider;
using Java.Util.Zip;
using Android.OS.Storage;
using Android.Database.Sqlite;

[assembly: Xamarin.Forms.Dependency(typeof(Febris.MobileCompanionV3.Droid.Utilities.SharedFileSystem))]
namespace Febris.MobileCompanionV3.Droid.Utilities
{
    /// <summary>
    ///    Content Provider Approach:
    ///      A content provider is suitable when you want to share data(files) between different applications using a common interface.
    ///      It's particularly useful for structured data access and offers a way to control access to your app's data.
    ///    
    ///    Intent and Storage Access Framework Approach:
    ///      If you're working with public shared storage and intend to allow users to interact with files using standard Android file pickers and document providers, using intents and the storage access framework is a user-friendly approach.
    ///      This approach is often used when the user's interaction with files is a primary concern, such as when letting them choose files or directories.
    ///      
    /// 
    ///     If your use case primarily involves file interaction by users 
    ///     (e.g., selecting, creating, and deleting files), 
    ///     using the storage access framework with intents might be more appropriate. 
    ///     
    ///     If your focus is on providing a common data interface for multiple applications 
    ///     to access shared files, a content provider could be a suitable choice.
    ///     
    /// 
    ///     DCIM storage?
    /// </summary>
    public class SharedFileSystem : ISharedFileSystem
    {
        public static string _publicFileName = "com.febris.public_files";
        public static string _sharedFiles = "public_files";
        public static string _providerName = "com.febris.provider";
        public static string _baseUriString = string.Empty;
        private readonly ContentResolver _contentResolver;
        private readonly Context _context;
        public SharedFileSystem()
        {
            _context = Android.App.Application.Context;
            //_baseUriString = $"content://{_providerName}";
            _baseUriString = $"file://{_providerName}";
        }

        public async Task<string> GetBaseDirectoryPath()
        {
            try
            {
                var SDK = Build.VERSION.SdkInt;

                string output = string.Empty;
                string baseDirectory = string.Empty;
                string publicFileName = string.Empty;
                //string basePath = $"content://{_providerName}";
                string basePath = $"file://{_providerName}";
                bool worked = false;



                //basePath = $"content://{_providerName}";


                #region obsolete due to Android 11 stupidity (trying to add file provider)
                if (SDK <= BuildVersionCodes.Q)
                {
                    baseDirectory = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;
                    publicFileName = _publicFileName;
                    basePath = System.IO.Path.Combine(baseDirectory, publicFileName);
                }
                else if (SDK > BuildVersionCodes.Q)
                {
                    #region SAF?

                    //// Assuming 'activity' is the current Android activity
                    //// Assuming 'REQUEST_CODE' is your request code constant

                    //// Create an intent for opening the document tree
                    //Intent intent = new Intent(Intent.ActionOpenDocumentTree);

                    //// Set flags to request access to shared documents
                    //intent.AddFlags(ActivityFlags.GrantPersistableUriPermission | ActivityFlags.GrantPrefixUriPermission);

                    //// Specify the initial directory to be selected by the user
                    //string initialDirectory = "document:downloads"; // Adjust as needed
                    //intent.PutExtra(Android.Provider.DocumentsContract.ExtraInitialUri, Android.Net.Uri.Parse(initialDirectory));

                    //// Start the activity for result
                    //((Activity)_context).StartActivityForResult(intent, 8675309);

                    #endregion

                    #region documentpicker?
                    //var documentPicker = ((Activity)_context).RememberForActivityResult(OpenDocument()){Uri ->
                    //        if (uri == null) return;
                    //    _context.ContentResolver.OpenInputStream(uri).use{

                    //    }

                    //}
                    #endregion


                    //baseDirectory = Android.OS.Environment.DirectoryDocuments;
                    //    baseDirectory = Android.OS.Environment.StorageDirectory.AbsolutePath;//.DirectoryDocuments;
                    //    publicFileName = _publicFileName;
                    //    basePath = System.IO.Path.Combine(baseDirectory, publicFileName);
                    //}
                    //else
                    //{
                    // Use FileProvider to get a content URI for the directory

                    #region this should load the document tree. no idea if useful
                    //Android.OS.Storage.StorageManager sm = (Android.OS.Storage.StorageManager)_context.GetSystemService(Context.StorageService);
                    //Android.OS.Storage.StorageVolume primaryVolume = sm.PrimaryStorageVolume;
                    ////Intent intent = sm.getPrimaryStorageVolume().createOpenDocumentTreeIntent();
                    //string startDir = "Documents";

                    //Intent intent = primaryVolume.CreateOpenDocumentTreeIntent();                    
                    //Android.Net.Uri uri = (Android.Net.Uri)intent.GetParcelableExtra("android.provider.extra.INITIAL_URI");
                    //string scheme = uri.ToString();

                    //scheme = scheme.Replace("/root/", "/document/");
                    //scheme += "%3A" + startDir;
                    //uri = Uri.Parse(scheme);
                    //intent.PutExtra("android.provider.extra.INITIAL_URI", uri);

                    //((Activity)_context).StartActivityForResult(intent, _context.REQUEST_ACTION_OPEN_DOCUMENT_TREE);
                    #endregion

                    #region this is a different way of using the document file 
                    //// Assuming 'application' is the current Android application instance
                    //// Assuming 'REQUEST_CODE' is your request code constant

                    //StorageManager storageManager = _context.ApplicationContext.GetSystemService(Context.StorageService) as StorageManager;

                    //// Get the primary storage volume
                    //StorageVolume primaryVolume = storageManager.PrimaryStorageVolume;

                    //// Create an intent for opening the document tree
                    //Intent intent = primaryVolume.CreateOpenDocumentTreeIntent();

                    //// Specify the target directory to be selected by the user
                    //string targetDirectory = "Febris/"; // Adjust the directory as needed

                    //// Retrieve the INITIAL_URI extra from the intent
                    //Android.Net.Uri uri = (Android.Net.Uri)intent.GetParcelableExtra("android.provider.extra.INITIAL_URI");

                    //// Adjust the URI scheme and append the target directory
                    //string scheme = uri.ToString();
                    //scheme = scheme.Replace("/root/", "/document/");
                    //scheme += $":{Uri.Encode(targetDirectory)}";
                    //uri = Android.Net.Uri.Parse(scheme);

                    //// Update the INITIAL_URI extra in the intent
                    //intent.PutExtra("android.provider.extra.INITIAL_URI", uri);

                    //// Start the activity for result
                    //((Activity)_context).StartActivityForResult(intent, 8675309);
                    #endregion

                    //var docID = "primary:Android/media/";

                    //var androidUri = DocumentsContract.BuildDocumentUri(ExternalFileProvider.AUTHORITY, docID);
                    //basePath = androidUri.ToString();


                    //string directoryPath = "Android/media/Documents";
                    //Android.Net.Uri baseUri = MediaStore.Files.GetContentUri("external");
                    //Android.Net.Uri directoryUri = Android.Net.Uri.WithAppendedPath(baseUri, directoryPath);
                    //basePath = directoryUri.ToString();


                    //var androidUri = DocumentsContract.BuildDocumentUri(ExternalFileProvider.AUTHORITY, "primary:Android/media/");
                    //var androidTreeUri = DocumentsContract.BuildTreeDocumentUri(ExternalFileProvider.AUTHORITY, "primary:Android/media/");

                    //File directory = new File(_sharedFiles);
                    //Android.Net.Uri contentUri = FileProvider.GetUriForFile(_context, _providerName, directory);
                    //basePath = contentUri.ToString();

                    //baseDirectory = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDocuments);
                    //publicFileName = _publicFileName;
                    //basePath = System.IO.Path.Combine(baseDirectory, publicFileName);
                    //basePath =ExternalFileProvider.GetPath(basePath);

                }


                (worked, output) = await CreateDirectory(string.Empty, basePath);


                #endregion
                #region post- Android 11 (content Provider)
                ////string output = string.Empty;                
                ////string baseDirectory = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;
                ////string publicFileName = _publicFileName;
                ////

                /////This is doubling the same input so I should use
                //bool worked = false;                
                //(worked, output) = await CreateDirectory(_sharedFiles, basePath);


                //#region moved
                /////This creates a file. I need a folder
                ////Java.IO.File baseFile = new Java.IO.File(baseDirectory, publicFileName);
                ////bool newFileCreated = baseFile.Mkdir();
                ////if (newFileCreated)
                ////{
                ////    System.Console.WriteLine("new file created with path " + baseFile.AbsolutePath);
                ////}
                ////output = baseFile.AbsolutePath;
                //#endregion
                ////}
                ////output = baseDirectory.CreateNewFile(publicFileName);
                #endregion


                return output;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.StackTrace);
                //throw;
                return default;
            }
        }

        public async Task<string> CreateFile(string input)
        {
            try
            {
                var SDK = Build.VERSION.SdkInt;

                string output = string.Empty;
                string baseDirectory = string.Empty;
                string publicFileName = string.Empty;
                string basePath = string.Empty;


                if (SDK < BuildVersionCodes.Q)
                {
                    baseDirectory = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;
                    publicFileName = _publicFileName;
                    basePath = System.IO.Path.Combine(baseDirectory, publicFileName);
                }
                else if (SDK == BuildVersionCodes.Q)
                {
                    baseDirectory = Android.OS.Environment.StorageDirectory.AbsolutePath;//.DirectoryDocuments;
                    publicFileName = _publicFileName;
                    basePath = System.IO.Path.Combine(baseDirectory, publicFileName);
                }
                else if (SDK > BuildVersionCodes.Q)
                {

                    baseDirectory = Android.OS.Environment.StorageDirectory.AbsolutePath;//.DirectoryDocuments;
                    publicFileName = _publicFileName;
                    basePath = System.IO.Path.Combine(baseDirectory, publicFileName);
                }


                Java.IO.File baseFile = new Java.IO.File(basePath, input);
                bool newFileCreated = baseFile.CreateNewFile();
                if (newFileCreated)
                {
                    System.Console.WriteLine("new file created with path " + baseFile.AbsolutePath);
                }
                output = baseFile.AbsolutePath;

                return output;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        //private Uri GetContentUriForFile(string filePath)
        //{
        //    File file = new File(filePath);
        //    return FileProvider.GetUriForFile(_context, _context.PackageName + ".fileprovider", file);
        //}

        /// <summary>
        /// Creating a directory using a content provider involves a two-step process: 
        /// 1) creating a new entry in the provider for the directory 
        /// 2) handling the actual creation of the directory on the storage.
        /// </summary>
        /// <param name="directoryName"></param>
        /// <returns></returns>
        public async Task<(bool, string)> CreateDirectory(string directoryName, string expectedUri)
        {
            try
            {

                string output = string.Empty;

                #region Pre-Android 11                
                //Java.IO.File baseFile = new Java.IO.File(expectedUri, directoryName);
                //bool newFileCreated = baseFile.Mkdir();
                //if (newFileCreated)
                //{
                //    System.Console.WriteLine("new file created with path " + baseFile.AbsolutePath);
                //}
                //output = baseFile.AbsolutePath;

                //return (newFileCreated, output);
                #endregion



                #region post-Android 11 (File Provider)



                // Creating a collection for new files
                ContentValues values = new ContentValues();
                values.Put(MediaStore.IMediaColumns.DisplayName, directoryName);
                values.Put(MediaStore.IMediaColumns.MimeType, DocumentsContract.Document.MimeTypeDir);
                values.Put(MediaStore.IMediaColumns.RelativePath, Environment.DirectoryDocuments);

                // Get the content resolver
                ContentResolver contentResolver = _context.ContentResolver;

                // Insert into MediaStore to create the directory
                Android.Net.Uri baseUri = MediaStore.Files.GetContentUri("external");
                Android.Net.Uri directoryUri = contentResolver.Insert(baseUri, values);

                // Now 'directoryUri' is the content URI for the newly created directory


                //Java.IO.File externalFilesDir = _context.GetExternalFilesDir(null);

                //var initialDirectoryPath = Java.Nio.FileNio.Paths.Get(externalFilesDir.AbsolutePath, directoryName);
                //Java.Nio.FileNio.Files.CreateDirectories(initialDirectoryPath);

                //Android.Net.Uri directoryUri = FileProvider.GetUriForFile(_context, ExternalFileProvider.AUTHORITY, externalFilesDir);

                output = directoryUri?.ToString() ?? string.Empty;
                #endregion
                #region This was focused on internal scoped use not external use
                //string baseDirectory = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;
                //string publicFileName = _publicFileName;
                //string basePath = System.IO.Path.Combine(baseDirectory, publicFileName);

                //Java.IO.File baseFile = new Java.IO.File(basePath, input);
                //bool newFileCreated = baseFile.Mkdir();
                //if (newFileCreated)
                //{
                //    System.Console.WriteLine("new file created with path " + baseFile.AbsolutePath);
                //}
                //output = baseFile.AbsolutePath;
                #endregion
                #region post-Android 11 (content provider - modified but didn't work anyway)
                /////Create a value for puts
                ////string combinedUriString = default;
                ////if (expectedUri != default)
                ////{
                ////    combinedUriString = Path.Combine(directoryName, expectedUri);
                ////}
                ////else
                ////{
                ////    combinedUriString = directoryName;
                ////}


                ////Create new entry in the provider
                //ContentValues values = new ContentValues();
                //values.Put(MediaStore.IMediaColumns.DisplayName, directoryName);
                //values.Put(MediaStore.IMediaColumns.MimeType, DocumentsContract.Document.MimeTypeDir);




                ////create the directory in storage
                ////Uri baseUri = Uri.Parse(_baseUriString);
                //Uri baseUri = Uri.Parse(expectedUri);
                //Android.Net.Uri contentUri = FileProvider.GetUriForFile(_context, ExternalFileProvider.AUTHORITY, values);
                //Uri directoryUri = _context.ContentResolver.Insert(baseUri, values);

                ////output = directoryUri.ToString();
                //output = directoryUri?.ToString() ?? string.Empty;

                //#region This was focused on internal scoped use not external use
                ////string baseDirectory = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;
                ////string publicFileName = _publicFileName;
                ////string basePath = System.IO.Path.Combine(baseDirectory, publicFileName);

                ////Java.IO.File baseFile = new Java.IO.File(basePath, input);
                ////bool newFileCreated = baseFile.Mkdir();
                ////if (newFileCreated)
                ////{
                ////    System.Console.WriteLine("new file created with path " + baseFile.AbsolutePath);
                ////}
                ////output = baseFile.AbsolutePath;
                #endregion

                return (directoryUri != null, output);
                //#endregion
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        //public async Task<object> GetPlatformSpecificFile()
        //{
        //    try
        //    {
        //        bool output = false;


        //        return output;
        //    }
        //    catch (Exception ex)
        //    {
        //        System.Console.WriteLine(ex.StackTrace);
        //        throw;
        //    }
        //}


        //private string WriteTextFile2(string fileName, string content)
        //{
        //    ContentValues values = new ContentValues();
        //    values.Put(MediaStore.MediaColumns.DisplayName, fileName);
        //    values.Put(MediaStore.MediaColumns.MimeType, "text/plain");

        //    Uri contentProviderUri = Uri.Parse("content://" + _providerName);
        //    Uri fileUri = _contentResolver.Insert(contentProviderUri, values);

        //    using (System.IO.Stream outputStream = _contentResolver.OpenOutputStream(fileUri))
        //    {
        //        using (System.IO.StreamWriter writer = new System.IO.StreamWriter(outputStream))
        //        {
        //            writer.Write(content);
        //        }
        //    }

        //    return fileUri.ToString();
        //}

        //private string MoveFileToExternalDirectory2(string sourceFilePath, string targetDirectoryName)
        //{
        //    File targetDirectory = new File(Environment.ExternalStorageDirectory, targetDirectoryName);
        //    targetDirectory.Mkdirs();

        //    string fileName = System.IO.Path.GetFileName(sourceFilePath);
        //    string targetFilePath = System.IO.Path.Combine(targetDirectory.AbsolutePath, fileName);

        //    System.IO.File.Move(sourceFilePath, targetFilePath);

        //    return targetFilePath;
        //}

        //private string ReadTextFile2(string fileName)
        //{
        //    // Reference to YourCustomContentProvider
        //    var providerUri = ExternalContentProvider.BuildFileUri(fileName);

        //    using (System.IO.StreamReader reader = new System.IO.StreamReader(_contentResolver.OpenInputStream(providerUri)))
        //    {
        //        return reader.ReadToEnd();
        //    }
        //}

        public bool DeleteFile(string fileName)
        {
            bool deleted = false;

            try
            {
                // Construct the URI for the shared file based on your content provider's authority and the file name
                Android.Net.Uri sharedFileUri = Android.Net.Uri.Parse(fileName);

                // Delete the file using the content resolver
                int rowsDeleted = Application.Context.ContentResolver.Delete(sharedFileUri, null, null);

                // Check if the deletion was successful
                deleted = rowsDeleted > 0;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.Message);
                // Handle the exception as needed
            }

            return deleted;
        }

        public async Task<List<string>> GetDirectoryContentNames(string filePath)
        {
            try
            {
                List<string> output = new List<string>();

                try
                {
                    // Construct the URI for the directory based on the file path
                    Uri directoryUri = Uri.Parse(filePath);

                    // Get a DocumentFile instance for the directory
                    DocumentFile directory = DocumentFile.FromTreeUri(_context, directoryUri);

                    // Check if the DocumentFile represents a directory
                    if (directory != default && directory.IsDirectory)
                    {
                        // Iterate over the files in the directory
                        DocumentFile[] files = directory.ListFiles();
                        if (files != null)
                        {
                            foreach (var file in files)
                            {
                                // Retrieve the display name of each file
                                string displayName = file.Name;//.GetName();

                                // Add the display name to the output list
                                output.Add(displayName);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"Error parsing URI: {ex.Message}");
                    return default;
                }

                return output;
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine(ex.Message);
                return default;
            }
        }

        public async Task<bool> FileExists(string filePath)
        {
            try
            {
                // Construct the URI for the file based on the file path
                Android.Net.Uri fileUri = Android.Net.Uri.Parse(filePath);

                // Query the content provider to check if the file exists
                using (var cursor = Application.Context.ContentResolver.Query(
                    fileUri, null, null, null, null))
                {
                    return cursor != null && cursor.Count > 0;
                }
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine(ex.Message);
                // Handle the exception as needed
                return false;
            }
        }

        public async Task<bool> DirectoryExists(string filePath)
        {
            try
            {
                // Construct the URI for the directory based on the directory path
                Android.Net.Uri directoryUri = Android.Net.Uri.Parse(filePath);

                // Query the content provider to check if there are any subdirectories
                using (var cursor = Application.Context.ContentResolver.Query(
                    directoryUri, null, null, null, null))
                {
                    if (cursor != null)
                    {
                        while (cursor.MoveToNext())
                        {
                            // Check if the MIME type indicates a directory
                            int mimeTypeColumnIndex = cursor.GetColumnIndex(DocumentsContract.Document.ColumnMimeType);
                            string mimeType = cursor.GetString(mimeTypeColumnIndex);

                            if (DocumentsContract.Document.MimeTypeDir.Equals(mimeType, System.StringComparison.OrdinalIgnoreCase))
                            {
                                return true; // At least one subdirectory exists
                            }
                        }

                        cursor.Close();
                    }
                }

                return false; // No subdirectories found
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine(ex.Message);
                // Handle the exception as needed
                return false;
            }
        }

        public Task<(bool, string)> CreateDirectory(string directoryPath)
        {
            throw new System.NotImplementedException();
        }

        public async Task<bool> FileMover(string sourceFilePath, string targetDirectory)
        {
            try
            {
                // Construct the URIs for the current and new paths based on your content provider's authority
                Android.Net.Uri currentPathUri = Android.Net.Uri.Parse(sourceFilePath);
                Android.Net.Uri newPathUri = Android.Net.Uri.Parse(targetDirectory);

                // Open input stream from the content resolver for the current path
                using (System.IO.Stream inputStream = Application.Context.ContentResolver.OpenInputStream(currentPathUri))
                {
                    // Open output stream to the content resolver for the new path
                    using (System.IO.Stream outputStream = Application.Context.ContentResolver.OpenOutputStream(newPathUri))
                    {
                        // Copy the content from the current path to the new path
                        await inputStream.CopyToAsync(outputStream);
                    }
                }

                // Delete the original folder
                Application.Context.ContentResolver.Delete(currentPathUri, null, null);

                return true;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.Message);
                // Handle the exception as needed
                return false;
            }
        }

        public async Task<string> ReadTextFile(string filePath)
        {
            string output = string.Empty;
            try
            {
                // Construct the URI for the file based on the file path
                Android.Net.Uri fileUri = Android.Net.Uri.Parse(filePath);

                // Open an InputStream from the content resolver for the file
                using (System.IO.Stream inputStream = Application.Context.ContentResolver.OpenInputStream(fileUri))
                {
                    if (inputStream != null)
                    {
                        // Read the content of the file into a byte array
                        using (System.IO.MemoryStream memoryStream = new System.IO.MemoryStream())
                        {
                            await inputStream.CopyToAsync(memoryStream);

                            // Convert the byte array to a string using UTF-8 encoding
                            output = Encoding.UTF8.GetString(memoryStream.ToArray());
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine(ex.Message);
                // Handle the exception as needed
            }
            return output;
        }

        public async Task<bool> WriteTextFile(string filePath, string content)
        {
            try
            {
                // Construct the URI for the file based on the file path
                Android.Net.Uri fileUri = Android.Net.Uri.Parse(filePath);

                // Open an OutputStream to the file specified by the URI
                using (var outputStream = Application.Context.ContentResolver.OpenOutputStream(fileUri))
                {
                    if (outputStream != null)
                    {
                        // Convert the content string to a byte array using UTF-8 encoding
                        byte[] contentBytes = Encoding.UTF8.GetBytes(content);

                        // Write the byte array to the OutputStream
                        await outputStream.WriteAsync(contentBytes, 0, contentBytes.Length);

                        return true; // Writing succeeded
                    }
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.Message);
                // Handle the exception as needed
            }

            return false; // Writing failed
        }

        public void WriteAllBytes(string targetPath, byte[] body)
        {
            try
            {
                // Construct the URI for the file based on the file path
                Android.Net.Uri fileUri = Android.Net.Uri.Parse(targetPath);

                // Open an OutputStream to the file specified by the URI
                using (var outputStream = Application.Context.ContentResolver.OpenOutputStream(fileUri))
                {
                    // Write the byte array to the OutputStream
                    outputStream.Write(body, 0, body.Length);
                }
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine(ex.Message);
                // Handle the exception as needed
            }
        }

        //public async Task<bool> FileUnzipper(string zipFilePath, string unzipFolderPath, string linkName)
        //{
        //    bool unzipped = false;            
        //    try
        //    {
        //        string zipPath = zipFilePath;                
        //        System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, unzipFolderPath);
        //        unzipped = true;
        //    }
        //    catch (Exception ex)
        //    {
        //        System.Console.WriteLine(ex.Message);
        //        return false;
        //    }        
        //    return unzipped;
        //}

        public async Task<bool> FileUnzipper(string zipFilePath, string unzipFolderPath, string linkName)
        {
            bool unzipped = false;
            try
            {
                // Construct the URI for the zip file based on the file path
                Android.Net.Uri zipFileUri = Android.Net.Uri.Parse(zipFilePath);

                // Open an InputStream for the zip file
                using (var inputStream = Application.Context.ContentResolver.OpenInputStream(zipFileUri))
                {
                    if (inputStream != null)
                    {
                        // Create a ZipInputStream from the InputStream
                        using (var zipInputStream = new ZipInputStream(inputStream))
                        {
                            ZipEntry zipEntry;

                            // Loop through each entry in the zip file
                            while ((zipEntry = zipInputStream.NextEntry) != null)
                            {
                                // Construct the target URI for the entry based on the unzip folder path and entry name
                                //Android.Net.Uri targetUri = Android.Net.Uri.Parse($"{unzipFolderPath}/{zipEntry.Name}");
                                Android.Net.Uri targetUri = Android.Net.Uri.Parse(unzipFolderPath);

                                // Open an OutputStream for the target entry
                                using (var outputStream = Application.Context.ContentResolver.OpenOutputStream(targetUri))
                                {
                                    if (outputStream != null)
                                    {
                                        // Use a buffer to read and write the data
                                        byte[] buffer = new byte[4096];
                                        int bytesRead;

                                        // Read from zipInputStream and write to outputStream
                                        while ((bytesRead = zipInputStream.Read(buffer, 0, buffer.Length)) > 0)
                                        {
                                            outputStream.Write(buffer, 0, bytesRead);
                                        }
                                    }
                                }
                            }
                        }

                        unzipped = true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.Message);
                return false;
            }

            return unzipped;
        }

        public string GetDirectoryApkFiles(string directoryPath)
        {
            string output = string.Empty;

            try
            {
                // Construct the URI for the directory based on the directory path
                Android.Net.Uri directoryUri = Android.Net.Uri.Parse(directoryPath);

                // Query the content provider to get APK files
                using (var cursor = Application.Context.ContentResolver.Query(
                    directoryUri, null, null, null, null))
                {
                    if (cursor != null)
                    {
                        while (cursor.MoveToNext())
                        {
                            // Get the file name from the cursor
                            int displayNameColumnIndex = cursor.GetColumnIndex(DocumentsContract.Document.ColumnDisplayName);
                            string fileName = cursor.GetString(displayNameColumnIndex);

                            // Check if the file is an APK
                            if (fileName != null && fileName.EndsWith(".apk", System.StringComparison.OrdinalIgnoreCase))
                            {
                                output = fileName;
                                break;
                            }
                        }

                        cursor.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.Message);
                // Handle the exception as needed
            }

            return output;
        }

        public static Java.IO.File GetExternalStoragePublicDirectoryFile(string pathToModulefolder)
        {
            try
            {
                Android.Net.Uri uri = Android.Net.Uri.Parse(pathToModulefolder);

                // Open the file descriptor from the content resolver
                ParcelFileDescriptor parcelFileDescriptor = Application.Context.ContentResolver.OpenFileDescriptor(uri, "r");

                if (parcelFileDescriptor != null)
                {
                    // Get the file descriptor from the parcel file descriptor
                    FileDescriptor fileDescriptor = parcelFileDescriptor.FileDescriptor;

                    // Create a Java.IO.File from the file descriptor
                    Java.IO.File file = new Java.IO.File(fileDescriptor.ToString());

                    // Close the parcel file descriptor
                    parcelFileDescriptor.Close();

                    return file;
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.Message);
                // Handle the exception as needed
            }

            return null; // Conversion failed
        }

        public async Task<byte[]> OutgoingFileData(string fileName)
        {
            try
            {
                byte[] output = default;
                // Construct the URI for the shared file based on your content provider's authority and the file name
                Android.Net.Uri sharedFileUri = Android.Net.Uri.Parse(fileName);

                // Open an input stream from the content resolver
                using (System.IO.Stream inputStream = Application.Context.ContentResolver.OpenInputStream(sharedFileUri))
                {
                    // Read the content of the file into a byte array
                    using (System.IO.MemoryStream memoryStream = new System.IO.MemoryStream())
                    {
                        inputStream.CopyTo(memoryStream);
                        output = memoryStream.ToArray();
                        return output;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.Message);
                // Handle the exception as needed
                return null;
            }
        }

        #region This returns the Uri but I am not sure why Maybe the GetExternalStoragePublicDirectory hast mutlipule options
        //public static Java.IO.File GetExternalStoragePublicDirectory(string pathToModulefolder)
        //{
        //    try
        //    {
        //        // Construct the URI for the shared file based on your content provider's authority and the file name
        //        Android.Net.Uri sharedFileUri = Android.Net.Uri.Parse(pathToModulefolder);

        //        // Open the file descriptor from the content resolver
        //        ParcelFileDescriptor parcelFileDescriptor = Application.Context.ContentResolver.OpenFileDescriptor(sharedFileUri, "r");

        //        if (parcelFileDescriptor != null)
        //        {
        //            // Get the file descriptor from the parcel file descriptor
        //            FileDescriptor fileDescriptor = parcelFileDescriptor.FileDescriptor;

        //            // Create a Java.IO.File from the file descriptor
        //            Java.IO.File file = new Java.IO.File(fileDescriptor);

        //            // Close the parcel file descriptor
        //            parcelFileDescriptor.Close();

        //            return file;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        System.Console.WriteLine(ex.Message);
        //        // Handle the exception as needed
        //    }

        //    return null; // Retrieval failed
        //}
        #endregion
        //}
        //public async Task<bool> FileUnzipperold(string zipFilePath, string unzipFolderPath, string linkName)
        //{
        //    bool unzipped = false;
        //    try
        //    {
        //        // Construct the URI for the zip file based on the file path
        //        Android.Net.Uri zipFileUri = Android.Net.Uri.Parse(zipFilePath);

        //        // Open an InputStream for the zip file
        //        using (var inputStream = Application.Context.ContentResolver.OpenInputStream(zipFileUri))
        //        {
        //            if (inputStream != null)
        //            {
        //                // Create a ZipInputStream to read the zip file
        //                using (var zipInputStream = new ZipInputStream(inputStream))
        //                {
        //                    ZipEntry zipEntry;

        //                    // Loop through each entry in the zip file
        //                    while ((zipEntry = zipInputStream.GetNextEntry()) != null)
        //                    {
        //                        // Construct the target URI for the entry based on the unzip folder path and entry name
        //                        Android.Net.Uri targetUri = Android.Net.Uri.Parse($"content://com.febris.provider/files/{unzipFolderPath}/{zipEntry.Name}");

        //                        // Open an OutputStream for the target entry
        //                        using (var outputStream = Application.Context.ContentResolver.OpenOutputStream(targetUri))
        //                        {
        //                            if (outputStream != null)
        //                            {
        //                                // Copy the contents of the zip entry to the target entry
        //                                zipInputStream.CopyTo(outputStream);
        //                            }
        //                        }
        //                    }
        //                }

        //                unzipped = true;
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        System.Console.WriteLine(ex.Message);
        //        return false;
        //    }

        //    return unzipped;
        //}


    }

    #region Post-Android 11 File Provider
    //[ContentProvider(new string[] { Febris.MobileCompanionV3.Droid.Utilities.ExternalFileProvider.AUTHORITY }, GrantUriPermissions = true, Name = Febris.MobileCompanionV3.Droid.Utilities.ExternalFileProvider.AUTHORITY)]
    //public class ExternalFileProvider : FileProvider
    //{
    //    public const string AUTHORITY = "com.febris.provider";
    //    private readonly Context _context;
    //    public ExternalFileProvider()
    //    {
    //        _context = Android.App.Application.Context;
    //    }

    //    public static bool IsPermissionGranted(Context context)
    //    {
    //        bool output = false;
    //        try
    //        {
    //            if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
    //            {

    //                return Environment.IsExternalStorageManager;
    //            }
    //            else
    //            {
    //                var ReadExtStorage = ContextCompat.CheckSelfPermission(context, Android.Manifest.Permission.ReadExternalStorage);
    //                if (ReadExtStorage != Android.Content.PM.Permission.Granted)
    //                {
    //                    return output;
    //                }
    //                else
    //                {
    //                    output = true;
    //                }

    //            }
    //        }
    //        catch (System.Exception ex)
    //        {
    //            System.Console.WriteLine(ex.StackTrace);
    //            throw;
    //        }
    //        return output;
    //    }

    //    internal static string GetPath(string path)
    //    {
    //        string output = default;
    //        try
    //        {
    //            Uri uri = FileProvider.GetUriForFile(Android.App.Application.Context, AUTHORITY, new File(path));
    //            output = uri.ToString();
    //        }
    //        catch (System.Exception ex)
    //        {
    //            System.Console.WriteLine("Error occured in ExternalFileProvider GetPath: " + ex.Message);
    //            throw;
    //        }
    //        return output;
    //    }







    //}

    #endregion

    #region Post-Android 11 Attempted Content Provider (not working)
    /// <summary>
    /// This is required for android 11+ as they decided to remove the ability to access the file system after 10
    /// </summary>    
    [ContentProvider(new string[] { Febris.MobileCompanionV3.Droid.Utilities.ExternalContentProvider.AUTHORITY }, Exported = true, GrantUriPermissions = true, Name = Febris.MobileCompanionV3.Droid.Utilities.ExternalContentProvider.AUTHORITY)]
    public class ExternalContentProvider : ContentProvider
    {
        private SQLiteDatabase _database;
        public const string AUTHORITY = "com.febris.provider";
        public const string table_Name = "XApiStatements";        
       
        public override bool OnCreate()
        {
            try
            {
                // Initialize your database or other necessary setup
                //DatabaseOpenHelper dbHelper = new DatabaseOpenHelper(Context);
                //_database = dbHelper.WritableDatabase;                
                return true;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error in OnCreate: {ex.Message}");
                return false;
            }
        }

        public override ICursor Query(Android.Net.Uri uri, string[] projection, string selection, string[] selectionArgs, string sortOrder)
        {
            try
            {
                // Handle query operations
                // Example: Query data from the database

                SQLiteQueryBuilder builder = new SQLiteQueryBuilder();
                builder.Tables = table_Name;

                ICursor cursor = builder.Query(_database, projection, selection, selectionArgs, null, null, sortOrder);
                cursor.SetNotificationUri(Context.ContentResolver, uri);

                return cursor;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error in Query: {ex.Message}");
                return null;
            }
        }

        public override string GetType(Android.Net.Uri uri)
        {
            // Return the MIME type for the data at the given URI
            return "vnd.android.cursor.item/vnd.example.data";
        }

        public override Android.Net.Uri Insert(Android.Net.Uri uri, ContentValues values)
        {
            try
            {
                // Handle insert operations
                // Example: Insert data into the database

                long id = _database.Insert(table_Name, null, values);
                Context.ContentResolver.NotifyChange(uri, null);

                return Android.Net.Uri.WithAppendedPath(uri, id.ToString());
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error in Insert: {ex.Message}");
                return null;
            }
        }

        public override int Delete(Android.Net.Uri uri, string selection, string[] selectionArgs)
        {
            try
            {
                // Handle delete operations
                // Example: Delete data from the database

                int count = _database.Delete(table_Name, selection, selectionArgs);
                Context.ContentResolver.NotifyChange(uri, null);

                return count;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error in Delete: {ex.Message}");
                return 0;
            }
        }

        public override int Update(Android.Net.Uri uri, ContentValues values, string selection, string[] selectionArgs)
        {
            try
            {
                // Handle update operations
                // Example: Update data in the database

                int count = _database.Update(table_Name, values, selection, selectionArgs);
                Context.ContentResolver.NotifyChange(uri, null);

                return count;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error in Update: {ex.Message}");
                return 0;
            }
        }
        #region Database
        // Inner class for database creation and management
        //private class DatabaseOpenHelper : SQLiteOpenHelper
        //{
        //    private const string DatabaseName = "febris_xApi.db";
        //    private const int DatabaseVersion = 1;

        //    public DatabaseOpenHelper(Context context)
        //        : base(context, DatabaseName, null, DatabaseVersion)
        //    {
        //    }

        //    public override void OnCreate(SQLiteDatabase db)
        //    {
        //        // Create your database tables
        //        // Example: db.ExecSQL("CREATE TABLE IF NOT EXISTS your_table_name (column1 TEXT, column2 TEXT);");
        //    }

        //    public override void OnUpgrade(SQLiteDatabase db, int oldVersion, int newVersion)
        //    {
        //        // Handle database upgrades
        //    }
        //}
        #endregion
    }
    #endregion
}


  
