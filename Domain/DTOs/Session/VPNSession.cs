using Domain.Entities.VPN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.DTOs.Session
{
    public class VPNSession
    {
        public HttpClient Client { get; set; }
        public VPNServer Server { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> Cookies { get; set; }
    }
}
