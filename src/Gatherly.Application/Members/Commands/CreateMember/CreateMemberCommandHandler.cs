﻿using Gatherly.Application.Abstractions.Messaging;
using Gatherly.Domain.Entities;
using Gatherly.Domain.Errors;
using Gatherly.Domain.Repositories;
using Gatherly.Domain.Shared;
using Gatherly.Domain.ValueObjects;
using MediatR;

namespace Gatherly.Application.Members.Commands.CreateMember;

internal sealed class CreateMemberCommandHandler
    : ICommandHandler<CreateMemberCommand, Guid>
{
    private readonly IMemberRepository _memberRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateMemberCommandHandler(
        IMemberRepository memberRepository, 
        IUnitOfWork unitOfWork)
    {
        _memberRepository = memberRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateMemberCommand request, CancellationToken cancellationToken)
    {
        Result<Email> emailResult = Email.Create(request.Email);

        if (emailResult.IsFailure)
            return Result.Failure<Guid>(emailResult.Errors);

        Result<FirstName> firstNameResult = FirstName.Create(request.FirstName);
        Result<LastName> lastNameResult = LastName.Create(request.LastName);
        
        if (!await _memberRepository.IsEmailUniqueAsync(emailResult.Value, cancellationToken))
            return Result.Failure<Guid>(DomainErrors.Member.EmailAlreadyInUse);
        
        var member = Member.Create(
            Guid.NewGuid(),
            firstNameResult.Value,
            lastNameResult.Value,
            emailResult.Value);

        _memberRepository.Add(member);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return member.Id;
    }
}
