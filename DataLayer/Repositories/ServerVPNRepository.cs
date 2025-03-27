using DataLayer.Context;
using Domain.Entities.VPN;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.Repositories
{
    public class ServerVPNRepository : IServerVPNRepository
    {
        private readonly VeilVpnDbContext _context;

        public ServerVPNRepository(VeilVpnDbContext context)
        {
            _context = context;
        }

        public async Task<List<VPNServer>> GetAllServersAsync()
        {
            return await _context.VPNServers
                .OrderByDescending(s => s.UpdatedAt)
                .ToListAsync();
        }

        public async Task<VPNServer> GetServerByIdAsync(string id)
        {
            return await _context.VPNServers.FindAsync(id);
        }

        public async Task<VPNServer> CreateServerAsync(VPNServer server)
        {
            server.Id = Guid.NewGuid().ToString();
            server.CreateDateTime = DateTime.Now;
            server.UpdatedAt = DateTime.Now;

            await _context.VPNServers.AddAsync(server);
            await _context.SaveChangesAsync();

            return server;
        }

        public async Task<bool> UpdateServerAsync(VPNServer server)
        {
            try
            {
                // ابتدا موجودیت را از دیتابیس دریافت می‌کنیم
                var existingServer = await _context.VPNServers.FindAsync(server.Id);

                if (existingServer == null)
                    return false;

                // به‌روزرسانی فیلدها
                existingServer.Name = server.Name;
                existingServer.IpAddress = server.IpAddress;
                existingServer.Location = server.Location;
                existingServer.MaxUsers = server.MaxUsers;
                existingServer.CurrentUsers = server.CurrentUsers;
                existingServer.IsActive = server.IsActive;
                existingServer.ApiUrl = server.ApiUrl;
                existingServer.ApiUsername = server.ApiUsername;

                // اگر رمز جدید وارد شده باشد، آن را به‌روزرسانی کنیم
                if (!string.IsNullOrEmpty(server.ApiPassword))
                {
                    existingServer.ApiPassword = server.ApiPassword;
                }

                existingServer.Flag = server.Flag;
                existingServer.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                // لاگ کردن خطا
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<bool> DeleteServerAsync(string id)
        {
            try
            {
                var server = await _context.VPNServers.FindAsync(id);
                if (server == null)
                    return false;

                _context.VPNServers.Remove(server);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteMultipleServersAsync(List<string> ids)
        {
            try
            {
                var servers = await _context.VPNServers
                    .Where(s => ids.Contains(s.Id))
                    .ToListAsync();

                if (!servers.Any())
                    return false;

                _context.VPNServers.RemoveRange(servers);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<int> GetActiveServersCountAsync()
        {
            return await _context.VPNServers
                .CountAsync(s => s.IsActive);
        }

        public async Task<int> GetTotalServersCountAsync()
        {
            return await _context.VPNServers.CountAsync();
        }
    }
}
