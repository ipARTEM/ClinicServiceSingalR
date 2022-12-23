using ClinicService.Models.Requests;
using ClinicServiceProtos;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using System.Net.Http.Headers;
using static ClinicServiceProtos.AuthenticateService;
using AuthenticationRequest = ClinicServiceProtos.AuthenticationRequest;
using AuthenticationResponse = ClinicServiceProtos.AuthenticationResponse;

namespace ClinicService.Services.Impl
{
    [Authorize]
    public class AuthService : AuthenticateServiceBase
    {
        #region Services

        private readonly IAuthenticateService _authenticateService;

        #endregion

        #region Constructors

        public AuthService(IAuthenticateService authenticateService)
        {
            _authenticateService = authenticateService;
        }

        #endregion

        [AllowAnonymous]
        public override Task<AuthenticationResponse> Login(AuthenticationRequest request, ServerCallContext context)
        {
            Models.Requests.AuthenticationResponse response = _authenticateService.Login(new Models.Requests.AuthenticationRequest
            {
                Login = request.UserName,
                Password = request.Password
            });

            if (response.Status == Models.AuthenticationStatus.Success)
            {
                context.ResponseTrailers.Add("X-Session-Token", response.SessionContext.SessionToken);
            }

            return Task.FromResult(new AuthenticationResponse
            {
                Status = (int)response.Status,
                SessionContext = new SessionContext
                {
                    SessionId = response.SessionContext.SessionId,
                    SessionToken = response.SessionContext.SessionToken,
                    Account = new AccountDto
                    {
                        AccountId = response.SessionContext.Account.AccountId,
                        EMail = response.SessionContext.Account.EMail,
                        FirstName = response.SessionContext.Account.FirstName,
                        LastName = response.SessionContext.Account.LastName,
                        SecondName = response.SessionContext.Account.SecondName,
                        Locked = response.SessionContext.Account.Locked
                    }
                }
            });

        }

        public override Task<GetSessionResponse> GetSession(GetSessionRequest request, ServerCallContext context)
        {
            var authorizationHeader = context.RequestHeaders.FirstOrDefault(header => header.Key == "Authorization");
            if (AuthenticationHeaderValue.TryParse(authorizationHeader.Value, out var headerValue))
            {
                var scheme = headerValue.Scheme; // "Bearer"
                var sessionToken = headerValue.Parameter; // Token
                if (string.IsNullOrEmpty(sessionToken))
                {
                    return Task.FromResult(new GetSessionResponse());
                }

                ClinicService.Models.SessionContext sessionContext = _authenticateService.GetSessionInfo(sessionToken);
                if (sessionContext == null)
                {
                    return Task.FromResult(new GetSessionResponse());
                }

                return Task.FromResult(new GetSessionResponse
                {
                    SessionContext = new SessionContext
                    {
                        SessionId = sessionContext.SessionId,
                        SessionToken = sessionContext.SessionToken,
                        Account = new AccountDto
                        {
                            AccountId = sessionContext.Account.AccountId,
                            EMail = sessionContext.Account.EMail,
                            FirstName = sessionContext.Account.FirstName,
                            LastName = sessionContext.Account.LastName,
                            SecondName = sessionContext.Account.SecondName,
                            Locked = sessionContext.Account.Locked
                        }
                    }
                });
            }

            return Task.FromResult(new GetSessionResponse());

        }


    }
}
