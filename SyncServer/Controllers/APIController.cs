using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SyncServer.Classes;
using SyncServer.Models;
using Newtonsoft.Json;
using SyncServer.DAL;
using Azure.Storage.Blobs;
using System.IO;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using System.Globalization;

namespace SyncServer.Controllers
{
	public class APIController : Controller
	{
		private static readonly BlobContainerClient _containerClient;
		private readonly ILogger<HomeController> _logger;
		private readonly Auth _auth;
		private readonly SyncContext _db;

		static APIController()
		{
			BlobServiceClient blobServiceClient = new BlobServiceClient("DefaultEndpointsProtocol=https;AccountName=midistorage;AccountKey=s5CGWLkZVCDat5vYMz0ZBeHVzUaHEcsEGiLipnGdTTAFUVQn0VP1+xWZo5xwdWXZ8YXf9Whhx3q9EUWasqvV/Q==;EndpointSuffix=core.windows.net");
			string containerName = "designarsenal";
			_containerClient = blobServiceClient.GetBlobContainerClient(containerName);
		}

		public APIController(ILogger<HomeController> logger, Auth auth, SyncContext db)
		{
			_logger = logger;
			_auth = auth;
			_db = db;
		}

		public IActionResult Login(string email, string pwd)
		{
			_auth.Login(HttpContext, email, pwd);
			return null;
		}

		class SyncFile
		{
			public string Dt { get; set; }
			public string Path { get; set; }
			public DateTime DtParsed { get; set; }
		}

		class RetSyncFile
		{
			public string Path { get; set; }
			public bool Download { get; set; }// else upload
		}

		[HttpPost]
		public string ListOutdated(string dir)
		{
			string jsonfilelist;
			using(var reader = new StreamReader(HttpContext.Request.Body))
			{
				jsonfilelist = reader.ReadToEndAsync().Result;
			}

			var retlist = new List<RetSyncFile>();

			var list = JsonConvert.DeserializeObject<List<SyncFile>>(jsonfilelist);
			var t = _db.SyncList.ToList();
			foreach(var item in list)
			{
				item.DtParsed = DateTime.ParseExact(item.Dt, "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
				var model = _db.SyncList.FirstOrDefault(s => s.Dir == dir && s.Path == item.Path);
				if(model != null)
				{
					DateTime dt = item.DtParsed;
					if(model.Dt == dt)
						continue;
					retlist.Add(new RetSyncFile()
					{
						Path = item.Path,
						Download = model.Dt > dt
					});
				}
				else
				{
					retlist.Add(new RetSyncFile()
					{
						Path = item.Path,
						Download = false
					});
				}
			}

			return JsonConvert.SerializeObject(retlist);
		}

		public async Task<IActionResult> DownloadFile(string dir, string subpath)
		{
			var user = _auth.GetRequestUser(HttpContext);
			var path = user.Id + "/" + dir + "/" + subpath;

			BlobClient blob = _containerClient.GetBlobClient(path);
			BlobDownloadInfo download = await blob.DownloadAsync();

			using(MemoryStream downloadFileStream = new MemoryStream())
			{
				await download.Content.CopyToAsync(downloadFileStream);
				return File(downloadFileStream.ToArray(), "application/octet-stream");
			}
		}

		[HttpPost]
		public async Task<IActionResult> UploadFile(string dir, string subpath, string dt, long size)
		{
			var user = _auth.GetRequestUser(HttpContext);
			var path = user.Id + "/" + dir + "/" + subpath;
			var ddt = DateTime.ParseExact(dt, "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

			BlobClient blob = _containerClient.GetBlobClient(path);

			// Open the file and upload its data
			await blob.UploadAsync(Request.Body, true);
			var n = new SyncListModel
			{
				Dir = dir,
				Path = subpath,
				Dt = ddt,
				Size = size
			};
			_db.SyncList.Add(n);
			_db.SaveChanges();

			return Ok();
		}
	}
}
