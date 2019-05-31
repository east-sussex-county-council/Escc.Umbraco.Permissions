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

                var loginRequest = new LoginRequest { Username = _configuration["Escc:Umbraco:Permissions:UmbracoUsername"], Password = _configuration["Escc:Umbraco:Permissions:UmbracoPassword"] };
                var loginRequestJson = JsonConvert.SerializeObject(loginRequest);
                var content = new StringContent(loginRequestJson, Encoding.UTF8, "application/json");
                _ = await _httpClient.PostAsync(_configuration["Escc:Umbraco:Permissions:UmbracoBaseUrl"]?.TrimEnd('/') + "/umbraco/backoffice/UmbracoApi/Authentication/PostLogin", content);

                var json = await _httpClient.GetStringAsync(_configuration["Escc:Umbraco:Permissions:UmbracoBaseUrl"]?.TrimEnd('/') + "/umbraco/backoffice/api/permissions/forpage?url=" + HttpUtility.UrlEncode(url));
                model.Page = JsonConvert.DeserializeObject<Page>(json);

                if (model.Page != null)
                {
                    model.Page.WebTeamEmail = _configuration["Escc:Umbraco:Permissions:WebTeamEmail"];

                    if (model.Page.LastEditedBy != null)
                    {
                        var hiddenGroups = _configuration["Escc:Umbraco:Permissions:HideUsersInTheseGroups"]?.Split(',');
                        if (hiddenGroups != null)
                        {
                            foreach (var group in hiddenGroups)
                            {
                                if (model.Page.LastEditedBy.GroupNames.Contains(group))
                                {
                                    model.Page.LastEditedBy = null;
                                    break;
                                }
                            }
                        }

                        if (model.Page.LastEditedBy != null)
                        {
                            model.Page.LastEditedBy.UserProfileUrl = new Uri(string.Format(CultureInfo.InvariantCulture, _configuration["Escc:Umbraco:Permissions:UserProfileUrl"], model.Page.LastEditedBy.Username), UriKind.RelativeOrAbsolute);
                        }
                    }
                    foreach (var user in model.Page.UsersWithPermissions)
                    {
                        user.UserProfileUrl = new Uri(string.Format(CultureInfo.InvariantCulture, _configuration["Escc:Umbraco:Permissions:UserProfileUrl"], user.Username), UriKind.RelativeOrAbsolute);
                    }
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
    }
}
