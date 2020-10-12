using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SyncServer.DAL;
using SyncServer.Models;

namespace SyncServer.Classes
{
	public class Auth
	{
		private static Dictionary<string, int> _G2U = new Dictionary<string, int>();
		private SyncContext _db;

		public Auth(SyncContext db)
		{
			_db = db;
		}

		public bool Login(HttpContext ctx, string email, string pwd)
		{
			var user = _db.Users.FirstOrDefault(u => u.Email == email && u.Pwd == pwd);

			if(user != null)
			{
				Guid guid = Guid.NewGuid();
				_G2U[guid.ToString()] = user.Id;

				ctx.Session.SetString("userguid", guid.ToString());
				return true;
			}
			return false;
		}

		public UserModel GetRequestUser(HttpContext ctx)
		{
			var guid = ctx.Session.GetString("userguid");
			if(guid == null)
				return null;
			var id = _G2U[guid];
			var user = _db.Users.Find(id);
			return user;
		}
	}
}
