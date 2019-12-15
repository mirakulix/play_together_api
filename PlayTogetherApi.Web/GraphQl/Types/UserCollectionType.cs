﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using GraphQL.Types;
using PlayTogetherApi.Data;

namespace PlayTogetherApi.Web.GraphQl.Types
{
    public class UserCollectionType : ObjectGraphType<IQueryable<User>>
    {
        public UserCollectionType(PlayTogetherDbContext db, IConfiguration config)
        {
            FieldAsync<IntGraphType>("total",
                description: "The total number of users registered",
                resolve: async context =>
                {
                    var total = await db.Users.CountAsync();
                    return total;
                }
            );

            FieldAsync<IntGraphType>("count",
                description: "The number of users selected by the query",
                resolve: async context =>
                {
                    var count = await context.Source.CountAsync();
                    return count;
                }
            );

            FieldAsync<ListGraphType<UserType>>("items",
                resolve: async context => await context.Source.ToListAsync());
        }
    }
}
