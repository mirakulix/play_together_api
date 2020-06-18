﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using GraphQL;
using GraphQL.Types;
using PlayTogetherApi.Services;
using PlayTogetherApi.Data;
using PlayTogetherApi.Web.Models;
using PlayTogetherApi.Web.GraphQl.Types;

namespace PlayTogetherApi.Web.GraphQl
{
    public class PlayTogetherMutation : ObjectGraphType
    {
        public PlayTogetherMutation(
            PlayTogetherDbContext db,
            AuthenticationService authenticationService,
            ObservablesService observables,
            PushMessageService pushMessageService,
            FriendLogicService friendLogicService,
            UserStatisticsService userStatisticsService,
            InteractionsService interactionsService)
        {
            Name = "Mutation";

            FieldAsync<BooleanGraphType>(
                "joinEvent",
                description: "Add the currently logged in user to an event.",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<IdGraphType>> { Name = "event" },
                    new QueryArgument<UserEventStatusGraphType> { Name = "status", DefaultValue = UserEventStatus.AcceptedInvitation }
                ),
                resolve: async context =>
                {
                    var principal = context.UserContext as ClaimsPrincipal;
                    var userIdClaim = principal.Claims.FirstOrDefault(n => n.Type == "userid")?.Value;
                    if (!Guid.TryParse(userIdClaim, out var userId))
                    {
                        context.Errors.Add(new ExecutionError("Unauthorized"));
                        return false;
                    }
                    var user = await db.Users.FirstOrDefaultAsync(n => n.UserId == userId);
                    if (user == null)
                    {
                        context.Errors.Add(new ExecutionError("User not found."));
                        return false;
                    }

                    var eventId = context.GetArgument<Guid>("event");
                    var gameEvent = await db.Events.FirstOrDefaultAsync(n => n.EventId == eventId);
                    if (gameEvent == null)
                    {
                        context.Errors.Add(new ExecutionError("Event not found."));
                        return false;
                    }

                    var eventOwner = await db.Users.FirstOrDefaultAsync(n => n.UserId == gameEvent.CreatedByUserId);
                    if (eventOwner == null)
                    {
                        context.Errors.Add(new ExecutionError("Invalid event; the creator no longer exists."));
                        return false;
                    }



                    // todo: if it's a friendsonly event, then verify that the caller is the creator or a friend of the creator
                    if(gameEvent.FriendsOnly)
                    {

                    }



                    var signup = await db.UserEventSignups.FirstOrDefaultAsync(n => n.EventId == eventId && n.UserId == userId);
                    if (signup != null)
                    {
                        context.Errors.Add(new ExecutionError("Already signed up to this event."));
                        return false;
                    }

                    signup = new UserEventSignup {
                        EventId = eventId,
                        UserId = userId,
                        Status = context.GetArgument<UserEventStatus>("status")
                    };
                    db.UserEventSignups.Add(signup);
                    await db.SaveChangesAsync();

                    observables.UserEventSignupStream.OnNext(signup);

                    await userStatisticsService.UpdateStatisticsAsync(db, userId, user);

                    _ = pushMessageService.PushMessageAsync(
                        "JoinEvent",
                        "A player has joined!",
                        $"{user.DisplayName} signed up for \"{gameEvent.Title}\".", // todo: add a cleverly-formatted date/time?
                        new {
                            type = "JoinEvent",
                            eventId = eventId,
                            userId = userId,
                            eventName = gameEvent.Title,
                            userName = user.DisplayName
                        },
                        eventOwner.DeviceToken
                    );

                    return true;
                }
            );

