﻿using Gatherly.Domain.Enums;
using Gatherly.Domain.Errors;
using Gatherly.Domain.Exceptions;
using Gatherly.Domain.Primitives;
using Gatherly.Domain.Shared;

namespace Gatherly.Domain.Entities;

public sealed class Gathering : AggregateRoot
{
    private readonly List<Invitation> _invitations = new();
    private readonly List<Attendee> _attendees = new();

    private Gathering(
        Guid id,
        Member creator,
        GatheringType type,
        DateTime scheduledAtUtc,
        string name,
        string? location)
        : base(id)
    {
        Creator = creator;
        Type = type;
        ScheduledAtUtc = scheduledAtUtc;
        Name = name;
        Location = location;
    }

    public Member Creator { get; private set; }
    public GatheringType Type { get; private set; }
    public string Name { get; private set; }
    public DateTime ScheduledAtUtc { get; private set; }
    public string? Location { get; private set; }
    public int? MaximumNumberOfAttendees { get; private set; }
    public DateTime? InvitationsExpireAtUtc { get; private set; }
    public int NumberOfAttendees { get; private set; }
    public IReadOnlyCollection<Attendee> Attendees => _attendees;
    public IReadOnlyCollection<Invitation> Invitations => _invitations;

    public static Gathering Create(
        Guid id,
        Member creator,
        GatheringType type,
        DateTime scheduledAtUtc,
        string name,
        string? location,
        int? maximumNumberOfAttendees,
        int? invitationsValidBeforeInHours)
    {
        // Create Gathering
        var gathering = new Gathering(
            Guid.NewGuid(),
            creator,
            type,
            scheduledAtUtc,
            name,
            location);


        // Calculate gathering type details
        gathering.CalculateGatheringTypeDetails(maximumNumberOfAttendees, invitationsValidBeforeInHours);

        return gathering;
    }

    private void CalculateGatheringTypeDetails(
        int? maximumNumberOfAttendees,
        int? invitationsValidBeforeInHours)
    {
        switch (Type)
        {
            case GatheringType.WithFixedNumberOfAttendees:
                if (maximumNumberOfAttendees is null)
                {
                    throw new GatheringMaximumNumberOfAttendeesIsNullDomainException(
                        $"{nameof(maximumNumberOfAttendees)} can't be null."
                    );
                }
                MaximumNumberOfAttendees = maximumNumberOfAttendees;
                break;
            case GatheringType.WithExpirationForInvitations:
                if (invitationsValidBeforeInHours is null)
                {
                    throw new GatheringInvitationsValidBeforeInHoursIsNullDomainException(
                        $"{nameof(invitationsValidBeforeInHours)} can't be null."
                    );
                }
                InvitationsExpireAtUtc = ScheduledAtUtc.AddHours(invitationsValidBeforeInHours.Value);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(GatheringType));
        }
    }

    public Result<Invitation> SendInvitation(Member member)
    {
        // Validate 
        if (Creator.Id == member.Id) 
            return Result.Failure<Invitation>(DomainErrors.Gathering.InvitingCreator);

        if (ScheduledAtUtc < DateTime.UtcNow) 
            return Result.Failure<Invitation>(DomainErrors.Gathering.AlreadyPassed);

        var invitation = new Invitation(Guid.NewGuid(), member, this);

        _invitations.Add(invitation);

        return invitation;
    }

    public Result<Attendee> AcceptInvitation(Invitation invitation)
    {
        var reachedMaximumNumberOfAttendess = Type == GatheringType.WithFixedNumberOfAttendees && NumberOfAttendees == MaximumNumberOfAttendees;

        var reachedInvitationsExpiration = Type == GatheringType.WithExpirationForInvitations && InvitationsExpireAtUtc < DateTime.UtcNow;

        var expired = reachedMaximumNumberOfAttendess || reachedInvitationsExpiration;

        if (expired)
        {
            invitation.Expire();

            return Result.Failure<Attendee>(DomainErrors.Gathering.Expired);
        }

        var attendee = invitation.Accept();

        _attendees.Add(attendee);
        NumberOfAttendees++;

        return attendee;
    }
}
