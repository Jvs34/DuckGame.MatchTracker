using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatchViewer.Shared;

public static class SharedProgram
{
    public static void ConfigureServices( IServiceCollection services, bool isServer, params string[] args )
    {
        services.AddMudServices();
    }

    public static void ConfigureApp( IServiceCollection services, bool isServer, params string[] args )
    {

    }
}
