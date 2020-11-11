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

		public const string DEFAULT_USER_EMAIL = "ramon@misoftware.com.br";
		public const string DEFAULT_USER_PWD = "SEnha123";

		public DbSet<UserModel> Users { get; set; }
		public DbSet<SyncListModel> SyncList { get; set; }

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			optionsBuilder.UseLazyLoadingProxies();
			base.OnConfiguring(optionsBuilder);
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<UserModel>(b =>
			{
				b.HasIndex(b => b.Email).IsUnique();

				b.HasData(new UserModel()
				{
					Id = 1,
					Email = DEFAULT_USER_EMAIL,
					Pwd = DEFAULT_USER_PWD,
				});
			});
		}
	}
}