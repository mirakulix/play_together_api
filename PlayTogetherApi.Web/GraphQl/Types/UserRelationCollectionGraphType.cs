﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using GraphQL.Types;
using PlayTogetherApi.Data;
using PlayTogetherApi.Models;
using PlayTogetherApi.Web.Models;

namespace PlayTogetherApi.Web.GraphQl.Types
{
    public class UserRelationCollectionGraphType : ObjectGraphType<UserRelationCollectionModel>
    {
        public UserRelationCollectionGraphType()
        {
            Name = "UserRelationCollection";

            FieldAsync<IntGraphType>("total",
                description: "The total number of friends available",
                resolve: async context =>
                {
                    var total = await context.Source.TotalItemsQuery.CountAsync();
                    return total;
                }
            );

            FieldAsync<IntGraphType>("count",
                description: "The number of friends selected by the query",
                resolve: async context =>
                {
                    var count = await context.Source.ItemsQuery.CountAsync();
                    return count;
                }
            );

            FieldAsync<ListGraphType<UserRelationGraphType>>("items",
                resolve: async context => await context.Source.ItemsQuery.Select(rel => new UserRelationExtModel { ActiveUserId = context.Source.UserId, Relation = rel }).ToListAsync());
        }
    }
}
