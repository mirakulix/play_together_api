﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Types;
using GameCalendarApi.Domain;

namespace GameCalendarApi.Web.GraphQl.Types
{
    public class UserType : ObjectGraphType<User>
    {
        public UserType()
        {
            Field("id", x => x.UserId, type: typeof(IdGraphType)).Description("Id property from the user object.");
            Field(x => x.DisplayName).Description("DisplayName property from the user object.");
            Field(x => x.Email).Description("Email property from the user object.");
        }
    }
}
