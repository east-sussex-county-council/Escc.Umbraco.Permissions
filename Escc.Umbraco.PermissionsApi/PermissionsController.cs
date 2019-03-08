using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Web.Http;
using Escc.Umbraco.Permissions;
using System.Linq;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.WebApi;
using Umbraco.Core.Models.Membership;
using Umbraco.Web.PublishedCache;

namespace Escc.Umbraco.PermissionsApi
{
    /// <summary>
    /// Gets information about permissions for pages in Umbraco
    /// </summary>
    public class PermissionsController : UmbracoAuthorizedApiController
    {
        [HttpGet]
        public Page ForPage([FromUri] Uri url)
        {
            Page result = GetPageFromUrl(url, Services.UserService, Services.ContentService, UmbracoContext.ContentCache);

            if (result.NodeId > 0)
            {
                var permissionsReader = new UmbracoPermissionsReader(Services.UserService, Services.ContentService);
                result.UsersWithPermissions = GetUsersForPage(result.NodeId, permissionsReader, Services.UserService);
            }

            return result;
        }

        private IEnumerable<User> GetUsersForPage(int nodeId, IUmbracoPermissionsReader permissionsReader, IUserService userService)
        {
            var usersById = new Dictionary<int, User>();
            var groupIds = permissionsReader.GroupsWithPermissionForPage(nodeId, UmbracoPermission.UPDATE);
            foreach (var groupId in groupIds)
            {
                var users = permissionsReader.ActiveUsersInGroup(groupId);
                foreach (var user in users)
                {
                    if (!usersById.ContainsKey(user.Id))
                    {
                        usersById.Add(user.Id, GetUser(user.Id, userService));
                    }
                }
            }

            return usersById.Values;
        }

        private Page GetPageFromUrl(Uri url, IUserService userService, IContentService contentService, ContextualPublishedContentCache contentCache)
        {
            var result = new Page();
            var absoluteUrl = new Uri(Request.RequestUri, url);

            var match = Regex.Match(absoluteUrl.Fragment, "^#/content/content/edit/([0-9]+)$");
            if (match.Success)
            {
                var pageId = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                var page = contentService.GetById(pageId);
                if (page != null)
                {
                    result.NodeId = page.Id;
                    result.Name = page.Name;

                    result.LastEditedDate = page.UpdateDate;
                    result.LastEditedBy = GetUser(page.WriterId, userService);
                }
            }
            else
            {
                var page = contentCache.GetByRoute(absoluteUrl.AbsolutePath);
                if (page != null)
                {
                    result.NodeId = page.Id;
                    result.Url = new Uri(page.UrlAbsolute());
                    result.Name = page.Name;

                    result.LastEditedDate = page.UpdateDate;
                    result.LastEditedBy = GetUser(page.WriterId, userService);
                }
            }


            return result;
        }

        private User GetUser(int userId, IUserService userService)
        {
            var lastEditedBy = userService.GetUserById(userId);
            if (lastEditedBy != null)
            {
                return new User
                {
                    Name = lastEditedBy.Name,
                    Username = lastEditedBy.Username,
                    Email = lastEditedBy.Email,
                    GroupNames = lastEditedBy.Groups.Select(x => x.Name)
                };
            }
            return null;
        }
    }
}