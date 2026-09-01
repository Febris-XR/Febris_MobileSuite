// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileServerV3.Resources;
using Febris.SharedMobileLibrary.FileSystem;
using Febris.SharedMobileLibrary.Models.Data;
using Febris.SharedMobileLibrary.Utilites;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Febris.MobileServerV3.APIInteractions
{
    public class CompanionAppRequest
    {
        private readonly ILogger _log;
        private readonly IConfiguration _config;
        //private readonly JSONHandler _jSONHandler;
        public string _endpoint;
        private readonly TokenRequest _tokenHandler;

        public CompanionAppRequest(ILogger log, IConfiguration config)
        {
            _log = log;
            _config = config;
            //_jSONHandler = new JSONHandler(_log, _config);
            _tokenHandler = new TokenRequest(_log, _config);
            _endpoint = LocalHardwareStaticDetails.ApiUrl;
        }

        public CompanionAppRequest()
        {
            _tokenHandler = new TokenRequest();// _log, _config);
            _endpoint = LocalHardwareStaticDetails.ApiUrl;
        }



        #region Requests

        private async Task<string> MakeGetRequest(string method, string dataPackage)
        {
            try
            {
                string endpoint = _endpoint;
                IAPIRequestFactory request = new APIRequestFactory()
                {
                    endPoint = endpoint + "CompanionApp/" + method,
                    httpMethod = httpVerb.GET,
                    authTech = AuthenticaitonTechnique.Token,
                    authType = Authenticationtype.BearerToken,
                    postJSON = dataPackage ?? string.Empty,
                    //contentType = "",
                    token = LocalHardwareStaticDetails._hardwareAuthenticationResponse?.JwtToken ?? string.Empty

                };
                string response = string.Empty;
                HttpStatusCode status;
                (response, status) = await request.MakeStringRequest();
                if (status != HttpStatusCode.OK)
                {
                    //await _tokenHandler.Authenticate();
                    bool complete = await _tokenHandler.RenewToken();
                    if (complete)
                    {
                        request.token = LocalHardwareStaticDetails._hardwareAuthenticationResponse.JwtToken;
                        //request.token = StaticDetails.LicenseAuthenticateResponse.JwtToken;
                        (response, status) = await request.MakeStringRequest();
                    }
                }
                return response;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        private async Task<string> MakePostRequest(string method, string dataPackage)
        {
            try
            {
                string endpoint = _endpoint;
                IAPIRequestFactory request = new APIRequestFactory()
                {
                    endPoint = endpoint + "CompanionApp/" + method,
                    httpMethod = httpVerb.POST,
                    authTech = AuthenticaitonTechnique.Token,
                    authType = Authenticationtype.BearerToken,
                    postJSON = dataPackage ?? string.Empty,
                    token = LocalHardwareStaticDetails._hardwareAuthenticationResponse?.JwtToken ?? string.Empty
                };
                string response = string.Empty;
                HttpStatusCode status;
                (response, status) = await request.MakeStringRequest();
                if (status != HttpStatusCode.OK)
                {
                    //await _tokenHandler.Authenticate();
                    bool complete = await _tokenHandler.RenewToken();
                    if (complete)
                    {
                        request.token = LocalHardwareStaticDetails._hardwareAuthenticationResponse.JwtToken;
                        (response, status) = await request.MakeStringRequest();
                    }
                }
                return response;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private async Task<byte[]> MakeDownloadRequest(string method, string dataPackage)
        {
            try
            {
                string endpoint = _endpoint;
                IAPIRequestFactory request = new APIRequestFactory()
                {
                    endPoint = endpoint + "CompanionApp/" + method,
                    httpMethod = httpVerb.GET,
                    authTech = AuthenticaitonTechnique.Token,
                    authType = Authenticationtype.BearerToken,
                    postJSON = dataPackage ?? string.Empty,
                    token = LocalHardwareStaticDetails._hardwareAuthenticationResponse?.JwtToken ?? string.Empty
                };
                byte[] response = { };
                HttpStatusCode status;
                (response, status) = await request.MakeByteArrayRequest();
                if (status != HttpStatusCode.OK)
                {
                    //await _tokenHandler.Authenticate();
                    bool complete = await _tokenHandler.RenewToken();
                    if (complete)
                    {
                        request.token = LocalHardwareStaticDetails._hardwareAuthenticationResponse.JwtToken;
                        (response, status) = await request.MakeByteArrayRequest();
                    }
                }
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
                //return ex.Message;
            }
        }
        #endregion


        public async Task<LocalSoftwarePackage> GetLatestVersion()
        {
            //bool hasModules = false;
            string dataPackage = string.Empty;
            string method = string.Empty;
            string result = string.Empty;
            try
            {
                LocalSoftwarePackage output = new LocalSoftwarePackage();
                method = "getlatestversion";
                result = await MakeGetRequest(method, dataPackage);
                output = JsonConvert.DeserializeObject<LocalSoftwarePackage>(result);
               
                return output;
            }
            catch (Exception e)
            {
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = e.Message;
                throw;
            }
        }


        public async Task<bool> Download(Guid input)
        {
            //bool hasModules = false;
            bool complete = false;
            string dataPackage = string.Empty;
            string method = string.Empty;
            byte[] result = default;
            try
            {
                List<Guid> output = new List<Guid>();
                method = "download/" + input.ToString();                
                result = await MakeDownloadRequest(method, dataPackage);

                if (result == default)
                {
                    return complete;
                }

                //stream result
                using (MemoryStream inputStream = new MemoryStream(result))// result.Content.ReadAsStreamAsync())// new MemorySteam(result)await result.ReadAsStreamAsync())
                {
                    string fileToWriteTo = Path.Combine(FileSystem.CompressedCompanionApplicationPath, input.ToString() + ".zip");
                    //string fileToWriteTo = Path.Combine(PCFileSystem.ZippedModulePath, input.ToString());
                    //write to a file
                    using (Stream streamToWriteTo = File.Open(fileToWriteTo, FileMode.Create))
                    {
                        #region test 3
                        //Process process = SharedServices.Launcher.ProgressBarService.StartProgressBar(input.ToString(), StatusType.Downloading);
                        await inputStream.CopyToAsync(streamToWriteTo);
                        //ProgressBarService.StopProgressBar(process);
                        #endregion
                        complete = true;
                    }
                    //Send to unpacking
                }
                return complete;
            }
            catch (Exception e)
            {
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = e.Message;
                throw;
            }
        }
    }
}
