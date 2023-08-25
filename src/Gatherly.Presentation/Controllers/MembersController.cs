﻿using Gatherly.Application.Members.Commands.CreateMember;
using Gatherly.Application.Members.Commands.Login;
using Gatherly.Application.Members.Commands.UpdateMember;
using Gatherly.Application.Members.Queries.GetMemberById;
using Gatherly.Domain.Enums;
using Gatherly.Domain.Shared;
using Gatherly.Infrastructure.Authentication;
using Gatherly.Presentation.Abstractions;
using Gatherly.Presentation.Contracts.Members;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gatherly.Presentation.Controllers;

[Route("api/members")]
public sealed class MembersController : ApiController
{
    public MembersController(ISender sender) 
        : base(sender)
    { }

    //[HasPermission(Permission.ReadMembers)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetMemberId(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetMemberByIdQuery(id);

        Result<MemberResponse> response = await Sender.Send(query, cancellationToken);

        return response.IsSuccess 
            ? Ok(response.Value) 
            : NotFound(response.Error);
    }

    [HttpPost("login")]
    public async Task<IActionResult> LoginMember(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var command = new LoginCommand(request.Email);

        Result<string> tokenResult = await Sender.Send(command, cancellationToken);

        if (tokenResult.IsFailure)
            return HandleFailure(tokenResult);

        return Ok(tokenResult.Value);
    }

    [HttpPost]
    public async Task<IActionResult> RegisterMember(
        [FromBody] RegisterMemberRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateMemberCommand(
            request.Email,
            request.FirstName,
            request.LastName);

        Result<Guid> result = await Sender.Send(command, cancellationToken);

        if(result.IsFailure)
            return HandleFailure(result);

        return CreatedAtAction(
            nameof(GetMemberId),
            new { Id = result.Value },
            result.Value);
    }

    [HasPermission(Permission.UpdateMember)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateMember(
        Guid id,
        [FromBody] UpdateMemberRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateMemberCommand(id, request.FirstName, request.LastName);

        Result result = await Sender.Send(
            command, cancellationToken);

        if(result.IsFailure)
            return HandleFailure(result);

        return NoContent();
    }
}
