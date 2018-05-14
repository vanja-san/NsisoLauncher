﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NsisoLauncher.Core.Net.MojangApi.Api
{

    /// <summary>
    /// 请求者class, 处理所有的请求.
    /// </summary>
    public static class Requester
    {

        /// <summary>
        /// 定义http请求的超时时间.
        /// </summary>
        public static TimeSpan Timeout = TimeSpan.FromSeconds(5);

        /// <summary>
        /// 定义读取响应和写入请求的编码.
        /// </summary>
        public static Encoding Encoding = Encoding.Default;

        /// <summary>
        /// NsisoLauncher目前名称.
        /// </summary>
        public readonly static string Name = "NsisoLauncher";

        /// <summary>
        /// NsisoLauncher目前版本号.
        /// </summary>
        public readonly static string Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

        /// <summary>
        /// 代表请求者实例的UUID.
        /// </summary>
        public static string ClientToken
        {
            get
            {
                if (_clientToken == null)
                    _clientToken = Guid.NewGuid().ToString();
                return _clientToken;
            }
            set { _clientToken = value; }
        }
        private static string _clientToken;

        /// <summary>
        /// 表示Web请求中使用的http客户端.
        /// </summary>
        internal static HttpClient Client
        {
            get
            {
                if (_client == null)
                    _client = new HttpClient() { Timeout = Requester.Timeout };
                return _client;
            }
            private set
            {
                _client = value;
            }
        }
        private static HttpClient _client;

        /// <summary>
        /// 向给定endpoint发送GET请求.
        /// </summary>
        /// <typeparam name="T">返回响应的类型</typeparam>
        internal async static Task<Response> Get<T>(IEndpoint<T> endpoint, bool authenticate = false)
        {
            if (endpoint == null)
                throw new ArgumentNullException("Endpoint", "Endpoint should not be null.");

            HttpResponseMessage httpResponse = null;
            Error error = null;
            string rawMessage = null;

            try
            {
                // 如果给予了令牌
                if (authenticate && endpoint.Arguments.Count > 0)
                {
                    //application/x-www-form-urlencoded
                    Requester.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", endpoint.Arguments[0]);
                    Requester.Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
                    Requester.Client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(Requester.Name, Requester.Version));
                }

                httpResponse = await Requester.Client.GetAsync(endpoint.Address);
                rawMessage = await httpResponse.Content.ReadAsStringAsync();
                httpResponse.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                error = new Error()
                {
                    ErrorMessage = ex.Message,
                    ErrorTag = ex.Message,
                    Exception = ex
                };
            }
            return new Response()
            {
                Code = httpResponse.StatusCode,
                RawMessage = rawMessage,
                IsSuccess = httpResponse.IsSuccessStatusCode && (
                            httpResponse.StatusCode == HttpStatusCode.Accepted ||
                            httpResponse.StatusCode == HttpStatusCode.Continue ||
                            httpResponse.StatusCode == HttpStatusCode.Created ||
                            httpResponse.StatusCode == HttpStatusCode.Found ||
                            httpResponse.StatusCode == HttpStatusCode.OK ||
                            httpResponse.StatusCode == HttpStatusCode.PartialContent ||
                            httpResponse.StatusCode == HttpStatusCode.NoContent) &&
                            error == null,
                Error = error
            };

        }

        /// <summary>
        /// 向给定endpoint发送POST请求.
        /// </summary>
        /// <typeparam name="T">返回响应的类型</typeparam>
        internal async static Task<Response> Post<T>(IEndpoint<T> endpoint)
        {
            if (endpoint == null)
                throw new ArgumentNullException("Endpoint", "Endpoint should not be null.");

            if (endpoint.PostContent == null)
                throw new ArgumentNullException("PostContent", "PostContent should not be null.");

            HttpResponseMessage httpResponse = null;
            Error error = null;
            string rawMessage = null;

            try
            {
                StringContent contents = new StringContent(endpoint.PostContent, Requester.Encoding, "application/json");
                httpResponse = await Requester.Client.PostAsync(endpoint.Address, contents);
                rawMessage = await httpResponse.Content.ReadAsStringAsync();
                httpResponse.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                error = new Error()
                {
                    ErrorMessage = ex.Message,
                    ErrorTag = ex.Message,
                    Exception = ex
                };
            }

            return new Response()
            {
                Code = httpResponse.StatusCode,
                RawMessage = rawMessage,
                IsSuccess = httpResponse.IsSuccessStatusCode && (
                            httpResponse.StatusCode == HttpStatusCode.Accepted ||
                            httpResponse.StatusCode == HttpStatusCode.Continue ||
                            httpResponse.StatusCode == HttpStatusCode.Created ||
                            httpResponse.StatusCode == HttpStatusCode.Found ||
                            httpResponse.StatusCode == HttpStatusCode.OK ||
                            httpResponse.StatusCode == HttpStatusCode.PartialContent) &&
                            error == null,
                Error = error
            };
        }

        /// <summary>
        /// 向给定endpoint发送POST请求.
        /// </summary>
        internal async static Task<Response> Post<T>(IEndpoint<T> endpoint, Dictionary<string, string> toEncode)
        {
            if (endpoint == null)
                throw new ArgumentNullException("Endpoint", "Endpoint should not be null.");

            if (toEncode == null || toEncode.Count < 1)
                throw new ArgumentNullException("PostContent", "PostContent should not be null.");


            HttpResponseMessage httpResponse = null;
            Error error = null;
            string rawMessage = null;

            try
            {
                //application/x-www-form-urlencoded
                Requester.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", endpoint.Arguments[0]);
                Requester.Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
                Requester.Client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(Requester.Name, Requester.Version));

                httpResponse = await Requester.Client.PostAsync(endpoint.Address, new FormUrlEncodedContent(toEncode));
                rawMessage = await httpResponse.Content.ReadAsStringAsync();
                httpResponse.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                if (httpResponse.StatusCode == HttpStatusCode.Unauthorized ||
                    httpResponse.StatusCode == HttpStatusCode.Forbidden)
                {
                    JObject err = JObject.Parse(rawMessage);
                    error = new Error()
                    {
                        ErrorMessage = err["errorMessage"].ToObject<string>(),
                        ErrorTag = err["error"].ToObject<string>(),
                        Exception = ex
                    };
                }
                else
                {
                    error = new Error()
                    {
                        ErrorMessage = ex.Message,
                        ErrorTag = ex.GetBaseException().Message,
                        Exception = ex
                    };
                }

            }

            return new Response()
            {
                Code = httpResponse.StatusCode,
                RawMessage = rawMessage,
                IsSuccess = httpResponse.IsSuccessStatusCode && (
                            httpResponse.StatusCode == HttpStatusCode.Accepted ||
                            httpResponse.StatusCode == HttpStatusCode.Continue ||
                            httpResponse.StatusCode == HttpStatusCode.Created ||
                            httpResponse.StatusCode == HttpStatusCode.Found ||
                            httpResponse.StatusCode == HttpStatusCode.OK ||
                            httpResponse.StatusCode == HttpStatusCode.PartialContent) &&
                            error == null,
                Error = error
            };
        }

        /// <summary>
        /// 向给定endpoint发送一个PUT请求.
        /// </summary>
        internal async static Task<Response> Put<T>(IEndpoint<T> endpoint, FileInfo file)
        {
            if (endpoint == null)
                throw new ArgumentNullException("Endpoint", "Endpoint should not be null.");

            if (file == null)
                throw new ArgumentNullException("Skin", "No file given.");

            if (!file.Exists)
                throw new ArgumentException("Given file does not exist.");


            HttpResponseMessage httpResponse = null;
            Error error = null;
            string rawMessage = null;

            try
            {
                //application/x-www-form-urlencoded
                Requester.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", endpoint.Arguments[0]);
                Requester.Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
                Requester.Client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(Requester.Name, Requester.Version));
                

                using (var contents = new MultipartFormDataContent())
                {
                    contents.Add(new StringContent(bool.Parse(endpoint.Arguments[1]) == true ? "true" : "false"), "model");
                    contents.Add(new ByteArrayContent(File.ReadAllBytes(file.FullName)), "file", file.Name);

                    httpResponse = await Requester.Client.PutAsync(endpoint.Address, contents);
                    rawMessage = await httpResponse.Content.ReadAsStringAsync();
                    httpResponse.EnsureSuccessStatusCode();
                }

            }
            catch (Exception ex)
            {
                if (httpResponse.StatusCode == HttpStatusCode.Unauthorized ||
                    httpResponse.StatusCode == HttpStatusCode.Forbidden)
                {
                    JObject err = JObject.Parse(rawMessage);
                    error = new Error()
                    {
                        ErrorMessage = err["errorMessage"].ToObject<string>(),
                        ErrorTag = err["error"].ToObject<string>(),
                        Exception = ex
                    };
                }
                else
                {
                    error = new Error()
                    {
                        ErrorMessage = ex.Message,
                        ErrorTag = ex.GetBaseException().Message,
                        Exception = ex
                    };
                }

            }

            return new Response()
            {
                Code = httpResponse.StatusCode,
                RawMessage = rawMessage,
                IsSuccess = httpResponse.IsSuccessStatusCode && (
                            httpResponse.StatusCode == HttpStatusCode.Accepted ||
                            httpResponse.StatusCode == HttpStatusCode.Continue ||
                            httpResponse.StatusCode == HttpStatusCode.Created ||
                            httpResponse.StatusCode == HttpStatusCode.Found ||
                            httpResponse.StatusCode == HttpStatusCode.OK ||
                            httpResponse.StatusCode == HttpStatusCode.PartialContent) &&
                            error == null,
                Error = error
            };
        }

        /// <summary>
        /// 向给定endpoint发送DELETE请求.
        /// </summary>
        internal async static Task<Response> Delete<T>(IEndpoint<T> endpoint)
        {
            if (endpoint == null)
                throw new ArgumentNullException("Endpoint", "Endpoint should not be null.");


            HttpResponseMessage httpResponse = null;
            Error error = null;
            string rawMessage = null;

            try
            {
                //application/x-www-form-urlencoded
                Requester.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", endpoint.Arguments[0]);
                Requester.Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
                Requester.Client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(Requester.Name, Requester.Version));

                httpResponse = await Requester.Client.DeleteAsync(endpoint.Address);
                rawMessage = await httpResponse.Content.ReadAsStringAsync();
                httpResponse.EnsureSuccessStatusCode();

            }
            catch (Exception ex)
            {
                if (httpResponse.StatusCode == HttpStatusCode.Unauthorized ||
                    httpResponse.StatusCode == HttpStatusCode.Forbidden)
                {
                    JObject err = JObject.Parse(rawMessage);
                    error = new Error()
                    {
                        ErrorMessage = err["errorMessage"].ToObject<string>(),
                        ErrorTag = err["error"].ToObject<string>(),
                        Exception = ex
                    };
                }
                else
                {
                    error = new Error()
                    {
                        ErrorMessage = ex.Message,
                        ErrorTag = ex.GetBaseException().Message,
                        Exception = ex
                    };
                }

            }

            return new Response()
            {
                Code = httpResponse.StatusCode,
                RawMessage = rawMessage,
                IsSuccess = httpResponse.IsSuccessStatusCode && (
                            httpResponse.StatusCode == HttpStatusCode.Accepted ||
                            httpResponse.StatusCode == HttpStatusCode.Continue ||
                            httpResponse.StatusCode == HttpStatusCode.Created ||
                            httpResponse.StatusCode == HttpStatusCode.Found ||
                            httpResponse.StatusCode == HttpStatusCode.OK ||
                            httpResponse.StatusCode == HttpStatusCode.PartialContent) &&
                            error == null,
                Error = error
            };
        }
    }
}
