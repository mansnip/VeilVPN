using Application.Services.Implimentation;
using Application.Services.Interfaces;
using DataLayer.Repositories;
using Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoC
{
    public class DependencyContainer
    {
        public static void RegisterDependencies(IServiceCollection service)
        {
            #region services

            service.AddScoped<IUserService, UserService>();

            #endregion

            #region Repositories

            service.AddScoped<IUserRepository, UserRepository>();


            #endregion
        }
    }
}
