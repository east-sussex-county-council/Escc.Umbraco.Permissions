using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Escc.Umbraco.Permissions.Models;
using System.Net.Http;
using System.Web;
using Newtonsoft.Json;
using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.Globalization;

namespace Escc.Umbraco.Permissions.Controllers
{
    public class HomeController : Controller
    {
        private static HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public HomeController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var model = new Page();
            return View(model);
        }

        class LoginRequest
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }

        [HttpGet]
        public async Task<ActionResult> WhoIsResponsible(string url)
        {
            var model = new Page
            {
                WebTeamEmail = _configuration["Escc:Umbraco:Permissions:WebTeamEmail"]
            };
            if (url != null)
            {
                if (_httpClient == null)
                {
                    _httpClient = new HttpClient(new HttpClientHandler());
                }

                var loginRequest = new LoginRequest { Username = _configuration["Escc:Umbraco:Permissions:UmbracoUsername"], Password = _configuration["Escc:Umbraco:Permissions:UmbracoPassword"] };
                var loginRequestJson = JsonConvert.SerializeObject(loginRequest);
                var content = new StringContent(loginRequestJson, Encoding.UTF8, "application/json");
                var loginResult = await _httpClient.PostAsync(_configuration["Escc:Umbraco:Permissions:UmbracoBaseUrl"]?.TrimEnd('/') + "/umbraco/backoffice/UmbracoApi/Authentication/PostLogin", content);

                var json = await _httpClient.GetStringAsync(_configuration["Escc:Umbraco:Permissions:UmbracoBaseUrl"]?.TrimEnd('/') + "/umbraco/backoffice/api/permissions/forpage?url=" + HttpUtility.UrlEncode(url));
                model = JsonConvert.DeserializeObject<Page>(json);

                if (model.LastEditedBy != null)
                {
                    var administratorGroups = _configuration["Escc:Umbraco:Permissions:HideUsersInTheseGroups"]?.Split(',');
                    if (administratorGroups != null)
                    {
                        foreach (var group in administratorGroups)
                        {
                            if (model.LastEditedBy.GroupNames.Contains(group))
                            {
                                model.LastEditedBy = null;
                                break;
                            }
                        }
                    }

                    if (model.LastEditedBy != null)
                    {
                        model.LastEditedBy.UserProfileUrl = new Uri(string.Format(CultureInfo.InvariantCulture, _configuration["Escc:Umbraco:Permissions:UserProfileUrl"], model.LastEditedBy.Username), UriKind.RelativeOrAbsolute);
                    }
                }
                foreach (var user in model.UsersWithPermissions)
                {
                    user.UserProfileUrl = new Uri(string.Format(CultureInfo.InvariantCulture, _configuration["Escc:Umbraco:Permissions:UserProfileUrl"], user.Username), UriKind.RelativeOrAbsolute);
                }
            }

            return View("Index", model);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
