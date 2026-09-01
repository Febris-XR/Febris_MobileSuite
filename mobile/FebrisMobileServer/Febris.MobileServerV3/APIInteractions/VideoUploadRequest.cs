// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileServerV3.Resources;
using Febris.SharedMobileLibrary.Models.ViewModels;
using Febris.SharedMobileLibrary.Utilites;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Febris.MobileServerV3.APIInteractions
{
    class VideoUploadRequest
    {
        public string _endpoint;
        private readonly TokenRequest _tokenHandler;
        private readonly IConfiguration _config;
        //private readonly JSONHandler _jSONHandler;
        private ILogger _log;

        public VideoUploadRequest(ILogger log, IConfiguration config)
        {
            _log = log;
            _config = config;
            //_jSONHandler = new JSONHandler(_log, _config);
            _tokenHandler = new TokenRequest(_log, _config);


            _endpoint = LocalHardwareStaticDetails.ApiUrl;
        }

        //public VideoUploadRequest(ILogger log)
        //{
        //    _log = log;
        //}


        #region Requests

        private async Task<string> MakeGetRequest(string method, string dataPackage)
        {
            try
            {
                string endpoint = _endpoint;
                IAPIRequestFactory request = new APIRequestFactory()
                {
                    endPoint = endpoint + "Video/" + method,
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
        private async Task<string> MakePostRequest(string method, string fileName)
        {
            try
            {
                string endpoint = _endpoint;
                IAPIRequestFactory request = new APIRequestFactory()
                {
                    endPoint = endpoint + "Video/" + method,
                    httpMethod = httpVerb.POST,
                    authTech = AuthenticaitonTechnique.Token,
                    authType = Authenticationtype.BearerToken,
                    //postDataPackage = dataPackage,
                    //postJSON = dataPackage ?? string.Empty,
                    token = LocalHardwareStaticDetails._hardwareAuthenticationResponse?.JwtToken ?? string.Empty
                };
                string response = string.Empty;
                HttpStatusCode status;
                (response, status) = await request.MakeMultipartFormRequest(fileName);
                if (status != HttpStatusCode.OK)
                {
                    bool complete = await _tokenHandler.RenewToken();
                    if (complete)
                    {
                        request.token = LocalHardwareStaticDetails._hardwareAuthenticationResponse.JwtToken;
                        //request.token = StaticDetails.LicenseAuthenticateResponse.JwtToken;
                        (response, status) = await request.MakeMultipartFormRequest(fileName);
                    }
                }
                return response;
            }
            catch (Exception ex)
            {
                throw;
                //return ex.Message;
            }
        }



        #endregion


        internal async Task<bool> VideoUpload(string fileName)
        {
            //byte[] dataPackage = default;
            string method = string.Empty;
            string result = string.Empty;
            try
            {
                //dataPackage = System.IO.File.ReadAllBytes(fileName);
                result = await MakePostRequest(method, fileName);
                //string stringResult = System.Text.Encoding.UTF8.GetString(result);
                VideoFileUploadResponseViewModel output = JsonConvert.DeserializeObject<VideoFileUploadResponseViewModel>(result);
                return output.Success;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }

            #region removed
            ////make awaiter to essentually make a que so the http requests are not overwhelmed 
            //await Service.AsyncAwaiter.AwaitAsync(nameof(FebrisRestClient), async () =>
            //{
            //hardwareLicense = Utilites.UniqueIdentifier.GetHardwareLicense();
            //token = TokenHandler.GetStoredToken();
            //hardwareLicense = _uniqueIdentifier.GetHardwareLicense();
            //token = _tokenHandler.GetStoredToken();
            //if (token == string.Empty)
            //{
            //    return false;
            //}
            //HttpClientHandler httpClientHandler = new HttpClientHandler();
            //using (var client = new HttpClient(httpClientHandler))
            //{
            //    using (MultipartFormDataContent content = new MultipartFormDataContent())
            //    {
            //        //var fileContent = new ByteArrayContent(System.IO.File.ReadAllBytes(FileName));
            //        //fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")//have to make sure this is form data. 
            //        //{
            //        //    FileName = Path.GetFileName(FileName)
            //        //};
            //        //content.Add(fileContent);
            //        #region Authenticaiton and headers
            //        //############################################################################################################################
            //        //add headers  
            //        //-----------Says I am miss using headers here. 
            //        //############################################################################################################################
            //        //string authHeader = string.Empty;
            //        //fileContent.Headers.Add("Authorization", "Bearer " + StaticDetails.token);//authType.ToString() + " " + authHeader);
            //        //fileContent.Headers.Add("hardwareLicense", StaticDetails.hardwareId);                    
            //        //client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            //        //client.DefaultRequestHeaders.Add("hardwareLicense", hardwareLicense);

            //        //############################################################################################################################
            //        #endregion


            //        content.Add(fileContent);

            //        var requestUri = SharedDetails.SharedDetails.VideoUploaderUrl;// + StaticDetails.UniqueId;
            //        requestUri = AlterEndpoint(requestUri).Result;

            //        try
            //        {
            //            using (HttpResponseMessage result = client.PostAsync(requestUri, content).Result)
            //            {
            //                if (result.StatusCode != HttpStatusCode.OK)
            //                {
            //                    //Try to get new token if it fails initally. 
            //                    try
            //                    {
            //                        _tokenHandler.GetToken();
            //                    }
            //                    catch (Exception)
            //                    {
            //                        throw new ApplicationException("error code: " + result.StatusCode.ToString());
            //                    }
            //                }
            //                else
            //                {
            //                    rslt = true;
            //                }
            //            }
            //        }
            //        catch (Exception ex)
            //        {
            //            _log.LogError(ex.Message);
            //        }
            //    }
            //}
            //});
            #endregion

        }
    }
    
}
