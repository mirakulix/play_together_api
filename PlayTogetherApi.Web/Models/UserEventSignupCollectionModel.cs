﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlayTogetherApi.Domain;

namespace PlayTogetherApi.Web.Models
{
    public class UserEventSignupCollectionModel
    {
        public IQueryable<UserEventSignup> TotalSignupsQuery;
        public IQueryable<UserEventSignup> SignupsQuery;
    }
}