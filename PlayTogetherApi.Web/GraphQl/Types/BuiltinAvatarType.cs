﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using GraphQL.Types;
using PlayTogetherApi.Data;
using PlayTogetherApi.Web.Models;

namespace PlayTogetherApi.Web.GraphQl.Types
{
    public class BuiltinAvatarType : ObjectGraphType<BuiltinAvatar>
    {
        public BuiltinAvatarType(PlayTogetherDbContext db, IConfiguration config)
        {
            Field("id", avatar => avatar.AvatarId, type: typeof(IntGraphType));
            Field("filename", avatar => avatar.ImagePath, type: typeof(StringGraphType));
            Field("url", avatar => config.GetValue<string>("AssetPath") + avatar.ImagePath, type: typeof(StringGraphType));
        }
    }
}
