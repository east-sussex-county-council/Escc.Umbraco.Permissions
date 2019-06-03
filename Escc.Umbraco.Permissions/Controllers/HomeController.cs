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
using Escc.EastSussexGovUK.Core;
using Exceptionless;

namespace Escc.Umbraco.Permissions.Controllers
{
    public class HomeController : Controller
    {
        private static HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IEastSussexGovUKTemplateRequest _templateRequest;
        private readonly IViewModelDefaultValuesProvider _defaultModelValues;

        public HomeController(IConfiguration configuration, IEastSussexGovUKTemplateRequest templateRequest, IViewModelDefaultValuesProvider defaultModelValues)
        {
            _configuration = configuration;
            _templateRequest = templateRequest;
            _defaultModelValues = defaultModelValues;
        }

        [HttpGet]
        [Route("")]
        public async Task<IActionResult> Index()
        {
            var model = new PermissionsViewModel(_defaultModelValues);

            try
            {
                // Do this to load the template controls.
                model.TemplateHtml = await _templateRequest.RequestTemplateHtmlAsync();
            }
            catch (Exception ex)
            {
                // Catch and report exceptions - don't throw them and cause the page to fail
                ex.ToExceptionless().Submit();
            }

            return View(model);
        }

        class LoginRequest
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }

        [HttpGet]
        [Route("WhoIsResponsible")]
        public async Task<IActionResult> WhoIsResponsible(string url)
        {
            var model = new PermissionsViewModel(_defaultModelValues);
            if (url != null)
            {
                if (_httpClient == null)
                {
                    _httpClient = new HttpClient(new HttpClientHandler());
                }

                var permissionsApiUrl = _configuration["Escc:Umbraco:Permissions:PermissionsApiBaseUrl"]?.TrimEnd('/');
                var publishedContentUrl = _configuration["Escc:Umbraco:Permissions:PublishedContentBaseUrl"]?.TrimEnd('/');

                var loginRequest = new LoginRequest { Username = _configuration["Escc:Umbraco:Permissions:UmbracoUsername"], Password = _configuration["Escc:Umbraco:Permissions:UmbracoPassword"] };
                var loginRequestJson = JsonConvert.SerializeObject(loginRequest);
                var content = new StringContent(loginRequestJson, Encoding.UTF8, "application/json");
                _ = await _httpClient.PostAsync(permissionsApiUrl + "/umbraco/backoffice/UmbracoApi/Authentication/PostLogin", content);

                var json = await _httpClient.GetStringAsync(permissionsApiUrl + "/umbraco/backoffice/api/permissions/forpage?url=" + HttpUtility.UrlEncode(url));
                model.Page = JsonConvert.DeserializeObject<Page>(json);

                if (model.Page != null)
                {
                    if (model.Page.Url != null && model.Page.Url.IsAbsoluteUri && !String.IsNullOrEmpty(publishedContentUrl))
                    {
                        model.Page.Url = new Uri(model.Page.Url.ToString().Replace(permissionsApiUrl, publishedContentUrl));
                    }
                    model.Page.WebTeamEmail = _configuration["Escc:Umbraco:Permissions:WebTeamEmail"];

                    if (model.Page.LastEditedBy != null)
                    {
                        if (UserIsInHiddenGroup(model.Page.LastEditedBy))
                        {
                            model.Page.LastEditedBy = null;
                        }

                        if (model.Page.LastEditedBy != null)
                        {
                            model.Page.LastEditedBy.UserProfileUrl = new Uri(string.Format(CultureInfo.InvariantCulture, _configuration["Escc:Umbraco:Permissions:UserProfileUrl"], model.Page.LastEditedBy.Username), UriKind.RelativeOrAbsolute);
                        }
                    }
                    var usersToShow = new List<User>();
                    foreach (var user in model.Page.UsersWithPermissions)
                    {
                        if (!UserIsInHiddenGroup(user))
                        {
                            user.UserProfileUrl = new Uri(string.Format(CultureInfo.InvariantCulture, _configuration["Escc:Umbraco:Permissions:UserProfileUrl"], user.Username), UriKind.RelativeOrAbsolute);
                            usersToShow.Add(user);
                        }
                    }
                    model.Page.UsersWithPermissions = usersToShow;
                }
            }

            try
            {
                // Do this to load the template controls.
                model.TemplateHtml = await _templateRequest.RequestTemplateHtmlAsync();
            }
            catch (Exception ex)
            {
                // Catch and report exceptions - don't throw them and cause the page to fail
                ex.ToExceptionless().Submit();
            }

            return View("Index", model);
        }

        private bool UserIsInHiddenGroup(User user)
        {
            var hiddenGroups = _configuration["Escc:Umbraco:Permissions:HideUsersInTheseGroups"]?.Split(',');
            if (hiddenGroups != null)
            {
                foreach (var group in hiddenGroups)
                {
                    if (user.GroupNames.Contains(group))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
