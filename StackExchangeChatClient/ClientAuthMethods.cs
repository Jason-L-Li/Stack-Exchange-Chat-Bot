﻿using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace StackExchangeChatClient
{
    public partial class Client
    {
        private void SignIn(string username, string password)
        {
            HtmlDocument htmlDocument = new HtmlDocument();
            var rawLoginPage = GetLoginPage().Result;

            htmlDocument.LoadHtml(rawLoginPage);
            FKey = htmlDocument.DocumentNode.SelectSingleNode("//input[@name='fkey']").Attributes["value"].Value.Trim();

            var rawLoggedInPage = SignInToOpenIdEndPoint(username, password).Result;
            var signInLink = GetSignInViaOpenIdLink().Result;
            var authTokenPage = GetOpenIdAuthTokenPage(signInLink).Result;

            htmlDocument.LoadHtml(authTokenPage);
            var redirectLink = htmlDocument.DocumentNode.SelectSingleNode("//a").Attributes["href"].Value.Trim();
            var stackExchangePage = string.Empty;
            try
            {
                stackExchangePage = SignInToStackExchange(redirectLink).Result;
            }
            catch
            {
                System.Threading.Thread.Sleep(1000);
                stackExchangePage = SignInToStackExchange(redirectLink).Result;
            }            

            var chatSignInPage = GetChatSignInPage().Result;

            htmlDocument.LoadHtml(chatSignInPage);
            var url = htmlDocument.DocumentNode.SelectSingleNode("//form[@id='chat-login-form']").Attributes["action"].Value.Trim();
            var token = htmlDocument.DocumentNode.SelectSingleNode("//input[@id='authToken']").Attributes["value"].Value.Trim();
            var nonce = htmlDocument.DocumentNode.SelectSingleNode("//input[@name='nonce']").Attributes["value"].Value.Trim();

            var chatRoomList = SignInToChat(url, token, nonce).Result;

            var favoriteRoomsPage = GetFavoriteRooms().Result;
            htmlDocument.LoadHtml(favoriteRoomsPage);
            FKey = htmlDocument.DocumentNode.SelectSingleNode("//input[@name='fkey']").Attributes["value"].Value.Trim();
        }

        /// <summary>
        /// Need to hit stack exchange login page which contains the fkey
        /// </summary>
        /// <returns></returns>
        private async Task<string> GetLoginPage()
        {
            using (var handler = new HttpClientHandler() { CookieContainer = this.CookieContainer })
            using (var client = new HttpClient(handler))
            {
                client.BaseAddress = new Uri("https://openid.stackexchange.com/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                HttpResponseMessage response = await client.GetAsync("account/login");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    throw new Exception("ERROR getting RawFKey, status: " + response.StatusCode.ToString());
                }
            }
        }

        private async Task<string> SignInToOpenIdEndPoint(string email, string password)
        {
            using (var handler = new HttpClientHandler() { CookieContainer = this.CookieContainer })
            using (var client = new HttpClient(handler))
            {
                client.BaseAddress = new Uri("https://openid.stackexchange.com/");
                var content = new FormUrlEncodedContent(new[] 
                {
                    new KeyValuePair<string, string>("fkey", FKey),
                    new KeyValuePair<string, string>("email", email),
                    new KeyValuePair<string, string>("password", password)
                });

                HttpResponseMessage response = await client.PostAsync("account/login/submit", content);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    throw new Exception("ERROR logging in, status: " + response.StatusCode.ToString());
                }
            }
        }

        private async Task<string> GetSignInViaOpenIdLink()
        {
            using (var handler = new HttpClientHandler() { CookieContainer = this.CookieContainer })
            using (var client = new HttpClient(handler))
            {
                client.BaseAddress = new Uri("https://stackexchange.com/");
                var content = new FormUrlEncodedContent(new[] 
                {
                    new KeyValuePair<string, string>("from", "https://stackexchange.com/users/login#log-in")
                });

                HttpResponseMessage response = await client.PostAsync("users/signin", content);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    throw new Exception("ERROR getting open id sign in link, status: " + response.StatusCode.ToString());
                }
            }
        }

        private async Task<string> GetOpenIdAuthTokenPage(string signInLink)
        {
            using (var handler = new HttpClientHandler() { CookieContainer = this.CookieContainer })
            using (var client = new HttpClient(handler))
            {
                client.BaseAddress = new Uri(signInLink);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                HttpResponseMessage response = await client.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    throw new Exception("ERROR getting open id auth token, status: " + response.StatusCode.ToString());
                }
            }
        }

        private async Task<string> SignInToStackExchange(string signInLink)
        {
            using (var handler = new HttpClientHandler() { CookieContainer = this.CookieContainer })
            using (var client = new HttpClient(handler))
            {
                client.BaseAddress = new Uri(signInLink);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                HttpResponseMessage response = await client.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    throw new Exception("ERROR signing in to Stack Exchange, status: " + response.StatusCode.ToString());
                }
            }
        }

        /// <summary>
        /// Gets the chat Sign In page which contains the url, auth token, and nonce required for login
        /// </summary>
        /// <returns></returns>
        private async Task<string> GetChatSignInPage()
        {
            using (var handler = new HttpClientHandler() { CookieContainer = this.CookieContainer })
            using (var client = new HttpClient(handler))
            {
                client.BaseAddress = new Uri("http://stackexchange.com/users/chat-login");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                HttpResponseMessage response = await client.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    throw new Exception("ERROR getting the chat sign in page, status: " + response.StatusCode.ToString());
                }
            }
        }

        private async Task<string> SignInToChat(string signInUrl, string authToken, string nonce)
        {
            using (var handler = new HttpClientHandler() { CookieContainer = this.CookieContainer })
            using (var client = new HttpClient(handler))
            {
                client.BaseAddress = new Uri(signInUrl);
                client.DefaultRequestHeaders.Add("Referer", "http://stackexchange.com/users/chat-login");
                var content = new FormUrlEncodedContent(new[] 
                {
                    new KeyValuePair<string, string>("authToken", authToken),
                    new KeyValuePair<string, string>("nonce", nonce)
                });

                HttpResponseMessage response = await client.PostAsync("", content);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    throw new Exception("ERROR signing into chat, status: " + response.StatusCode.ToString());
                }
            }
        }

        /// <summary>
        /// Need to hit stack exchange chat favorites page to refresh the fkey
        /// </summary>
        /// <returns></returns>
        private async Task<string> GetFavoriteRooms()
        {
            using (var handler = new HttpClientHandler() { CookieContainer = this.CookieContainer })
            using (var client = new HttpClient(handler))
            {
                client.BaseAddress = new Uri("http://chat.stackexchange.com/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                HttpResponseMessage response = await client.GetAsync("chats/join/favorite");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    throw new Exception("ERROR getting the Favorite Rooms, status: " + response.StatusCode.ToString());
                }
            }
        }
    }
}