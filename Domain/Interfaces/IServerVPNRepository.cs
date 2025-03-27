using Domain.Entities.VPN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IServerVPNRepository
    {
        Task<List<VPNServer>> GetAllServersAsync();
        Task<VPNServer> GetServerByIdAsync(string id);
        Task<VPNServer> CreateServerAsync(VPNServer server);
        Task<bool> UpdateServerAsync(VPNServer server);
        Task<bool> DeleteServerAsync(string id);
        Task<bool> DeleteMultipleServersAsync(List<string> ids);
        Task<int> GetActiveServersCountAsync();
        Task<int> GetTotalServersCountAsync();
    }
}