            FieldAsync<BooleanGraphType>(
                "leaveEvent",
                description: "Remove the currently logged in user from an event.",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<IdGraphType>> { Name = "event" }
                ),
                resolve: async context =>
                {
                    var principal = context.UserContext as ClaimsPrincipal;
                    var userIdClaim = principal.Claims.FirstOrDefault(n => n.Type == "userid")?.Value;
                    if (!Guid.TryParse(userIdClaim, out var userId))
                    {
                        context.Errors.Add(new ExecutionError("Unauthorized"));
                        return false;
                    }
                    if (!await db.Users.AnyAsync(n => n.UserId == userId))
                    {
                        context.Errors.Add(new ExecutionError("User not found."));
                        return false;
                    }

                    var eventId = context.GetArgument<Guid>("event");

                    var signup = await db.UserEventSignups.FirstOrDefaultAsync(n => n.EventId == eventId && n.UserId == userId);
                    if (signup == null)
                    {
                        context.Errors.Add(new ExecutionError("Not signed up to this event."));
                        return false;
                    }

                    db.UserEventSignups.Remove(signup);
                    await db.SaveChangesAsync();

                    signup.Status = UserEventStatus.Cancelled;
                    observables.UserEventSignupStream.OnNext(signup);

                    await userStatisticsService.UpdateStatisticsAsync(db, userId);

                    return true;
                }
            );

