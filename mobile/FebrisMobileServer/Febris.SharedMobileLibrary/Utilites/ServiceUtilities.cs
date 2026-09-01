// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.SharedMobileLibrary.Services;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Febris.SharedMobileLibrary.Utilites
{
    public class ServiceUtilities
    {
        private ILogger _log;
        private IConfiguration _config;

        public ServiceUtilities(ILogger log, IConfiguration config)
        {
            _log = log;
            _config = config;
        }

        #region Service initalizer
        public void ServiceInitializer()
        {
            try
            {
                ServiceInstalledCheck();
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }
        }

        /// <summary>
        /// Cycles through all services checking them and if they do no exist installing them
        /// </summary>
        private void ServiceInstalledCheck()
        {
            try
            {
                //each each service Then if it is not running start it or install it
                foreach (ServiceOptions service in Enum.GetValues(typeof(ServiceOptions)))
                {
                    bool isInstalled = ServiceIsInstalled(service);
                    if (!isInstalled)
                    {
                        //install it
                        //isInstalled = ServiceInstaller(service);
                        //if (isInstalled)
                        //{
                        ServiceStarter(service);
                        //}
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }
            #region This is for running a service
            //try
            //{
            //    //each each service Then if it is not running start it or install it
            //    foreach (ServiceOptions service in Enum.GetValues(typeof(ServiceOptions)))
            //    {
            //        bool isInstalled = ServiceIsInstalled(service);
            //        if (!isInstalled)
            //        {
            //            //install it
            //            isInstalled = ServiceInstaller(service);
            //            if (isInstalled)
            //            {
            //                ServiceStarter(service);
            //            }
            //        }
            //    }
            //}
            //catch (Exception ex)
            //{
            //    _log.Error(ex.Message);
            //}
            #endregion

        }
        /// <summary>
        /// Checks if the service exists
        /// </summary>
        /// <param name="service"></param>
        /// <returns></returns>
        private bool ServiceIsInstalled(ServiceOptions service)
        {
            #region This is for running as a process
            try
            {
                bool isInstalled = false;
                string serviceName = GetServiceName(service);
                //now check if it is installed
                Process[] process = Process.GetProcesses();
                var serviceCheck = process.FirstOrDefault(s => s.ProcessName == serviceName);

                if (serviceCheck == null)
                {
                    isInstalled = false;
                }
                else
                {
                    isInstalled = true;
                }

                return isInstalled;
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
                return false;
            }
            #endregion
            #region This is for running a service
            //try
            //{
            //    bool isInstalled = false;
            //    string serviceName = GetServiceName(service);
            //    //now check if it is installed
            //    ServiceController sc = new ServiceController(serviceName);
            //    ServiceController[] services = ServiceController.GetServices();
            //    var serviceCheck = services.FirstOrDefault(s => s.ServiceName == serviceName);

            //    if (serviceCheck == null)
            //    {
            //        isInstalled = false;
            //    }
            //    else
            //    {
            //        isInstalled = true;
            //    }

            //    return isInstalled;
            //}
            //catch (Exception ex)
            //{
            //    _log.Error(ex.Message);
            //    return false;
            //}
            #endregion

        }

        #endregion

        #region Service force restart
        /// <summary>
        /// restarts the service without uninstalling it
        /// </summary>
        /// <param name="service"></param>
        public void ServiceRestarter(ServiceOptions service)
        {
            try
            {
                //ServiceRestart(service);
                ServiceReinstaller(service);
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }
            #region This is for running a service
            //try
            //{
            //    ServiceRestart(service);
            //}
            //catch (Exception ex)
            //{
            //    _log.Error(ex.Message);
            //}
            #endregion
        }
        /// <summary>
        /// Uninstalls and reinstalls the service
        /// </summary>
        /// <param name="service"></param>
        public void ServiceReinstaller(ServiceOptions service)
        {
            //uninstall
            try
            {
                ServiceUnInstaller(service);
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }
            //install
            try
            {
                ServiceStarter(service);
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }
            #region This is for running a service
            ////uninstall
            //try
            //{
            //    ServiceUnInstaller(service);
            //}
            //catch (Exception ex)
            //{
            //    _log.Error(ex.Message);
            //}
            ////install
            //try
            //{
            //    ServiceInstaller(service);

            //    ServiceStarter(service);
            //}
            //catch (Exception ex)
            //{
            //    _log.Error(ex.Message);
            //}
            #endregion

        }

        /// <summary>
        /// installs services via topshelf
        /// </summary>
        /// <param name="service"></param>
        /// <returns></returns>
        private bool ServiceInstaller(ServiceOptions service)
        {
            try
            {
                string filePath = GetServicePath(service);
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                process.StartInfo = new System.Diagnostics.ProcessStartInfo(filePath);
                process.StartInfo.Arguments = "install";// --autostart";
                process.StartInfo.UseShellExecute = true;
                process.StartInfo.Verb = "runas";
                process.Start();

                //Thread.Sleep(2000);

                if (process.HasExited)
                {
                    return false;
                }
                else
                {
                    return true;
                }
                //return true;
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
                return false;
            }
            #region This is for running a service
            //try
            //{
            //    string filePath = GetServicePath(service);
            //    System.Diagnostics.Process process = new System.Diagnostics.Process();
            //    process.StartInfo = new System.Diagnostics.ProcessStartInfo(filePath);
            //    process.StartInfo.Arguments = "install";// --autostart";
            //    process.StartInfo.UseShellExecute = true;
            //    process.StartInfo.Verb = "runas";
            //    process.Start();

            //    //Thread.Sleep(2000);

            //    if (process.HasExited)
            //    {
            //        return false;
            //    }
            //    else
            //    {
            //        return true;
            //    }
            //    //return true;
            //}
            //catch (Exception ex)
            //{
            //    _log.Error(ex.Message);
            //    return false;
            //}
            #endregion

        }

        /// <summary>
        /// starts service
        /// </summary>
        /// <param name="service"></param>
        /// <returns></returns>
        private bool ServiceStarter(ServiceOptions service)
        {
            #region This is for process
            try
            {
                string filePath = GetServicePath(service);
                Process process = new Process();
                process.StartInfo = new ProcessStartInfo(filePath);
                process.StartInfo.UseShellExecute = true;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.Start();
                if (process.HasExited)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
                return false;
            }
            #endregion
            #region This is for running a service
            //try
            //{
            //    string filePath = GetServicePath(service);
            //    System.Diagnostics.Process process = new System.Diagnostics.Process();
            //    process.StartInfo = new System.Diagnostics.ProcessStartInfo(filePath);
            //    process.StartInfo.Arguments = "start";
            //    process.StartInfo.UseShellExecute = true;
            //    process.StartInfo.Verb = "runas";
            //    process.Start();
            //    if (process.HasExited)
            //    {
            //        return false;
            //    }
            //    else
            //    {
            //        return true;
            //    }
            //    //return true;
            //}
            //catch (Exception ex)
            //{
            //    _log.Error(ex.Message);
            //    return false;
            //}
            #endregion

        }

        /// <summary>
        /// removes service via topshelf
        /// </summary>
        /// <param name="service"></param>
        /// 
        private void ServiceUnInstaller(ServiceOptions service)
        {
            #region this is for process
            try
            {
                try
                {
                    string filePath = GetServicePath(service);
                    string serviceName = GetServiceName(service);
                    Process[] process = Process.GetProcesses();
                    var serviceCheck = process.FirstOrDefault(s => s.ProcessName == serviceName);
                    serviceCheck.Kill();
                }
                catch (Exception ex)
                {
                    _log.Information(ex.Message);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }
            #endregion
            #region This is for running a service
            //try
            //{
            //    try
            //    {
            //        string filePath = GetServicePath(service);
            //        System.Diagnostics.Process process = new System.Diagnostics.Process();
            //        process.StartInfo = new System.Diagnostics.ProcessStartInfo(filePath);
            //        process.StartInfo.Arguments = "uninstall";
            //        process.StartInfo.UseShellExecute = true;
            //        process.StartInfo.Verb = "runas";
            //        process.Start();
            //    }
            //    catch (Exception ex)
            //    {
            //        _log.Information(ex.Message);
            //    }
            //}
            //catch (Exception ex)
            //{
            //    _log.Error(ex.Message);
            //}
            #endregion

        }
        /// <summary>
        /// restart a specific service
        /// </summary>
        /// <param name="service"></param>
        //private void ServiceRestart(ServiceOptions service)
        //{
        //    //check if service is running
        //    bool exists = ServiceIsInstalled(service);
        //    if (exists)
        //    {
        //        //get service name
        //        string serviceName = GetServiceName(service);
        //        double timeoutMilliseconds = 200;
        //        ServiceController serviceObject = new ServiceController(serviceName);
        //        try
        //        {
        //            int millisec1 = Environment.TickCount;
        //            TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);

        //            serviceObject.Stop();
        //            serviceObject.WaitForStatus(ServiceControllerStatus.Stopped, timeout);

        //            // count the rest of the timeout
        //            int millisec2 = Environment.TickCount;
        //            timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds - (millisec2 - millisec1));

        //            serviceObject.Start();
        //            serviceObject.WaitForStatus(ServiceControllerStatus.Running, timeout);
        //        }
        //        catch (Exception ex)
        //        {
        //            _log.Error(ex.Message);
        //        }
        //    }
        //    #region This is for running a service
        //    //check if service is running
        //    //bool exists = ServiceIsInstalled(service);
        //    //if (exists)
        //    //{
        //    //    //get service name
        //    //    string serviceName = GetServiceName(service);
        //    //    double timeoutMilliseconds = 200;
        //    //    ServiceController serviceObject = new ServiceController(serviceName);
        //    //    try
        //    //    {
        //    //        int millisec1 = Environment.TickCount;
        //    //        TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);

        //    //        serviceObject.Stop();
        //    //        serviceObject.WaitForStatus(ServiceControllerStatus.Stopped, timeout);

        //    //        // count the rest of the timeout
        //    //        int millisec2 = Environment.TickCount;
        //    //        timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds - (millisec2 - millisec1));

        //    //        serviceObject.Start();
        //    //        serviceObject.WaitForStatus(ServiceControllerStatus.Running, timeout);
        //    //    }
        //    //    catch (Exception ex)
        //    //    {
        //    //        _log.Error(ex.Message);
        //    //    }
        //    //}
        //    #endregion
        //}

        #endregion

        #region Miss path calls
        private string GetServicePathNameWithExtention(ServiceOptions service)
        {
            try
            {
                string servicePathName = string.Empty;
                switch (service)
                {
                    case ServiceOptions.Downloader:
                        servicePathName = FileSystem.FileSystem.downloaderName;
                        break;
                    case ServiceOptions.Uploader:
                        servicePathName = FileSystem.FileSystem.uploaderName;
                        break;
                }
                return servicePathName;
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }

            return string.Empty;
            #region This is for running a service
            //try
            //{
            //    string servicePathName = string.Empty;
            //    switch (service)
            //    {
            //        case ServiceOptions.Downloader:
            //            servicePathName = FileSystem.FileSystem.downloaderName;
            //            break;
            //        case ServiceOptions.Uploader:
            //            servicePathName = FileSystem.FileSystem.uploaderName;
            //            break;
            //    }
            //    return servicePathName;
            //}
            //catch (Exception ex)
            //{
            //    _log.Error(ex.Message);
            //}

            //return string.Empty;
            #endregion

        }
        private string GetServiceName(ServiceOptions service)
        {
            try
            {
                string serviceName = string.Empty;
                serviceName = GetServicePathNameWithExtention(service);
                serviceName = Path.GetFileNameWithoutExtension(serviceName);
                return serviceName;
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }

            return string.Empty;
            #region This is for running a service
            //try
            //{
            //    string serviceName = string.Empty;
            //    serviceName = GetServicePathNameWithExtention(service);
            //    serviceName = Path.GetFileNameWithoutExtension(serviceName);
            //    return serviceName;
            //}
            //catch (Exception ex)
            //{
            //    _log.Error(ex.Message);
            //}

            //return string.Empty;
            #endregion

        }
        private string GetServicePath(ServiceOptions service)
        {
            try
            {
                string servicePathName = string.Empty;
                switch (service)
                {
                    case ServiceOptions.Downloader:
                        servicePathName = FileSystem.FileSystem.DownloaderPath;
                        break;
                    case ServiceOptions.Uploader:
                        servicePathName = FileSystem.FileSystem.UploaderPath;
                        break;
                        //case default:
                        //    break;
                }
                return servicePathName;
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }

            return string.Empty;
            #region This is for running a service
            //try
            //{
            //    string servicePathName = string.Empty;
            //    switch (service)
            //    {
            //        case ServiceOptions.Downloader:
            //            servicePathName = FileSystem.FileSystem.DownloaderPath;
            //            break;
            //        case ServiceOptions.Uploader:
            //            servicePathName = FileSystem.FileSystem.UploaderPath;
            //            break;
            //            //case default:
            //            //    break;
            //    }
            //    return servicePathName;
            //}
            //catch (Exception ex)
            //{
            //    _log.Error(ex.Message);
            //}

            //return string.Empty;
            #endregion

        }
        #endregion
    }
}
