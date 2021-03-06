﻿using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using GraphQL;
using GraphQL.Types;
using PlayTogetherApi.Data;
using PlayTogetherApi.Web.Models;

namespace PlayTogetherApi.Web.GraphQl.Types
{
    public class GameGraphType : ObjectGraphType<Game>
    {
        public GameGraphType(IConfiguration config)
        {
            Name = "Game";

            Field("id", game => game.GameId, type: typeof(IdGraphType));
            Field(game => game.Title);
            Field("image", game => config.GetValue<string>("AssetPath") + game.ImagePath, type: typeof(StringGraphType));

            Field<EventCollectionGraphType>("events",
                arguments: new QueryArguments(
                   new QueryArgument<DateTimeGraphType> { Name = "beforeDate", Description = "Event occurs before or on this datetime." },
                   new QueryArgument<DateTimeGraphType> { Name = "afterDate", Description = "Event occurs on or after this datetime." },
                   new QueryArgument<IntGraphType> { Name = "skip", Description = "How many events to skip." },
                   new QueryArgument<IntGraphType> { Name = "take", Description = "How many events to return." }
                ),
                resolve: context =>
                {
                    var db = context.RequestServices.GetService<PlayTogetherDbContext>();

                    var query = db.Events.Where(n => n.GameId == context.Source.GameId);

                    var afterDate = context.GetArgument<DateTime>("afterDate");
                    if (afterDate != default(DateTime))
                    {
                        query = query.Where(n => n.EventEndDate >= afterDate);
                    }

                    var beforeDate = context.GetArgument<DateTime>("beforeDate");
                    if (beforeDate != default(DateTime))
                    {
                        query = query.Where(n => n.EventDate <= beforeDate);
                    }

                    var skip = context.GetArgument<int>("skip");
                    if (skip > 0)
                    {
                        query = query.Skip(skip);
                    }

                    var take = context.GetArgument<int>("take");
                    if (take > 0)
                    {
                        query = query.Take(take);
                    }

                    return new EventCollectionModel
                    {
                        EventsQuery = query,
                        TotalEventsQuery = db.Events.Where(n => n.GameId == context.Source.GameId)
                    };
                }
            );
        }
    }
}
