// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileServerV3.Resources;
using Febris.SharedMobileLibrary.Models.ViewModels;
using Febris.SharedMobileLibrary.Services;
using Febris.SharedMobileLibrary.Utilites;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Febris.MobileServerV3.APIInteractions
{
    class InitalizationRequest
    {
        private readonly ILogger _log;
        private readonly IConfiguration _config;
        //private readonly JSONHandler _jSONHandler;
        public string _endpoint;
        private readonly TokenRequest _tokenHandler;

        public InitalizationRequest(ILogger log, IConfiguration config)
        {
            _log = log;
            _config = config;
            //_jSONHandler = new JSONHandler(_log, _config);
            _tokenHandler = new TokenRequest(_log, _config);
            _endpoint = LocalHardwareStaticDetails.ApiUrl;
        }

        public InitalizationRequest()
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
                    endPoint = endpoint + "Launcher/" + method,
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
                        //request.token = LocalHardwareStaticDetails.LicenseAuthenticateResponse.JwtToken;
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
                    endPoint = endpoint + "Launcher/" + method,
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

        #endregion

        public async Task<HardwareInitializationResponse> Initalize()
        {
            HardwareInitializationResponse output = new HardwareInitializationResponse();
            string dataPackage = string.Empty;
            string method = string.Empty;
            string result = string.Empty;
            try
            {
                result = await MakeGetRequest(method, dataPackage);
                output = JsonConvert.DeserializeObject<HardwareInitializationResponse>(result);
                //LocalHardwareStaticDetails._hardwareInitializationResponse = output;
                return output;
            }
            catch (Exception ex)
            {
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = ex.Message;
                throw;
            }
        }


    }
}
