using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncServer.Models;

namespace SyncServer.DAL
{
	public class SyncContext : DbContext
	{
		public SyncContext(DbContextOptions<SyncContext> options)
			   : base(options)
		{
		}

		public DbSet<UserModel> Users { get; set; }
		public DbSet<SyncListModel> SyncList { get; set; }

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			optionsBuilder.UseLazyLoadingProxies();
			base.OnConfiguring(optionsBuilder);
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
		}
	}
}