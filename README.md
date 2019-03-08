# Escc.Umbraco.Permissions

This application provides a way for anyone to see who has responsibility for updating an Umbraco page, whether or not they have an Umbraco login themselves.

## Permissions API - an Umbraco NuGet package 

A .NET Framework permissions API is designed to be installed into an Umbraco installation as a NuGet package. It requires Umbraco 7.x and a minimum of Umbraco 7.7. It can provide information on a page based on its published URL or its back office edit URL.

## Who is responsible for this website page?

This ASP.NET Core application accepts a page URL and displays the result returned by the API above. It expects the following secrets to be defined:

	{
	  "Escc": {
	    "Umbraco": {
	      "Permissions": {
	        "UserProfileUrl": "https://hostname/{0}",
	        "UmbracoBaseUrl": "https://hostname/",
	        "UmbracoUsername": "user",
	        "UmbracoPassword": "password",
	        "HideUsersInTheseGroups": "Group1,Group2",
	        "WebTeamEmail": "email@example.org"
	      }
	    }
	  }
	}

* `UserProfileUrl` expects a URL to some other application where `{0}` is replaced by the user's Umbraco username.
* `Umbraco*` settings are for connecting to the Umbraco instance hosting the permissions API.
* `HideUsersInTheseGroups` expects a comma-separated list of Umbraco groups that should not be regarded as ordinary users, eg those containing administrators or automated processes.
* `WebTeamEmail` is the address queries should be sent to if no web authors are identified for a page.