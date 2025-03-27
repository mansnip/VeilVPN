using Application.Services.Interfaces;
using Domain.Entities.VPN;
using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Implimentation
{
    public class ServerVPNService : IServerVPNService
    {
        private readonly IServerVPNRepository _serverVPNRepository;
        private readonly IUserRepository _userRepository;

        public ServerVPNService(IServerVPNRepository serverVPNRepository, IUserRepository userRepository)
        {
            _serverVPNRepository = serverVPNRepository;
            _userRepository = userRepository;
        }

        public async Task<List<VPNServer>> GetAllServersAsync()
        {
            return await _serverVPNRepository.GetAllServersAsync();
        }

        public async Task<VPNServer> GetServerByIdAsync(string id)
        {
            return await _serverVPNRepository.GetServerByIdAsync(id);
        }

        public async Task<VPNServer> CreateServerAsync(VPNServer server)
        {
            return await _serverVPNRepository.CreateServerAsync(server);
        }

        public async Task<bool> UpdateServerAsync(VPNServer server)
        {
            return await _serverVPNRepository.UpdateServerAsync(server);
        }

        public async Task<bool> DeleteServerAsync(string id)
        {
            return await _serverVPNRepository.DeleteServerAsync(id);
        }

        public async Task<bool> DeleteMultipleServersAsync(List<string> ids)
        {
            return await _serverVPNRepository.DeleteMultipleServersAsync(ids);
        }

        public async Task<int> GetActiveServersCountAsync()
        {
            return await _serverVPNRepository.GetActiveServersCountAsync();
        }

        public async Task<int> GetTotalServersCountAsync()
        {
            return await _serverVPNRepository.GetTotalServersCountAsync();
        }
    }
}
