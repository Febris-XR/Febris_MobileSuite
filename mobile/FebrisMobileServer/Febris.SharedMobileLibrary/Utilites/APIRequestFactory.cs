// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Febris.SharedMobileLibrary.Utilites
{

    #region Enum options
    public enum httpVerb
    {
        GET,
        POST,
        PUT,
        DELETE
    }
    public enum Authenticationtype
    {
        Basic,
        License,
        BearerToken,
        Cookie
    }
    public enum AuthenticaitonTechnique
    {
        None,
        Token,
        //RefreshToken,
        Cookie
    }

    //public enum ContentType
    //{
    //    application/json,
    //    RefreshToken,
    //    Cookie
    //}
    public enum ReturnDataType
    {
        none,
        stringReturn,
        bytearray
    }
    #endregion

    public interface IAPIRequestFactory
    {
        string token { get; set; }
        Task<(string output, HttpStatusCode statusCode)> MakeStringRequest();
        Task<(byte[] output, HttpStatusCode statusCode)> MakeByteArrayRequest();
        Task<(string output, HttpStatusCode statusCode)> MakeMultipartFormRequest(string FileName);
    }

    public class APIRequestFactory : IAPIRequestFactory
    {
        #region variables
        private readonly ILogger _log;
        private readonly IConfiguration _config;

        //private readonly TokenHandler _tokenHandler;
        //private readonly Utilites.UniqueIdentifier _uniqueIdentifier;
        //private readonly DataProtection _dataProtection;
        //private readonly FebrisLocalLibrary.Service.ConfigSettings _configSettings;
        //private string _token;
        #endregion

        #region internal models
        public string endPoint { get; set; }
        public httpVerb httpMethod { get; set; }
        public Authenticationtype authType { get; set; }
        public AuthenticaitonTechnique authTech { get; set; }
        public string contentType { get; set; }
        public string token { get; set; }
        public string license { get; set; }
        public string postJSON { get; set; }
        public byte[] postDataPackage { get; set; }
        public string snickerDoodle { get; set; }

        #endregion



        #region generic request



        #region String based
        public async Task<(string output, HttpStatusCode statusCode)> MakeStringRequest()
        {
            //await AlterEndpoint();
            //###############################################################################################################################
            //Utilites.UniqueIdentifier.SetHardwareLicense();
            //set token
            string output = default;

            try
            {
                WebRequest request = WebRequest.Create(endPoint);
                request = await AttachNeededHeaders(request);
                request.Method = httpMethod.ToString();
                request.ContentType = "application/json";


                #region - Get
                if (request.Method == httpVerb.GET.ToString())
                {
                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    {
                        #region Response Check
                        if (response.StatusCode != HttpStatusCode.OK)
                        {
                            try
                            {
                                return (output, response.StatusCode);
                                //bool successful = await BadRequestHandler();
                                //if (successful)
                                //{

                                //}

                            }
                            catch (Exception ex)
                            {
                                throw;
                            }
                        }
                        #endregion
                        //request.ContentType = "application/json";
                        #region Get Request    
                        //stream response data
                        using (Stream responseStream = response.GetResponseStream())
                        {

                            if (responseStream != null)
                            {
                                using (StreamReader streamToWriteTo = new StreamReader(responseStream))
                                {
                                    output = await streamToWriteTo.ReadToEndAsync();
                                    //using (StreamReader reader = new StreamReader(responseStream))
                                    //{
                                    //    strResponseValue = reader.ReadToEnd();
                                    //}
                                }
                            }
                        }
                        return (output, response.StatusCode);
                        #endregion
                    }

                    #region Get requestion cannot contain a body with webrequests
                    //if (postJSON != string.Empty)
                    //{
                    //    //request.ContentType = "application/json";
                    //    using (StreamWriter swJSONPayload = new StreamWriter(request.GetRequestStream()))
                    //    {
                    //        swJSONPayload.Write(postJSON);
                    //        swJSONPayload.Close();
                    //        #region Post Response
                    //        using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    //        {
                    //            #region Response Check
                    //            if (response.StatusCode != HttpStatusCode.OK)
                    //            {
                    //                try
                    //                {
                    //                    return (output, response.StatusCode);
                    //                    //bool successful = await BadRequestHandler();
                    //                    //if (successful)
                    //                    //{

                    //                    //}

                    //                }
                    //                catch (Exception ex)
                    //                {
                    //                    throw;
                    //                }
                    //            }
                    //            #endregion
                    //            //request.ContentType = "application/json";
                    //            #region Get Request    
                    //            //stream response data
                    //            using (Stream responseStream = response.GetResponseStream())
                    //            {

                    //                if (responseStream != null)
                    //                {
                    //                    using (StreamReader streamToWriteTo = new StreamReader(responseStream))
                    //                    {
                    //                        output = await streamToWriteTo.ReadToEndAsync();
                    //                        //using (StreamReader reader = new StreamReader(responseStream))
                    //                        //{
                    //                        //    strResponseValue = reader.ReadToEnd();
                    //                        //}
                    //                    }
                    //                }
                    //            }
                    //            return (output, response.StatusCode);
                    //            #endregion
                    //        }                           
                    //        #endregion
                    //    }
                    //}
                    //else
                    //{
                    //    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    //    {
                    //        #region Response Check
                    //        if (response.StatusCode != HttpStatusCode.OK)
                    //        {
                    //            try
                    //            {
                    //                return (output, response.StatusCode);
                    //                //bool successful = await BadRequestHandler();
                    //                //if (successful)
                    //                //{

                    //                //}

                    //            }
                    //            catch (Exception ex)
                    //            {
                    //                throw;
                    //            }
                    //        }
                    //        #endregion
                    //        //request.ContentType = "application/json";
                    //        #region Get Request    
                    //        //stream response data
                    //        using (Stream responseStream = response.GetResponseStream())
                    //        {

                    //            if (responseStream != null)
                    //            {
                    //                using (StreamReader streamToWriteTo = new StreamReader(responseStream))
                    //                {
                    //                    output = await streamToWriteTo.ReadToEndAsync();
                    //                    //using (StreamReader reader = new StreamReader(responseStream))
                    //                    //{
                    //                    //    strResponseValue = reader.ReadToEnd();
                    //                    //}
                    //                }
                    //            }
                    //        }
                    //        return (output, response.StatusCode);
                    //        #endregion
                    //    }
                    //}
                    #endregion
                }
                #endregion
                #region - Post 
                if (request.Method == httpVerb.POST.ToString() && postJSON != string.Empty)
                {
                    //using (StreamWriter swJSONPayload = new StreamWriter(await request.GetRequestStreamAsync()))//(request.GetRequestStream()))
                    using (StreamWriter swJSONPayload = new StreamWriter(request.GetRequestStream()))
                    {
                        swJSONPayload.Write(postJSON);
                        //await swJSONPayload.WriteAsync(postJSON);//.Write(postJSON);
                        swJSONPayload.Close();
                        #region Post Responsed
                        using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                        {
                            //output = await new StreamReader(response.GetResponseStream()).ReadToEndAsync();//.ReadToEnd();
                            output = new StreamReader(response.GetResponseStream()).ReadToEnd();
                            return (output, response.StatusCode);
                        }
                        #endregion
                    }
                }
                #endregion

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                //_log.LogWarning(ex.Message);
                if (ex.Message == "The remote server returned an error: (401) Unauthorized.")
                {
                    //token = _tokenHandler.GetToken();
                }
                //return (string.Empty, HttpStatusCode.InternalServerError);
                //throw;
            }
            return (string.Empty, HttpStatusCode.InternalServerError);
            //return output;
            //return (T)strResponseValue;
        }
        #endregion
        #region Byte Array based
        /// <summary>
        /// No idea if this is actually working. Not tested *****************************
        /// </summary>
        /// <returns></returns>
        public async Task<(byte[] output, HttpStatusCode statusCode)> MakeByteArrayRequest()
        {
            //await AlterEndpoint();
            //###############################################################################################################################
            //Utilites.UniqueIdentifier.SetHardwareLicense();
            //set token
            byte[] output = default;

            try
            {
                //WebRequest request = WebRequest.Create(endPoint);
                HttpClientHandler request = new HttpClientHandler();
                //request = await AttachNeededHeaders(request);
                //request.Method = httpMethod.ToString();
                //request.ContentType = "application/json";                
                #region - tried to combine but it did not work
                //using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                //{
                //    #region Response Check
                //    //if (response.StatusCode != HttpStatusCode.OK)
                //    //{
                //    //    try
                //    //    {
                //    //        return (output, response.StatusCode);
                //    //        //bool successful = await BadRequestHandler();
                //    //        //if (successful)
                //    //        //{

                //    //        //}

                //    //    }
                //    //    catch (Exception ex)
                //    //    {
                //    //        throw;
                //    //    }
                //    //}
                //    #endregion

                //    #region Get Request    
                //    //stream response data
                //    //if (request.Method == httpVerb.GET.ToString())
                //    //{
                //    //    using (Stream responseStream = response.GetResponseStream())
                //    //    {
                //    //        if (responseStream != null)
                //    //        {
                //    //            using (MemoryStream streamToWriteTo = new MemoryStream())
                //    //            {
                //    //                await responseStream.CopyToAsync(streamToWriteTo);
                //    //                output = streamToWriteTo.ToArray();
                //    //                //output = (T)Convert.ChangeType(streamToWriteTo.ToArray(), typeof(T));
                //    //            }
                //    //        }
                //    //    }
                //    //}
                //    #endregion

                //    #region Post Request
                //    //if (request.Method == httpVerb.POST.ToString() && postJSON != string.Empty)
                //    //{
                //    //    request.ContentType = "application/json";
                //    //    using (StreamWriter swJSONPayload = new StreamWriter(request.GetRequestStream()))
                //    //    {
                //    //        swJSONPayload.Write(postJSON);
                //    //        swJSONPayload.Close();
                //    //    }
                //    //}
                //    //return (output, response.StatusCode);
                //    #endregion

                //}
                #endregion
                #region - Get
                if (httpMethod == httpVerb.GET)
                {
                    using (HttpClient client = new HttpClient(request))
                    {
                        try
                        {
                            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                            using (HttpResponseMessage response = await client.GetAsync(endPoint, HttpCompletionOption.ResponseHeadersRead))
                            //using (HttpWebResponse response = (HttpWebResponse)request.GetResponse()) //await client.GetAsync(endPoint, HttpCompletionOption.ResponseHeadersRead))
                            {
                                if (response.StatusCode != HttpStatusCode.OK)
                                {
                                    try
                                    {
                                        return (output, response.StatusCode);
                                    }
                                    catch (Exception ex)
                                    {
                                        throw;
                                    }
                                }


                                using (Stream responseStream = await response.Content.ReadAsStreamAsync())//.GetResponseStream())
                                //using (Stream responseStream = response.GetResponseStream())
                                {
                                    if (responseStream != null)
                                    {
                                        using (MemoryStream streamToWriteTo = new MemoryStream())
                                        {
                                            //await streamToWriteTo.LoadIntoStream(responseStream);
                                            //responseStream.Position = 0;
                                            //responseStream.CopyTo(streamToWriteTo);

                                            await responseStream.CopyToAsync(streamToWriteTo);
                                            output = streamToWriteTo.ToArray();
                                            //output = (T)Convert.ChangeType(streamToWriteTo.ToArray(), typeof(T));
                                        }
                                    }
                                }
                                return (output, response.StatusCode);
                            }
                            //return (output, response.StatusCode);
                        }
                        catch (Exception e)
                        {
                            _log.Error(e.Message);

                        }
                    }
                    #region I don't think this was working
                    //using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    //{
                    //    #region Response Check
                    //    if (response.StatusCode != HttpStatusCode.OK)
                    //    {
                    //        try
                    //        {
                    //            return (output, response.StatusCode);
                    //        }
                    //        catch (Exception ex)
                    //        {
                    //            throw;
                    //        }
                    //    }
                    //    #endregion

                    //    #region Get Request    
                    //    //stream response data

                    //    using (Stream responseStream = response.GetResponseStream())
                    //    {
                    //        if (responseStream != null)
                    //        {
                    //            using (MemoryStream streamToWriteTo = new MemoryStream())
                    //            {
                    //                await responseStream.CopyToAsync(streamToWriteTo);
                    //                output = streamToWriteTo.ToArray();
                    //                //output = (T)Convert.ChangeType(streamToWriteTo.ToArray(), typeof(T));
                    //            }
                    //        }
                    //    }
                    //    return (output, response.StatusCode);
                    //    #endregion
                    //}
                    #endregion
                }
                #endregion
                #region - Post 
                if (httpMethod == httpVerb.POST && postDataPackage != default)
                {
                    //using (HttpClient client = new HttpClient(request))
                    using (HttpClient client = new HttpClient(request))
                    {
                        using (MultipartFormDataContent content = new MultipartFormDataContent())
                        {
                            //var fileContent = new ByteArrayContent(System.IO.File.ReadAllBytes(FileName));
                            var fileContent = new ByteArrayContent(postDataPackage);
                            //fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                            //{
                            //    FileName = Path.GetFileName(FileName)
                            //};

                            //Auth. Insure if there is a better way currently
                            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                            content.Add(fileContent);


                            using (HttpResponseMessage result = client.PostAsync(endPoint, content).Result)
                            {
                                return (output, result.StatusCode);
                            }

                        }
                    }

                    //using (Stream swJSONPayload = new MemoryStream(request.GetRequestStream()))
                    //{
                    //    swJSONPayload.Write(postJSON);
                    //    swJSONPayload.Close();
                    //    #region Post Response
                    //    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    //    {
                    //        output = new StreamReader(response.GetResponseStream()).ReadToEnd();
                    //        return (output, response.StatusCode);
                    //    }
                    //    #endregion
                    //}
                }
                #endregion
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                //_log.LogWarning(ex.Message);
                if (ex.Message == "The remote server returned an error: (401) Unauthorized.")
                {
                    //token = _tokenHandler.GetToken();
                }
                //throw;
            }
            return (default, HttpStatusCode.InternalServerError);

            //return output;
            //return (T)strResponseValue;
        }
        #endregion
        #region Multipart form Request - only used for uploading large files back to API (Recordings for the most part)
        public async Task<(string output, HttpStatusCode statusCode)> MakeMultipartFormRequest(string FileName)
        {
            string output = string.Empty;
            //bool rslt = false;
            try
            {
                //#region await async
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
                HttpClientHandler request = new HttpClientHandler();
                //request = await AttachNeededHeaders(request);
                #region - Get - not used
                //if (httpMethod == httpVerb.GET)
                //{
                //    using (HttpClient client = new HttpClient(request))
                //    {
                //        try
                //        {
                //            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                //            using (HttpResponseMessage response = await client.GetAsync(endPoint, HttpCompletionOption.ResponseHeadersRead))
                //            //using (HttpWebResponse response = (HttpWebResponse)request.GetResponse()) //await client.GetAsync(endPoint, HttpCompletionOption.ResponseHeadersRead))
                //            {
                //                if (response.StatusCode != HttpStatusCode.OK)
                //                {
                //                    try
                //                    {
                //                        return (output, response.StatusCode);
                //                    }
                //                    catch (Exception ex)
                //                    {
                //                        throw;
                //                    }
                //                }


                //                using (Stream responseStream = await response.Content.ReadAsStreamAsync())//.GetResponseStream())
                //                //using (Stream responseStream = response.GetResponseStream())
                //                {
                //                    if (responseStream != null)
                //                    {
                //                        using (MemoryStream streamToWriteTo = new MemoryStream())
                //                        {
                //                            //await streamToWriteTo.LoadIntoStream(responseStream);
                //                            //responseStream.Position = 0;
                //                            //responseStream.CopyTo(streamToWriteTo);

                //                            await responseStream.CopyToAsync(streamToWriteTo);
                //                            output = streamToWriteTo.ToArray();
                //                            //output = (T)Convert.ChangeType(streamToWriteTo.ToArray(), typeof(T));
                //                        }
                //                    }
                //                }
                //                return (output, response.StatusCode);
                //            }
                //            //return (output, response.StatusCode);
                //        }
                //        catch (Exception e)
                //        {
                //            _log.LogError(e.Message);

                //        }
                //    }
                //}
                #endregion
                #region - Post 
                if (httpMethod == httpVerb.POST && FileName != default)
                {
                    //using (HttpClient client = new HttpClient(request))
                    using (HttpClient client = new HttpClient(request))
                    {
                        using (MultipartFormDataContent content = new MultipartFormDataContent())
                        {
                            var fileContent = new ByteArrayContent(System.IO.File.ReadAllBytes(FileName));
                            fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")//have to make sure this is form data. 
                            {
                                FileName = Path.GetFileName(FileName)
                            };

                            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                            content.Add(fileContent);

                            using (HttpResponseMessage response = client.PostAsync(endPoint, content).Result)
                            {
                                output = new StreamReader(response.Content.ReadAsStreamAsync().Result).ReadToEnd();
                                return (output, response.StatusCode);
                            }
                            //using (HttpResponseMessage result = client.PostAsync(endPoint, content).Result)
                            //{
                            //    return (output, result.StatusCode);
                            //}
                            #region Post Response
                            //using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                            //{
                            //    output = new StreamReader(response.GetResponseStream()).ReadToEnd();
                            //    return (output, response.StatusCode);
                            //}
                            #endregion

                        }
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            return (default, HttpStatusCode.InternalServerError);
        }
        #endregion

        #endregion

        #region Generic Helpers
        public async Task<WebRequest> AttachNeededHeaders(WebRequest input)
        {
            try
            {
                switch (authType)
                {
                    case Authenticationtype.Basic:
                        {

                        }
                        break;
                    case Authenticationtype.BearerToken:
                        {
                            input.Headers.Add("Authorization", "Bearer " + token);
                        }
                        break;
                    //case Authenticationtype.Cookie:
                    //    {
                    //        Cookie thinMint = new Cookie("Febris.AuthCookie", snickerDoodle);
                    //        input = await TryAddCookie(input, thinMint);
                    //        if (input == null)
                    //        {
                    //            return null;
                    //        }
                    //    }
                    //    break;
                    default:
                        return null;
                        break;

                }
                return input;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        private async Task<bool> BadRequestHandler()
        {
            bool successful = false;
            try
            {

                return successful;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task<WebRequest> TryAddCookie(WebRequest webRequest, Cookie cookie)
        {
            try
            {
                HttpWebRequest httpRequest = webRequest as HttpWebRequest;
                if (httpRequest == null)
                {
                    return null;
                }

                if (httpRequest.CookieContainer == null)
                {
                    httpRequest.CookieContainer = new CookieContainer();
                }
                Uri site = new Uri(endPoint);
                httpRequest.CookieContainer.Add(site, cookie);
                return httpRequest;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task<WebRequest> TryAddCookie(WebRequest webRequest, CookieContainer cookieJar)
        {
            try
            {

                HttpWebRequest httpRequest = webRequest as HttpWebRequest;
                if (httpRequest == null)
                {
                    return null;
                }
                if (httpRequest.CookieContainer == null)
                {
                    httpRequest.CookieContainer = new CookieContainer();
                }

                httpRequest.CookieContainer = cookieJar;
                return httpRequest;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        #endregion
    }


}
#region - combined and not working
//using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
//{
//    #region Response Check
//    if (response.StatusCode != HttpStatusCode.OK)
//    {
//        try
//        {
//            return (output, response.StatusCode);
//            //bool successful = await BadRequestHandler();
//            //if (successful)
//            //{

//            //}

//        }
//        catch (Exception ex)
//        {
//            throw;
//        }
//    }
//    #endregion

//    #region Get Request    
//    //stream response data
//    if (request.Method == httpVerb.GET.ToString())
//    {
//        using (Stream responseStream = response.GetResponseStream())
//        {
//            if (responseStream != null)
//            {
//                using (StreamReader streamToWriteTo = new StreamReader(responseStream))
//                {
//                    output = await streamToWriteTo.ReadToEndAsync();
//                    //using (StreamReader reader = new StreamReader(responseStream))
//                    //{
//                    //    strResponseValue = reader.ReadToEnd();
//                    //}
//                }
//            }
//        }
//    }
//    #endregion

//    #region Post Request
//    if (request.Method == httpVerb.POST.ToString() && postJSON != string.Empty)
//    {
//        //request.ContentType = "application/json";
//        using (StreamWriter swJSONPayload = new StreamWriter(request.GetRequestStream()))
//        {
//            swJSONPayload.Write(postJSON);
//            swJSONPayload.Close();
//        }

//    }
//    return (output, response.StatusCode);
//    #endregion

//}
#endregion
#region not used
//        public async Task<(string output, HttpStatusCode statusCode)> MakeRequest()
//        {
//            //await AlterEndpoint();
//            //###############################################################################################################################
//            //Utilites.UniqueIdentifier.SetHardwareLicense();

//            string strResponseValue = string.Empty;

//            try
//            {
//                //HttpWebRequest request = (HttpWebRequest)WebRequest.Create(endPoint);//HttpWebRequest is being retired
//                WebRequest request = WebRequest.Create(endPoint);
//                request = await AttachNeededHeaders(request);
//                request.Method = httpMethod.ToString();


//                #region Authenticaiton and headers
//                //############################################################################################################################
//                //add headers
//                //  1) used basic for token request
//                //  2) use token for everything else
//                //        Add authentication here. https://www.youtube.com/watch?v=XX5pn4pJ4b0&list=PLpSmZmoBaROZNRmR3BHPHY6cqNOLqLkKA&index=2
//                // ############################################################################################################################
//                string userName = string.Empty;
//                string secret = string.Empty;
//                bool exists = false;
//                string authHeader = string.Empty;
//                if (authType == Authenticationtype.BearerToken)
//                {
//                    request.Headers.Add("Authorization", "Bearer " + token);
//                }
//                else if (authType == Authenticationtype.License)
//                {
//                    authHeader = System.Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(license));
//                    request.Headers.Add("Authorization", authType.ToString() + " " + authHeader);
//                }
//                else if (authType == Authenticationtype.Basic)
//                {
//                    authHeader = System.Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(userName + ":" + secret));
//                    request.Headers.Add("Authorization", authType.ToString() + " " + authHeader);
//                }
//                else if (authType == Authenticationtype.Cookie)
//                {
//                    //CookieContainer cookieJar = new CookieContainer();
//                    Cookie thinMint = new Cookie("Febris.AuthCookie", snickerDoodle);
//                    //Console.WriteLine("*****Gathered cookie to be attached" + thinMint + "*****");
//                    request = await TryAddCookie(request, thinMint);
//                    //cookieJar.SetCookies(thinMint);
//                    //request = await TryAddCookie(request, cookieJar);
//                    if (request == null)
//                    {
//                       // return ("The cookies crumbled and could not be delivered");
//                    }
//                }
//                else
//                {
//                    //return "Header Building Failed";
//                }


//                #region not used right now but is  normaly used for hardware filter
//                ///can use this for filtering out who is making the requests. 
//                ///the id or uuid of the license

//                //if (hardwareLicense != string.Empty && hardwareLicense != null && authType != Authenticationtype.Basic)
//                //if (authType != Authenticationtype.Basic)
//                //{
//                //    //hardwareLicense = Utilites.UniqueIdentifier.GetStoredUniqueIdentifier();
//                //    //hardwareLicense = _uniqueIdentifier.GetStoredUniqueIdentifier();
//                //    ////request.Headers.Add("hardwareLicense", hardwareLicense);
//                //    //request.Headers.Add("hardwareLicense", hardwareLicense);
//                //}
//                //############################################################################################################################
//                #endregion
//                #endregion

//                #region Post Request
//                if (request.Method == httpVerb.POST.ToString() && postJSON != string.Empty)
//                {
//                    request.ContentType = "application/json";
//                    using (StreamWriter swJSONPayload = new StreamWriter(request.GetRequestStream()))
//                    {
//                        swJSONPayload.Write(postJSON);

//                        swJSONPayload.Close();
//                    }
//                }
//                #endregion

//                #region Put Request
//                if (request.Method == httpVerb.PUT.ToString() && postJSON != string.Empty)
//                {
//                    request.ContentType = "application/json";
//                    using (StreamWriter swJSONPayload = new StreamWriter(request.GetRequestStream()))
//                    {
//                        swJSONPayload.Write(postJSON);

//                        swJSONPayload.Close();
//                    }
//                }
//                #endregion

//                #region Get Request                
//                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
//                {
//                    if (response.StatusCode != HttpStatusCode.OK)
//                    {
//                        //Try to get new token if it fails initally. 
//                        try
//                        {
//                            //return response.StatusCode.ToString();
//                        }
//                        catch (Exception ex)
//                        {
//                            _log.LogError(ex.Message);
//                            //return "Could Not Find Anything Here";
//                            throw;

//                            //new ApplicationException("error code: " + response.StatusCode.ToString());
//                        }
//                    }

//                    //stream response data
//                    using (Stream responseStream = response.GetResponseStream())
//                    {
//                        if (responseStream != null)
//                        {
//                            using (StreamReader reader = new StreamReader(responseStream))
//                            {
//                                strResponseValue = reader.ReadToEnd();
//                            }
//                        }
//                    }
//                }
//                #endregion

//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine(ex.StackTrace);
//                //_log.LogWarning(ex.Message);
//                if (ex.Message == "The remote server returned an error: (401) Unauthorized.")
//                {
//                    //token = _tokenHandler.GetToken();
//                }
//            }

//            //return strResponseValue;
//            return (string.Empty, HttpStatusCode.OK);
//        }
#endregion
#region constructors

//public APIRequest()
//{

//}
//public APIRequest(ILogger log)
//{
//    _log = log;
//    //_tokenHandler = new TokenHandler(_log);
//    //_uniqueIdentifier = new Utilites.UniqueIdentifier(_log);
//    //_dataProtection = new DataProtection(_log);
//    //_configSettings = new Service.ConfigSettings(_log, _config);
//}
//public APIRequest(ILogger log, IConfiguration config)
//{
//    _log = log;
//    _config = config;
//    //_tokenHandler = new TokenHandler(_log, _config);
//    //_uniqueIdentifier = new Utilites.UniqueIdentifier(_log, _config);
//    //_dataProtection = new DataProtection(_log, _config);
//    //_configSettings = new Service.ConfigSettings(_log, _config);
//}

//public APIRequest(ILogger log)
//{
//    _log = log;
//    _tokenHandler = new TokenHandler(_log);
//    _uniqueIdentifier = new Utilites.UniqueIdentifier(_log);
//    _dataProtection = new DataProtection(_log);
//    _configSettings = new Service.ConfigSettings(_log, _config);
//}

//public APIRequest(ILogger log, IConfiguration config)
//{
//    _log = log;
//    _config = config;
//    _tokenHandler = new TokenHandler(_log, _config);
//    _uniqueIdentifier = new Utilites.UniqueIdentifier(_log, _config);
//    _dataProtection = new DataProtection(_log, _config);
//    _configSettings = new Service.ConfigSettings(_log, _config);
//}
#endregion