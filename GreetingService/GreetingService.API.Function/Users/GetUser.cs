using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using GreetingService.API.Function.Authentication;
using GreetingService.Core.Entities;
using GreetingService.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;

namespace GreetingService.API.Function.Users
{
    public class GetUser
    {
        private readonly ILogger<GetUser> _logger;
        private readonly IAuthHandler _authHandler;
        private readonly IUserService _userService;

        public GetUser(ILogger<GetUser> log, IAuthHandler authHandler, IUserService userService)
        {
            _logger = log;
            _authHandler = authHandler;
            _userService = userService;
        }

        [FunctionName("GetUser")]
        [OpenApiOperation(operationId: "Run", tags: new[] { "User" })]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(User), Description = "The OK response")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = "Not found")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "user/{email}")] HttpRequest req, string email)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            if (!await _authHandler.IsAuthorizedAsync(req))
                return new UnauthorizedResult();

            var user = await _userService.GetUserAsync(email);

            if (user == null)
                return new NotFoundObjectResult("Not found");

            return new OkObjectResult(user);
        }
    }
}

