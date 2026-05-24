using BaiTapLon.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BaiTapLon.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected int? GetCustomerId(TokenService tokenService) =>
        tokenService.ValidateAccessToken(Request.Headers.Authorization);
}
