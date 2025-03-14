using Domain.Entities.Account;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.Context
{
    public class VeilVpnDbContext : DbContext
    {
        public VeilVpnDbContext(DbContextOptions<VeilVpnDbContext> options) : base(options)
        {

        }


        // DBSets
        public DbSet<User> Users { get; set; }

    }
}
