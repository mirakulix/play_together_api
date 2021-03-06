﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlayTogetherApi.Web.Models;
using PlayTogetherApi.Data;
using PlayTogetherApi.Services;

namespace PlayTogetherApi.Web.Controllers
{
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly AuthenticationService _authenticationService;

        public AuthenticationController(AuthenticationService securityService)
        {
            _authenticationService = securityService;
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("api/token")]
        public async Task<IActionResult> GetAccessToken([FromForm]TokenRequestModel dto)
        {
            IActionResult response = Unauthorized();

            if (dto != null)
            {
                var tokenResponse = await _authenticationService.RequestTokenAsync(dto);
                if (tokenResponse != null)
                {
                    response = Ok(tokenResponse);
                }
            }

            return response;
        }
    }
}