            FieldAsync<UserEventSignupGraphType>(
                "updateSignup",
                description: "Update the current or any users signup state for an event. This requires the caller to be authorized.",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<IdGraphType>> { Name = "event", Description = "Id of the event to update the status for." },
                    new QueryArgument<NonNullGraphType<UserEventStatusGraphType>> { Name = "status", Description = "The new status." },
                    new QueryArgument<IdGraphType> { Name = "user", Description = "Id of the user, for event-owners changing the status of signsup to their event." }
                ),
                resolve: async context =>
                {
                    var principal = context.UserContext as ClaimsPrincipal;
                    var claimText = principal?.Claims?.FirstOrDefault(n => n.Type == "userid")?.Value;
                    Guid authenticatedUserId = Guid.Empty;
                    if (string.IsNullOrEmpty(claimText) || !Guid.TryParse(claimText, out authenticatedUserId))
                    {
                        context.Errors.Add(new ExecutionError("Unauthorized"));
                        return false;
                    }

                    var eventId = context.GetArgument<Guid>("event");
                    var eventDto = await db.Events.FirstOrDefaultAsync(n => n.EventId == eventId);
                    if (eventDto == null)
                    {
                        context.Errors.Add(new ExecutionError("Event doesn't exist."));
                        return null;
                    }

                    var userId = context.GetArgument<Guid>("user", authenticatedUserId);
                    if (userId != authenticatedUserId && eventDto.CreatedByUserId != authenticatedUserId)
                    {
                        context.Errors.Add(new ExecutionError("Must be the creator of the event to modify other users signups."));
                        return null;
                    }

                    var signup = await db.UserEventSignups.FirstOrDefaultAsync(n => n.EventId == eventId && n.UserId == userId);
                    if (signup == null)
                    {
                        // todo: consider just creating a signup instead?

                        context.Errors.Add(new ExecutionError("No signup found."));
                        return null;
                    }

                    signup.Status = context.GetArgument<UserEventStatus>("status");

                    db.UserEventSignups.Update(signup);
                    await db.SaveChangesAsync();

                    observables.UserEventSignupStream.OnNext(signup);

                    await userStatisticsService.UpdateStatisticsAsync(db, userId);

                    // todo: if the new status was AcceptedInvitation, maybe push a message to the owner of the event?

                    return signup;
                }
            );

            FieldAsync<EventGraphType>(
                "callToArms",
                description: "Create a new call-to-arms event. This requires the caller to be authorized.",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<DateTimeGraphType>> { Name = "startdate" },
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "title" },
                    new QueryArgument<NonNullGraphType<IdGraphType>> { Name = "game" }
                ),
                resolve: async context =>
                {
                    var principal = context.UserContext as ClaimsPrincipal;
                    var userIdClaim = principal.Claims.FirstOrDefault(n => n.Type == "userid")?.Value;
                    if (!Guid.TryParse(userIdClaim, out var callingUserId))
                    {
                        context.Errors.Add(new ExecutionError("Unauthorized"));
                        return null;
                    }

                    var callingUser = await db.Users.FirstOrDefaultAsync(n => n.UserId == callingUserId);
                    if (callingUser == null)
                    {
                        context.Errors.Add(new ExecutionError("User not found."));
                        return null;
                    }

                    var startdate = context.GetArgument<DateTime>("startdate");
                    if (startdate > DateTime.Now.AddMinutes(90))
                    {
                        context.Errors.Add(new ExecutionError("Startdate must be within 45 minutes."));
                        return null;
                    }

                    var gameId = context.GetArgument<Guid>("game");
                    if (gameId != default(Guid))
                    {
                        if (!await db.Games.AnyAsync(n => n.GameId == gameId))
                        {
                            context.Errors.Add(new ExecutionError("Game doesn't exist."));
                            return null;
                        }
                    }

                    var friendsOfChangingUser = await db.UserRelations.Where(n => n.Status == FriendLogicService.Relation_MutualFriends && (n.UserAId == callingUser.UserId || n.UserBId == callingUser.UserId)).ToArrayAsync();
                    if (friendsOfChangingUser.Length == 0)
                    {
                        context.Errors.Add(new ExecutionError("Must have friends to issue a call to arms :("));
                        return null;
                    }

                    var newEvent = new Event
                    {
                        Title = context.GetArgument<string>("title"),
                        CreatedByUserId = callingUserId,
                        EventDate = startdate,
                        EventEndDate = startdate + TimeSpan.FromMinutes(45),
                        Description = "",
                        FriendsOnly = true,
                        CallToArms = true,
                        GameId = gameId
                    };
                    db.Events.Add(newEvent);
                    await db.SaveChangesAsync();

                    observables.GameEventStream.OnNext(new EventChangedModel
                    {
                        Event = newEvent,
                        ChangingUser = callingUser,
                        FriendsOfChangingUser = friendsOfChangingUser,
                        Action = EventAction.Created,
                    });

                    var friendIds = friendsOfChangingUser.Select(n => n.UserAId == callingUserId ? n.UserBId : n.UserAId).ToList();
                    var friendDeviceTokens = await db.Users.Where(n => friendIds.Contains(n.UserId)).Select(n => n.DeviceToken).ToArrayAsync();

                    await userStatisticsService.UpdateStatisticsAsync(db, callingUserId, callingUser);
                    foreach (var friendId in friendIds)
                    {
                        await userStatisticsService.UpdateStatisticsAsync(db, friendId);
                    }

                    //foreach (var deviceToken in friendDeviceTokens)
                    //{
                        _ = pushMessageService.PushMessageAsync(
                            "CallToArms",
                            $"{callingUser.DisplayName} calls you to arms!",
                            newEvent.Title,
                            new
                            {
                                type = "CallToArms",
                                userId = callingUserId.ToString("N"),
                                userName = callingUser.DisplayName,
                                eventId = newEvent.EventId.ToString("N"),
                                eventTitle = newEvent.Title
                            },
                            friendDeviceTokens
                        );
                    //}

                    return newEvent;
                }
            );

            FieldAsync<EventGraphType>(
                "createEvent",
                description: "Create a new event. This requires the caller to be authorized.",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<DateTimeGraphType>> { Name = "startdate" },
                    new QueryArgument<NonNullGraphType<DateTimeGraphType>> { Name = "enddate" },
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "title" },
                    new QueryArgument<StringGraphType> { Name = "description" },
                    new QueryArgument<BooleanGraphType> { Name = "friendsOnly", DefaultValue = false },
                    new QueryArgument<NonNullGraphType<IdGraphType>> { Name = "game" }
                ),
                resolve: async context =>
                {
                    var principal = context.UserContext as ClaimsPrincipal;
                    var userIdClaim = principal.Claims.FirstOrDefault(n => n.Type == "userid")?.Value;
                    if (!Guid.TryParse(userIdClaim, out var callingUserId))
                    {
                        context.Errors.Add(new ExecutionError("Unauthorized"));
                        return null;
                    }

                    var startDate = context.GetArgument<DateTime>("startdate");
                    var endDate = context.GetArgument<DateTime>("enddate");
                    var title = context.GetArgument<string>("title");
                    var description = context.GetArgument<string>("description");
                    var gameId = context.GetArgument<Guid>("game");
                    var friendsOnly = context.HasArgument("friendsOnly")
                        ? context.GetArgument<bool>("friendsOnly")
                        : false;

                    try
                    {
                        var newEvent = await interactionsService.CreateEventAsync(callingUserId, startDate, endDate, title, description, friendsOnly, gameId);
                        return newEvent;
                    }
                    catch(Exception ex)
                    {
                        context.Errors.Add(new ExecutionError(ex.Message));
                        return null;
                    }
                }
            );

            FieldAsync<EventGraphType>(
                "updateEvent",
                description: "Update an event. This requires the caller to be authorized, and be the creator of the event.",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<IdGraphType>> { Name = "id", Description = "The ID of the event." },
                    new QueryArgument<DateTimeGraphType> { Name = "startdate" },
                    new QueryArgument<DateTimeGraphType> { Name = "enddate" },
                    new QueryArgument<StringGraphType> { Name = "title" },
                    new QueryArgument<StringGraphType> { Name = "description" },
                    new QueryArgument<BooleanGraphType> { Name = "friendsOnly" },
                    new QueryArgument<IdGraphType> { Name = "game" }
                ),
                resolve: async context =>
                {
                    EventAction? action = null;




                    // todo: if this is a call to arms, it should reject changing friendonly as well as the date




                    var principal = context.UserContext as ClaimsPrincipal;
                    var userIdClaim = principal.Claims.FirstOrDefault(n => n.Type == "userid")?.Value;
                    if (!Guid.TryParse(userIdClaim, out var userId))
                    {
                        context.Errors.Add(new ExecutionError("Unauthorized"));
                        return null;
                    }

                    var user = await db.Users.FirstOrDefaultAsync(n => n.UserId == userId);
                    if (user == null)
                    {
                        context.Errors.Add(new ExecutionError("User not found."));
                        return null;
                    }

                    var eventId = context.GetArgument<Guid>("id");
                    var editedEvent = await db.Events.Include(n => n.Signups).FirstOrDefaultAsync(n => n.EventId == eventId);
                    if (editedEvent == null || editedEvent.CreatedByUserId != userId)
                    {
                        context.Errors.Add(new ExecutionError("Must be the creator of the event to modify it."));
                        return null;
                    }

                    if (context.HasArgument("startdate"))
                    {
                        var date = context.GetArgument<DateTime>("startdate");
                        if (date != default)
                        {
                            if (editedEvent.CallToArms && date > DateTime.Now.AddMinutes(60))
                            {
                                context.Errors.Add(new ExecutionError("Call to arms must be within 60 minutes."));
                                return null;
                            }

                            editedEvent.EventDate = date;
                            action = EventAction.EditedPeriod;
                        }
                    }

                    if (context.HasArgument("enddate"))
                    {
                        var date = context.GetArgument<DateTime>("enddate");
                        if (date != default)
                        {
                            if(editedEvent.CallToArms && date > DateTime.Now.AddHours(4))
                            {
                                context.Errors.Add(new ExecutionError("Call to arms can't end more then 4 hours in the future."));
                                return null;
                            }

                            editedEvent.EventEndDate = date;
                            action = EventAction.EditedPeriod;
                        }
                    }

                    if (editedEvent.EventDate > editedEvent.EventEndDate)
                    {
                        context.Errors.Add(new ExecutionError("Start- and end-dates not in correct order."));
                        return null;
                    }

                    var title = context.GetArgument<string>("title");
                    if (!string.IsNullOrEmpty(title))
                    {
                        editedEvent.Title = title;
                        action = action.HasValue ? (action.Value | EventAction.EditedText) : EventAction.EditedText;
                    }

                    var description = context.GetArgument<string>("description");
                    if (!string.IsNullOrEmpty(description))
                    {
                        editedEvent.Description = description;
                        action = action.HasValue ? (action.Value | EventAction.EditedText) : EventAction.EditedText;
                    }

                    if (context.HasArgument("friendsOnly"))
                    {
                        if(editedEvent.CallToArms)
                        {
                            context.Errors.Add(new ExecutionError("Can't change the friendsonly-state of a call to arms."));
                            return null;
                        }

                        var friendsOnly = context.GetArgument<bool>("friendsOnly");
                        if(friendsOnly != editedEvent.FriendsOnly)
                        {
                            editedEvent.FriendsOnly = friendsOnly;
                            action = action.HasValue ? (action.Value | EventAction.EditedVisibility) : EventAction.EditedVisibility;
                        }
                    }

                    if (context.HasArgument("game"))
                    {
                        var gameId = context.GetArgument<Guid>("game");
                        if (gameId != default(Guid))
                        {
                            if (!await db.Games.AnyAsync(n => n.GameId == gameId))
                            {
                                context.Errors.Add(new ExecutionError("Game doesn't exist."));
                                return null;
                            }

                            editedEvent.GameId = gameId;
                            action = action.HasValue ? (action.Value | EventAction.EditedGame) : EventAction.EditedGame;
                        }
                    }

                    db.Events.Update(editedEvent);
                    await db.SaveChangesAsync();

                    observables.GameEventStream.OnNext(new EventChangedModel
                    {
                        Event = editedEvent,
                        ChangingUser = user,
                        FriendsOfChangingUser = !editedEvent.FriendsOnly ? null : await db.UserRelations.Where(n => n.Status == FriendLogicService.Relation_MutualFriends && (n.UserAId == user.UserId || n.UserBId == user.UserId)).ToArrayAsync(),
                        Action = action
                    });

                    if (action.HasValue && action.Value.HasFlag(EventAction.EditedPeriod))
                    {
                        await userStatisticsService.UpdateStatisticsAsync(db, userId, user);
                    }

                    return editedEvent;
                }
            );

            FieldAsync<BooleanGraphType>(
                "deleteEvent",
                description: "Delete an event.  This can only be done by the creator of the event, and requires the caller to be authorized.",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<IdGraphType>> { Name = "id", Description = "The ID of the event." }
                ),
                resolve: async context =>
                {
                    var principal = context.UserContext as ClaimsPrincipal;
                    var userIdClaim = principal.Claims.FirstOrDefault(n => n.Type == "userid")?.Value;
                    if (!Guid.TryParse(userIdClaim, out var userId))
                    {
                        context.Errors.Add(new ExecutionError("Unauthorized"));
                        return null;
                    }

                    var user = await db.Users.FirstOrDefaultAsync(n => n.UserId == userId);
                    if (user == null)
                    {
                        context.Errors.Add(new ExecutionError("User not found."));
                        return null;
                    }

                    var eventId = context.GetArgument<Guid>("id");
                    var dbEvent = await db.Events.FirstOrDefaultAsync(n => n.EventId == eventId);
                    if(dbEvent == null)
                    {
                        context.Errors.Add(new ExecutionError("Event doesn't exist."));
                        return false;
                    }
                    if (dbEvent.CreatedByUserId != userId)
                    {
                        context.Errors.Add(new ExecutionError("Event not created by user."));
                        return false;
                    }

                    db.Events.Remove(dbEvent);
                    await db.SaveChangesAsync();

                    observables.GameEventStream.OnNext(new EventChangedModel
                    {
                        Event = dbEvent,
                        ChangingUser = user,
                        Action = EventAction.Deleted
                    });

                    await userStatisticsService.UpdateStatisticsAsync(db, userId, user);

                    return true;
                }
            );

            FieldAsync<UserGraphType>(
                "createUser",
                description: "Create a new user. This will fail if the email is already in use.",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "displayName" },
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "email" },
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "password" },
                    new QueryArgument<IntGraphType> { Name = "utcOffset", Description = "UTC offset in seconds" },
                    new QueryArgument<StringGraphType> { Name = "deviceToken", Description = "FCM device token" }
                ),
                resolve: async context =>
                {
                    var displayName = context.GetArgument<string>("displayName");
                    var email = context.GetArgument<string>("email");
                    var password = context.GetArgument<string>("password");

                    if (!ValidateDisplayName(displayName))
                    {
                        context.Errors.Add(new ExecutionError("Displayname too short."));
                        return null;
                    }

                    if (!ValidateEmail(email))
                    {
                        context.Errors.Add(new ExecutionError("Email invalid."));
                        return null;
                    }

                    if (!ValidatePassword(password))
                    {
                        context.Errors.Add(new ExecutionError("Password too weak."));
                        return null;
                    }

                    var userExists = await db.Users.AnyAsync(n => n.Email == email);
                    if (userExists)
                    {
                        context.Errors.Add(new ExecutionError("User with this email already exists."));
                        return null;
                    }

                    var usedDisplayIds = await db.Users.Where(n => n.DisplayName == displayName).Select(n => n.DisplayId).Distinct().ToListAsync();
                    if(usedDisplayIds.Count >= 9999)
                    {
                        context.Errors.Add(new ExecutionError("Too many users with this displayname."));
                        return null;
                    }
                    var displayId = GetUniqueDisplayId(usedDisplayIds);

                    var utcOffset = TimeSpan.Zero;
                    if(context.HasArgument("utcOffset"))
                    {
                        utcOffset = TimeSpan.FromSeconds(context.GetArgument<int>("utcOffset"));
                        if(utcOffset < -TimeSpan.FromHours(24) || utcOffset > TimeSpan.FromHours(24))
                        {
                            context.Errors.Add(new ExecutionError("UtcOffset larger than 24 hours."));
                            return null;
                        }
                    }

                    var deviceToken = context.HasArgument("deviceToken") ? context.GetArgument<string>("deviceToken") : null;

                    var passwordHash = authenticationService.CreatePasswordHash(password);

                    var newUser = new User
                    {
                        DisplayName = displayName,
                        DisplayId = displayId,
                        Email = email,
                        PasswordHash = passwordHash,
                        UtcOffset = utcOffset,
                        DeviceToken = deviceToken
                    };

                    db.Users.Add(newUser);
                    await db.SaveChangesAsync();

                    return newUser;
                }
            );

            FieldAsync<UserGraphType>(
                "updateUser",
                description: "Update the currently logged in user.",
                arguments: new QueryArguments(
                    new QueryArgument<StringGraphType> { Name = "displayName" },
                    new QueryArgument<StringGraphType> { Name = "email" },
                    new QueryArgument<StringGraphType> { Name = "password" },
                    new QueryArgument<StringGraphType> { Name = "avatar", Description = "Filename of the new avatar image" },
                    new QueryArgument<IntGraphType> { Name = "utcOffset", Description = "UTC offset in seconds" },
                    new QueryArgument<StringGraphType> { Name = "deviceToken", Description = "FCM device token" }
                ),
                resolve: async context =>
                {
                    var principal = context.UserContext as ClaimsPrincipal;
                    var userIdClaim = principal.Claims.FirstOrDefault(n => n.Type == "userid")?.Value;
                    if (!Guid.TryParse(userIdClaim, out var userId))
                    {
                        context.Errors.Add(new ExecutionError("Unauthorized"));
                        return null;
                    }

                    var editedUser = await db.Users.FirstOrDefaultAsync(n => n.UserId == userId);
                    if (editedUser == null)
                    {
                        context.Errors.Add(new ExecutionError("User not found."));
                        return null;
                    }

                    bool wasChangedRelevantForSubscription = false;

                    var displayName = context.GetArgument<string>("displayName")?.Trim();
                    if(!string.IsNullOrEmpty(displayName))
                    {
                        if (!ValidateDisplayName(displayName))
                        {
                            context.Errors.Add(new ExecutionError("Displayname too short."));
                            return null;
                        }
                        if (editedUser.DisplayName != displayName)
                        {
                            editedUser.DisplayName = displayName;

                            var usedDisplayIds = await db.Users.Where(n => n.DisplayName == displayName).Select(n => n.DisplayId).Distinct().ToListAsync();
                            if (usedDisplayIds.Count >= 9999)
                            {
                                context.Errors.Add(new ExecutionError("Too many users with this displayname."));
                                return null;
                            }
                            if (usedDisplayIds.Contains(editedUser.DisplayId))
                            {
                                editedUser.DisplayId = GetUniqueDisplayId(usedDisplayIds);
                            }

                            wasChangedRelevantForSubscription = true;
                        }
                    }

                    var email = context.GetArgument<string>("email")?.Trim();
                    if(!string.IsNullOrEmpty(email))
                    {
                        if (!ValidateEmail(email))
                        {
                            context.Errors.Add(new ExecutionError("Email invalid."));
                            return null;
                        }
                        editedUser.Email = email;
                    }

                    var password = context.GetArgument<string>("password");
                    if(!string.IsNullOrEmpty(password))
                    {
                        if (!ValidatePassword(password))
                        {
                            context.Errors.Add(new ExecutionError("Password too weak."));
                            return null;
                        }
                        editedUser.PasswordHash = authenticationService.CreatePasswordHash(password);
                    }

                    var avatar = context.GetArgument<string>("avatar")?.Trim();
                    if (!string.IsNullOrEmpty(avatar))
                    {
                        if (!db.Avatars.Any(n => n.ImagePath == avatar))
                        {
                            context.Errors.Add(new ExecutionError("Avatar not available."));
                        }
                        else
                        {
                            // todo: here we might later add checks around premium-avatars and access rights
                            if (editedUser.AvatarFilename != avatar)
                            {
                                editedUser.AvatarFilename = avatar;
                                wasChangedRelevantForSubscription = true;
                            }
                        }
                    }

                    if (context.HasArgument("utcOffset"))
                    {
                        var utcOffset = TimeSpan.FromSeconds(context.GetArgument<int>("utcOffset"));
                        if (utcOffset < -TimeSpan.FromHours(24) || utcOffset > TimeSpan.FromHours(24))
                        {
                            context.Errors.Add(new ExecutionError("UtcOffset larger than 24 hours."));
                            return null;
                        }
                        if(utcOffset != editedUser.UtcOffset)
                        {
                            editedUser.UtcOffset = utcOffset;

                            // todo: update to statistics subscription
                        }
                    }

                    if (context.HasArgument("deviceToken"))
                    {
                        var deviceToken = context.GetArgument<string>("deviceToken");
                        if (deviceToken != null)
                        {
                            editedUser.DeviceToken = deviceToken;
                        }
                    }

                    db.Users.Update(editedUser);
                    await db.SaveChangesAsync();

                    if (wasChangedRelevantForSubscription)
                    {
                        var friends = await db.UserRelations
                            .Where(relation => (relation.UserBId == userId || relation.UserAId == userId)) // todo: consider filtering out relations that are not friends or at least are blocked
                            .ToArrayAsync();
                        var observableModel = new UserChangedSubscriptionModel
                        {
                            ChangingUser = editedUser,
                            FriendsOfChangingUser = friends
                        };
                        observables.UserChangeStream.OnNext(observableModel);
                    }

                    return editedUser;
                }
            );

            FieldAsync<BooleanGraphType>(
                "deleteUser",
                description: "Delete the logged-in user. This requires the caller to be authorized. (STILL A WORK IN PROGRESS)",
                resolve: async context =>
                {
                    var principal = context.UserContext as ClaimsPrincipal;
                    var userIdClaim = principal.Claims.FirstOrDefault(n => n.Type == "userid")?.Value;
                    if (!Guid.TryParse(userIdClaim, out var userId))
                    {
                        context.Errors.Add(new ExecutionError("Unauthorized"));
                        return null;
                    }

                    var user = await db.Users.FirstOrDefaultAsync(n => n.UserId == userId);
                    if (user == null)
                    {
                        context.Errors.Add(new ExecutionError("User not found."));
                        return null;
                    }

                    user.DisplayName = "DELETED_USER";
                    user.DisplayId = await db.Users.CountAsync(n => n.SoftDelete) + 2;
                    user.AvatarFilename = "avatar_deleted.jpg";
                    user.SoftDelete = true;
                    user.Email = "DELETED";
                    user.PasswordHash = "DELETED";
                    db.Users.Update(user);

                    var futureSignups = db.UserEventSignups.Where(n => n.UserId == userId && n.Event.EventDate > DateTime.Now);
                    db.UserEventSignups.RemoveRange(futureSignups);

                    var relations = db.UserRelations.Where(n => n.UserAId == userId || n.UserBId == userId);
                    db.UserRelations.RemoveRange(relations);

                    var eventsWithNoSignups = db.Events.Where(n => n.CreatedByUserId == userId && !n.Signups.Any(nn => nn.UserId != userId));
                    db.Events.RemoveRange(eventsWithNoSignups);

                    await db.SaveChangesAsync();

                    return true;
                }
            );

            FieldAsync<UserRelationGraphType>(
                "changeUserRelation",
                description: "Invite a user to your friendlist, or accept an invitation from a user. This requires the caller to be authorized.",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<IdGraphType>> { Name = "user" },
                    new QueryArgument<NonNullGraphType<UserRelationActionGraphType>> { Name = "status" }
                ),
                resolve: async context =>
                {
                    var principal = context.UserContext as ClaimsPrincipal;
                    var userIdClaim = principal.Claims.FirstOrDefault(n => n.Type == "userid")?.Value;
                    if (!Guid.TryParse(userIdClaim, out var callingUserId))
                    {
                        context.Errors.Add(new ExecutionError("Unauthorized"));
                        return null;
                    }

                    var friendUserId = context.GetArgument<Guid>("user");
                    var action = context.GetArgument<UserRelationAction>("status");

                    try
                    {
                        var result = await interactionsService.ChangeUserRelationAsync(callingUserId, friendUserId, action);
                        return result;
                    }
                    catch (Exception ex)
                    {
                        context.Errors.Add(new ExecutionError(ex.Message));
                        return null;
                    }
                }
            );

            FieldAsync<TokenResponseGraphType>(
                "token",
                description: "Request authorization tokens for a user, based on email/password or a refresh token.",
                arguments: new QueryArguments(
                    new QueryArgument<StringGraphType> { Name = "email" },
                    new QueryArgument<StringGraphType> { Name = "password" },
                    new QueryArgument<StringGraphType> { Name = "refreshToken" },
                    new QueryArgument<IntGraphType> { Name = "accessTokenLifetime", Description = "Optional lifetime in minutes. Max: 90." },
                    new QueryArgument<IntGraphType> { Name = "refreshTokenLifetime", Description = "Optional lifetime in minutes. Max: 43200 (30 days)." }
                ),
                resolve: async context =>
                {
                    var requestDto = context.HasArgument("refreshToken")
                        ? new TokenRequestModel
                            {
                                Grant_type = "refresh_token",
                                Refresh_token = context.GetArgument<Guid>("refreshToken")
                            }
                        : new TokenRequestModel
                            {
                                Grant_type = "password",
                                Username = context.GetArgument<string>("email"),
                                Password = context.GetArgument<string>("password")
                            };

                    if(context.HasArgument("accessTokenLifetime"))
                    {
                        requestDto.AccessTokenLifetime = context.GetArgument<int>("accessTokenLifetime");
                    }
                    if (context.HasArgument("refreshTokenLifetime"))
                    {
                        requestDto.RefreshTokenLifetime = context.GetArgument<int>("refreshTokenLifetime");
                    }

                    var response = await authenticationService.RequestTokenAsync(requestDto);

                    return response;
                }
            );
        }

        // todo: better rules

        static private bool ValidateEmail(string email) => email.Length >= 5 && email.Contains("@");
        static private bool ValidateDisplayName(string displayName) => displayName.Trim().Length >= 3;
        static private bool ValidatePassword(string password) => password.Trim().Length >= 3;

        static private Random rnd = new Random();

        static private int GetUniqueDisplayId(List<int> usedDisplayIds)
        {
            if (usedDisplayIds.Count >= 9999)
                throw new ArgumentException("Too many users with same display name!");

            var range = usedDisplayIds.Count >= 999 ? 9999 : 999;

            while(true)
            {
                // this could obviously be optimized for when the list is long, but it's irrelevant now
                var id = rnd.Next(101, range);
                if (!usedDisplayIds.Contains(id))
                    return id;
            }
        }
    }
}
