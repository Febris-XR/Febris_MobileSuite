// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only


using Febris.SharedMobileLibrary.Models;
using Febris.SharedMobileLibrary.Models.Data;
using Febris.ModelLibrary.Models.XApiModels;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Febris.MobileCompanionV3.DataLogic
{
    public class LocalDatabaseConstants
    {
        public const string _localDB = "Febris_new_local.db3";

        public const SQLite.SQLiteOpenFlags Flags =
          // open the database in read/write mode
          SQLite.SQLiteOpenFlags.ReadWrite |
          // create the database if it doesn't exist
          SQLite.SQLiteOpenFlags.Create |
          // enable multi-threaded database access
          SQLite.SQLiteOpenFlags.SharedCache;

        public static string DatabasePath
        {
            get
            {
                var basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);//.LocalApplicationData);
                return Path.Combine(basePath, LocalDatabaseConstants._localDB);
            }
        }
    }
    //public class LocalDatabaseContext : SQLiteConnection
    //{
    //    static SQLiteAsyncConnection _dbContext;

    //    public LocalDatabaseContext(string databasePath) : base(databasePath)
    //    {
    //        InitializeDatabase().Wait();

    //        //_dbContext = new SQLiteAsyncConnection(LocalDatabaseConstants.DatabasePath, LocalDatabaseConstants.Flags);
    //        //_dbContext.CreateTableAsync<GroupOwnerDevice>();
    //        //_dbContext.CreateTableAsync<Module>();
    //        //_dbContext.CreateTableAsync<RawStatement>();
    //        //_dbContext.CreateTableAsync<ModuleFileModel>();
    //        //_dbContext.CreateTableAsync<MediaModel>();
    //        //_dbContext.CreateTableAsync<ModulePackageModel>();
    //    }

    //    public async Task InitializeDatabase()
    //    {

    //        _dbContext = new SQLiteAsyncConnection(LocalDatabaseConstants.DatabasePath, LocalDatabaseConstants.Flags);

    //        var ownerdev = await _dbContext.CreateTableAsync<GroupOwnerDevice>();
    //        var module = await _dbContext.CreateTableAsync<Module>();
    //        var statement = await _dbContext.CreateTableAsync<RawStatement>();
    //        var ModuleFile = await _dbContext.CreateTableAsync<ModuleFileModel>();
    //        var media = await _dbContext.CreateTableAsync<MediaModel>();
    //        var package = await _dbContext.CreateTableAsync<ModulePackageModel>();
    //    }
    //}
    public class LocalDatabase
    {
        // NOTE (MDM-B8): Static SQLite connection is reassigned by the ctor (line below), shared across all instances alongside the SharedCache flag (LocalDatabaseConstants.Flags). Deferred per do-not-change-functionality to the DI refactor (MOD-6). See docs/MODERNIZATION/MDM_MODERNIZATION.md.
        private static SQLiteAsyncConnection _dbContext;

        public static AsyncLazy<LocalDatabase> Instance = new AsyncLazy<LocalDatabase>(async () =>
        {
            var instance = new LocalDatabase();
            var ownerdev = await _dbContext.CreateTableAsync<GroupOwnerDevice>();
            var media = await _dbContext.CreateTableAsync<MediaModel>();
            var moduleClass = await _dbContext.CreateTableAsync<ModuleClassification>();
            var module = await _dbContext.CreateTableAsync<ModuleStorage>();
            var moduleFile = await _dbContext.CreateTableAsync<ModuleFileModel>();
            var package = await _dbContext.CreateTableAsync<ModulePackageStorageModel>();
            var statement = await _dbContext.CreateTableAsync<RawStatement>();
            return instance;
        });

        public LocalDatabase()
        {
            _dbContext = new SQLiteAsyncConnection(LocalDatabaseConstants.DatabasePath, LocalDatabaseConstants.Flags);
            //InitializeDatabase();//.Wait();            
        }

        //public async Task InitializeDatabase()
        //{

        //    //_dbContext = new SQLiteAsyncConnection(LocalDatabaseConstants.DatabasePath, LocalDatabaseConstants.Flags);
        //    try
        //    {
        //        //await _dbContext.CreateTableAsync<GroupOwnerDevice>();
        //        //await _dbContext.CreateTableAsync<Module>();
        //        //await _dbContext.CreateTableAsync<RawStatement>();
        //        //await _dbContext.CreateTableAsync<ModuleFileModel>();
        //        //await _dbContext.CreateTableAsync<MediaModel>();
        //        //await _dbContext.CreateTableAsync<ModulePackageModel>();
        //        var ownerdev = await _dbContext.CreateTableAsync<GroupOwnerDevice>();
        //        var module = await _dbContext.CreateTableAsync<Module>();
        //        var statement = await _dbContext.CreateTableAsync<RawStatement>();
        //        var ModuleFile = await _dbContext.CreateTableAsync<ModuleFileModel>();
        //        var media = await _dbContext.CreateTableAsync<MediaModel>();
        //        var package = await _dbContext.CreateTableAsync<ModulePackageModel>();
        //        //_dbContext.CreateTableAsync<GroupOwnerDevice>().Wait();
        //        //_dbContext.CreateTableAsync<Module>().Wait();
        //        //_dbContext.CreateTableAsync<RawStatement>().Wait();
        //        //_dbContext.CreateTableAsync<ModuleFileModel>().Wait();
        //        //_dbContext.CreateTableAsync<MediaModel>().Wait();
        //        //_dbContext.CreateTableAsync<ModulePackageModel>().Wait();
        //        //var ownerdev = _dbContext.CreateTableAsync<GroupOwnerDevice>().Result;
        //        //var module = _dbContext.CreateTableAsync<Module>().Result;
        //        //var statement =  _dbContext.CreateTableAsync<RawStatement>().Result;
        //        //var ModuleFile = _dbContext.CreateTableAsync<ModuleFileModel>().Result;
        //        //var media = _dbContext.CreateTableAsync<MediaModel>().Result;
        //        //var package = _dbContext.CreateTableAsync<ModulePackageModel>().Result;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.StackTrace);
        //        throw;
        //    }

        //}


        #region Generic        
        //public async Task<TViewModel> GetViewModel<TSearchModel, Tchild1, Tchild2, TViewModel>(long moduleId, Func<T, bool> predicate) where T : new()
        //{
        //    try
        //    {
        //        // First query to get the storage model based on Module.Id
        //        var storageModel = await Get<T>(moduleId, predicate);

        //        if (storageModel != null)
        //        {
        //            // Additional logic if needed to retrieve related entities based on storage model properties

        //            // Construct ViewModel from the storage model
        //            var viewModel = new TViewModel
        //            {
        //                // Map properties accordingly
        //            };

        //            return viewModel;
        //        }

        //        return default;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.StackTrace);
        //        throw;
        //    }
        //}

        public async Task<T> Get<T>(Func<T, bool> predicate) where T : new()
        {
            T output = default;
            try
            {
                List<T> list = await _dbContext.Table<T>().ToListAsync();

                output = list.FirstOrDefault(predicate);

                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task<bool> Exists<T>(Expression<Func<T, bool>> predicate) where T : class, new()
        {
            try
            {
                List<T> list = await _dbContext.Table<T>().ToListAsync();
                return list.Any(predicate.Compile());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task<List<T>> GetList<T>() where T : new()
        {
            try
            {
                List<T> output = await _dbContext.Table<T>().ToListAsync() ?? default;
                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task<T> GetLastItem<T>(string orderByPropertyName) where T : new()
        {
            try
            {
                T output = await _dbContext.Table<T>()
                    .OrderByDescending(CreateOrderByExpression<T>(orderByPropertyName))
                    .FirstOrDefaultAsync();
                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return default;
                //throw;
            }
            // Order by descending based on the specified property

        }

        private Expression<Func<T, object>> CreateOrderByExpression<T>(string propertyName)
        {
            var parameter = Expression.Parameter(typeof(T));
            var property = Expression.Property(parameter, propertyName);
            var lambda = Expression.Lambda<Func<T, object>>(Expression.Convert(property, typeof(object)), parameter);
            return lambda;
        }

        public async Task<T> Create<T>(T input) where T : new()
        {
            try
            {
                await _dbContext.InsertAsync(input);
                return input;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task<T> Update<T>(T input) where T : new()
        {
            try
            {
                await _dbContext.UpdateAsync(input);
                return input;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task<bool> Delete<T>(T input) where T : new()
        {
            try
            {
                await _dbContext.DeleteAsync(input);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }


        #endregion
        #region Module
        public async Task<List<Module>> GetFullModuleList()
        {
            try
            {
                return await _dbContext.Table<Module>().ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task<long> Create(Module input)
        {
            try
            {
                return await _dbContext.InsertAsync(input);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task<long> Update(Module input)
        {
            try
            {
                return await _dbContext.UpdateAsync(input);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task<bool> Delete(Module input)
        {
            try
            {
                await _dbContext.DeleteAsync(input);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }


        #endregion

        #region Statements
        public async Task<List<RawStatement>> GetFullStatementList()
        {
            try
            {
                return await _dbContext.Table<RawStatement>().ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task<long> Create(RawStatement input)
        {
            try
            {
                return await _dbContext.InsertAsync(input);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task<long> Update(RawStatement input)
        {
            try
            {
                return await _dbContext.UpdateAsync(input);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task<bool> Delete(RawStatement input)
        {
            try
            {
                await _dbContext.DeleteAsync(input);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }


        #endregion

    }
    public class AsyncLazy<T>
    {
        readonly Lazy<Task<T>> instance;

        public AsyncLazy(Func<T> factory)
        {
            instance = new Lazy<Task<T>>(() => Task.Run(factory));
        }

        public AsyncLazy(Func<Task<T>> factory)
        {
            instance = new Lazy<Task<T>>(() => Task.Run(factory));
        }

        public TaskAwaiter<T> GetAwaiter()
        {
            return instance.Value.GetAwaiter();
        }
    }


    #region for testing

    //public class Constants
    //{
    //    public const string _localDB = "Todo_local.db3";

    //    public const SQLite.SQLiteOpenFlags Flags =
    //      // open the database in read/write mode
    //      SQLite.SQLiteOpenFlags.ReadWrite |
    //      // create the database if it doesn't exist
    //      SQLite.SQLiteOpenFlags.Create |
    //      // enable multi-threaded database access
    //      SQLite.SQLiteOpenFlags.SharedCache;

    //    public static string DatabasePath
    //    {
    //        get
    //        {
    //            var basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    //            return Path.Combine(basePath, LocalDatabaseConstants._localDB);
    //        }
    //    }
    //}
    //public class TodoItemDatabase
    //{
    //    static SQLiteAsyncConnection Database;

    //    public static readonly AsyncLazy<TodoItemDatabase> Instance = new AsyncLazy<TodoItemDatabase>(async () =>
    //    {
    //        var instance = new TodoItemDatabase();
    //        CreateTableResult result = await Database.CreateTableAsync<TodoItem>();
    //        return instance;
    //    });

    //    public TodoItemDatabase()
    //    {
    //        Database = new SQLiteAsyncConnection(Constants.DatabasePath, Constants.Flags);
    //    }

    //    public Task<List<TodoItem>> GetItemsAsync()
    //    {
    //        return Database.Table<TodoItem>().ToListAsync();
    //    }

    //    public Task<List<TodoItem>> GetItemsNotDoneAsync()
    //    {
    //        return Database.QueryAsync<TodoItem>("SELECT * FROM [TodoItem] WHERE [Done] = 0");
    //    }

    //    public Task<TodoItem> GetItemAsync(int id)
    //    {
    //        return Database.Table<TodoItem>().Where(i => i.ID == id).FirstOrDefaultAsync();
    //    }

    //    public Task<int> SaveItemAsync(TodoItem item)
    //    {
    //        if (item.ID != 0)
    //        {
    //            return Database.UpdateAsync(item);
    //        }
    //        else
    //        {
    //            return Database.InsertAsync(item);
    //        }
    //    }

    //    public Task<int> DeleteItemAsync(TodoItem item)
    //    {
    //        return Database.DeleteAsync(item);
    //    }
    //}
    //public class TodoItem
    //{
    //    [PrimaryKey, AutoIncrement]
    //    public int ID { get; set; }
    //    public string Name { get; set; }
    //    public string Notes { get; set; }
    //    public bool Done { get; set; }
    //}
    #endregion

}
