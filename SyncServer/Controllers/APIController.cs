using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SyncServer.Classes;
using SyncServer.Models;

namespace SyncServer.Controllers
{
	public class APIController : Controller
	{
		private readonly ILogger<HomeController> _logger;
		private readonly Auth _auth;

		public APIController(ILogger<HomeController> logger, Auth auth)
		{
			_logger = logger;
			_auth = auth;
		}

		public IActionResult Login(string email, string pwd)
		{
			_auth.Login(HttpContext, email, pwd);
			return null;
		}

		public string ListOutdated(string filelist)
		{
			return null;
		}
	}
}