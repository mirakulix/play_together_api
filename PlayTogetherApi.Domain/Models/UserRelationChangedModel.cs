﻿using System;
using PlayTogetherApi.Data;

namespace PlayTogetherApi.Models
{
    public class UserRelationChangedModel
    {
        public UserRelation Relation;

        public User ActiveUser;
        public UserRelationAction ActiveUserAction;
        //public UserRelationStatus? ActiveUserPreviousStatus;

        public User TargetUser;
        //public UserRelationStatus? TargetUserPreviousStatus;
    }

    public class UserRelationChangedExtModel : UserRelationChangedModel
    {
        public Guid SubscribingUserId;

        public UserRelationChangedExtModel(UserRelationChangedModel model, Guid userId)
        {
            SubscribingUserId = userId;
            Relation = model.Relation;
            ActiveUser = model.ActiveUser;
            ActiveUserAction = model.ActiveUserAction;
            TargetUser = model.TargetUser;
        }
    }
}
