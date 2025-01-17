using System;
using System.Threading.Tasks;
using GreetingService.Core.Entities;
using GreetingService.Core.Interfaces;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace GreetingService.API.Function.Users;

public class SbCreateUser
{
    private readonly ILogger<SbCreateUser> _logger;
    private readonly IUserService _userService;

    public SbCreateUser(ILogger<SbCreateUser> log, IUserService userService)
    {
        _logger = log;
        _userService = userService;
    }

    [FunctionName("SbCreateUser")]
    public async Task Run([ServiceBusTrigger("main", "user_create", Connection = "ServiceBusConnectionString")] User user)
    {
        _logger.LogInformation($"C# ServiceBus topic trigger function processed message: {user}");

        try
        {
            await _userService.CreateUserAsync(user);
        }
        catch (Exception e)
        {
            _logger.LogError("Failed to insert User in IUserService", e);
            throw;
        }
    }
}
