using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SyncServer.Models
{
	public class SyncListModel
	{
		public int Id { get; set; }
		public string Dir { get; set; }
		public string Path { get; set; }
		public DateTime Dt { get; set; }
		public long Size { get; set; }
	}
}