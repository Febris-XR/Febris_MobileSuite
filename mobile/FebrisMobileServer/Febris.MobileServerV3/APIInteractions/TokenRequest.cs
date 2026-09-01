// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileApp.Resources;
using Febris.MobileServerV3.Resources;
using Febris.SharedMobileLibrary.FileSystem;
using Febris.SharedMobileLibrary.P2pNetworking.Crypto;
using Febris.SharedMobileLibrary.Interfaces;
using Febris.SharedMobileLibrary.Models;
using Febris.SharedMobileLibrary.Services;
using Febris.SharedMobileLibrary.Utilites;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Serilog;
using System;
using System.IO.MemoryMappedFiles;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Febris.MobileServerV3.APIInteractions
{
    class TokenRequest
    {
        //private readonly JSONHandler _jSONHandler;
        private IDevice _uniqueId;
        private ILogger _log;
        private IConfiguration _config;
        public string _endpoint;

        // NODE-9. Where the node-minted device credential lives. Constructed directly rather than
        // resolved, matching how MainActivity builds the P2P pairing store over the same backing.
        private readonly DeviceCredentialStore _credentialStore =
            new DeviceCredentialStore(new EssentialsSecureKeyValueStore());

        public TokenRequest(ILogger log)
        {
            _log = log;
            // _jSONHandler = new JSONHandler(_log);
            //_endpoint = LocalHardwareStaticDetails.PassedBackConfig.GetSection("ApiUrlPath").GetValue<string>("AuthenticationApi");
            
            _uniqueId = DependencyService.Get<IDevice>();
            _endpoint = LocalHardwareStaticDetails.ApiUrl;
        }

        public TokenRequest(ILogger log, IConfiguration config)
        {
            _log = log;
            _config = config;
            //_jSONHandler = new JSONHandler(_log);
            //_endpoint = LocalHardwareStaticDetails.PassedBackConfig.GetSection("ApiUrlPath").GetValue<string>("AuthenticationApi");

            _uniqueId = DependencyService.Get<IDevice>();
            _endpoint = LocalHardwareStaticDetails.ApiUrl;
        }

        public TokenRequest()
        {
            _uniqueId = DependencyService.Get<IDevice>();
            _endpoint = LocalHardwareStaticDetails.ApiUrl;
        }


        #region Requests
        private async Task<string> MakeAuthenticationPostRequest(string method, string dataPackage)
        {
            try
            {
                string endpoint = _endpoint;
                IAPIRequestFactory request = new APIRequestFactory()
                {
                    endPoint = endpoint + "Token/" + method,
                    httpMethod = httpVerb.POST,
                    authTech = AuthenticaitonTechnique.None,
                    authType = Authenticationtype.Basic,
                    postJSON = dataPackage ?? string.Empty,
                    //contentType = "",

                };
                string response = string.Empty;
                HttpStatusCode status;
                (response, status) = await request.MakeStringRequest();
                if (status != HttpStatusCode.OK)
                {

                }
                return response;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        private async Task<string> MakeGetRequest(string method, string dataPackage)
        {
            try
            {
                string endpoint = _endpoint;
                IAPIRequestFactory request = new APIRequestFactory()
                {
                    endPoint = endpoint + "Token/" + method,
                    httpMethod = httpVerb.GET,
                    authTech = AuthenticaitonTechnique.Token,
                    authType = Authenticationtype.BearerToken,
                    postJSON = dataPackage ?? string.Empty,
                    //contentType = "",
                    token = LocalHardwareStaticDetails._hardwareAuthenticationResponse?.RefreshToken ?? string.Empty

                };
                string response = string.Empty;
                HttpStatusCode status;
                (response, status) = await request.MakeStringRequest();
                if (status != HttpStatusCode.OK)
                {
                    await Authenticate();
                }
                return response;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        private async Task<string> MakePostRequest(string method, string dataPackage, string refreshTokenOverride = null)
        {
            try
            {
                string endpoint = _endpoint;
                // Bug fix: previously this method always read the refresh
                // token from static state, ignoring any explicit token
                // passed by the caller. Now Refresh(string input) can
                // actually pass its parameter through here. Static read
                // is kept as the fallback so existing callers (none
                // outside Refresh today, but the surface stays robust)
                // don't break.
                string effectiveToken = refreshTokenOverride
                    ?? LocalHardwareStaticDetails._hardwareAuthenticationResponse?.RefreshToken
                    ?? string.Empty;
                IAPIRequestFactory request = new APIRequestFactory()
                {
                    endPoint = endpoint + "Token/" + method,
                    httpMethod = httpVerb.POST,
                    authTech = AuthenticaitonTechnique.Token,
                    authType = Authenticationtype.BearerToken,
                    postJSON = dataPackage ?? string.Empty,
                    token = effectiveToken
                };
                string response = string.Empty;
                HttpStatusCode status;
                (response, status) = await request.MakeStringRequest();
                if (status != HttpStatusCode.OK)
                {

                }
                return response;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        //private async Task<string> MakePutRequest(string method, string dataPackage)
        //{
        //    try
        //    {
        //        string endpoint = _endpoint;
        //        IAPIRequestFactory request = new APIRequestFactory()
        //        {
        //            endPoint = endpoint + "Token/" + method,
        //            httpMethod = httpVerb.PUT,
        //            authTech = AuthenticaitonTechnique.Token,
        //            authType = Authenticationtype.BearerToken,
        //            postJSON = dataPackage ?? string.Empty
        //        };
        //        string response = string.Empty;
        //        HttpStatusCode status;
        //        (response, status) = await request.MakeStringRequest();
        //        if (status != HttpStatusCode.OK)
        //        {

        //        }
        //        return response;
        //    }
        //    catch (Exception ex)
        //    {
        //        return ex.Message;
        //    }
        //}
        #endregion


        public async Task<HardwareAuthenticationResponse> Authenticate()
        {
            string dataPackage = string.Empty;
            string method = string.Empty;
            string result = string.Empty;
            try
            {
                //if (string.IsNullOrEmpty(MainViewModel.ConfigVM.HardwareLicense))
                //{
                //    UniqueIdentifier uniqueIdentifier = new UniqueIdentifier();
                //    MainViewModel.ConfigVM.HardwareLicense = uniqueIdentifier.GetHardwareLicense();
                //}
                // NODE-9. This used to send _uniqueId.GetIdentifier(), a value the device derived
                // for itself. Audit T9 changed the node to MINT the device credential at
                // registration and store only its hash, so a self-derived value matches no row and
                // authentication always fails. The credential the node showed once at registration
                // is entered on the configuration screen and kept in platform secure storage.
                //
                // GetIdentifier() is unchanged everywhere else: it is still the right answer to
                // "which device is this" for statements and P2P. Only AUTHENTICATION moved.
                string deviceCredential = await _credentialStore.GetAsync();
                if (string.IsNullOrWhiteSpace(deviceCredential))
                {
                    // Fail LOUD and stop. Sending the derived identifier as a fallback would
                    // produce a 401 indistinguishable from a wrong credential, which is precisely
                    // how this defect stayed invisible: the request looked well formed and the node
                    // had nothing to match it against.
                    string unregistered =
                        "This device has no Febris credential. Register it on your node's Hardware "
                        + "page, copy the credential it shows once, and enter it on the "
                        + "configuration screen. Authentication cannot proceed without it.";
                    _log?.Error(unregistered);
                    Console.WriteLine(unregistered);
                    return null;
                }

                HardwareAuthenticationRequest request = new HardwareAuthenticationRequest()
                {
                    LicenseKey = deviceCredential
                };
                dataPackage = JsonConvert.SerializeObject(request);
                method = "authenticate";
                result = await MakeAuthenticationPostRequest(method, dataPackage);
                HardwareAuthenticationResponse output = JsonConvert.DeserializeObject<HardwareAuthenticationResponse>(result);
                //LocalHardwareStaticDetails._hardwareAuthenticationResponse = output;
                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }
        public async Task<HardwareAuthenticationResponse> Get(HardwareAuthenticationRequest input)
        {
            string dataPackage = string.Empty;
            string method = string.Empty;
            string result = string.Empty;
            try
            {
                dataPackage = JsonConvert.SerializeObject(input);
                method = "authentication";
                result = await MakeAuthenticationPostRequest(method, dataPackage);
                HardwareAuthenticationResponse output = JsonConvert.DeserializeObject<HardwareAuthenticationResponse>(result);
                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }
        public async Task<HardwareAuthenticationResponse> Refresh(string input)
        {
            // Bug fix: the `input` refresh-token parameter was previously
            // accepted but never actually used -- MakePostRequest read
            // straight from LocalHardwareStaticDetails. Now the parameter
            // is plumbed through. Caller-supplied token wins; static state
            // is the fallback inside MakePostRequest.
            string dataPackage = string.Empty;
            string method = string.Empty;
            string result = string.Empty;
            try
            {
                result = await MakePostRequest(method, dataPackage, input);
                HardwareAuthenticationResponse output = JsonConvert.DeserializeObject<HardwareAuthenticationResponse>(result);
                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        public async Task<bool> RenewToken()
        {
            bool output = default;
            try
            {
                HardwareAuthenticationResponse newTokens = default;
                //try refresh
                if (LocalHardwareStaticDetails._hardwareAuthenticationResponse != default)
                {
                    newTokens = await Refresh(LocalHardwareStaticDetails._hardwareAuthenticationResponse.RefreshToken);
                }

                if (newTokens != null && newTokens != default && !string.IsNullOrEmpty(newTokens.JwtToken) && string.IsNullOrEmpty(newTokens.RefreshToken))
                {
                    LocalHardwareStaticDetails._hardwareAuthenticationResponse.JwtToken = newTokens.JwtToken;
                }
                else if (newTokens != null && !string.IsNullOrEmpty(newTokens.JwtToken) && !string.IsNullOrEmpty(newTokens.RefreshToken))
                {
                    LocalHardwareStaticDetails._hardwareAuthenticationResponse = newTokens;
                }
                else
                {
                    newTokens = await Authenticate();
                    LocalHardwareStaticDetails._hardwareAuthenticationResponse = newTokens;
                }
                if (newTokens != default)
                {
                    output = true;
                }
                //try get
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
            return output;
        }



        //public async Task<string> GetToken()
        //{
        //    string token = string.Empty;
        //    PCDataProtection dataProtection = new PCDataProtection();
        //    bool credentialsGathered = dataProtection.CredentialsExist().Result;
        //    if (credentialsGathered)
        //    {
        //        dataPackage = JsonConvert.SerializeObject(input);
        //        method = "authentication";
        //        result = await MakeAuthenticationPostRequest(method, dataPackage);
        //        //FebrisRestClient febrisRestClientInitalization = new FebrisRestClient(_log)
        //        //{
        //        //    endPoint = FebrisLocalLibrary.SharedDetails.SharedDetails.getToken,
        //        //    authType = Authenticationtype.Basic,
        //        //    httpMethod = httpVerb.GET,
        //        //};

        //        //populate Test list
        //        string response = string.Empty;
        //        var task = febrisRestClientInitalization.MakeRequest();
        //        task.Wait();
        //        response = task.Result;
        //        if (response == string.Empty)
        //        {
        //            //Login loginWindow = new Login();
        //            //loginWindow.ShowDialog();
        //        }
        //        //token = Utilites.JSONHandler.SetToken(response);
        //        token = _jSONHandler.DeserializeToken(response);
        //        bool tokenSet = StoreToken(token);
        //    }
        //    return token;
        //}

    }
}
