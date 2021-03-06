﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using PlayTogetherApi.Data;

namespace PlayTogetherApi.Data.Migrations
{
    [DbContext(typeof(PlayTogetherDbContext))]
    [Migration("20200406203623_Update12")]
    partial class Update12
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .HasAnnotation("ProductVersion", "2.2.6-servicing-10079")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("PlayTogetherApi.Data.BuiltinAvatar", b =>
                {
                    b.Property<int>("AvatarId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("ImagePath")
                        .HasMaxLength(100);

                    b.HasKey("AvatarId");

                    b.ToTable("Avatars");
                });

            modelBuilder.Entity("PlayTogetherApi.Data.Event", b =>
                {
                    b.Property<Guid>("EventId")
                        .ValueGeneratedOnAdd();

                    b.Property<bool>("CallToArms");

                    b.Property<Guid>("CreatedByUserId");

                    b.Property<DateTime>("CreatedDate");

                    b.Property<string>("Description");

                    b.Property<DateTime>("EventDate");

                    b.Property<DateTime>("EventEndDate");

                    b.Property<bool>("FriendsOnly");

                    b.Property<Guid?>("GameId");

                    b.Property<string>("Title")
                        .HasMaxLength(50);

                    b.HasKey("EventId");

                    b.HasIndex("CreatedByUserId");

                    b.HasIndex("GameId");

                    b.ToTable("Events");
                });

            modelBuilder.Entity("PlayTogetherApi.Data.Game", b =>
                {
                    b.Property<Guid>("GameId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("ImagePath")
                        .HasMaxLength(100);

                    b.Property<string>("Title")
                        .HasMaxLength(50);

                    b.HasKey("GameId");

                    b.ToTable("Games");
                });

            modelBuilder.Entity("PlayTogetherApi.Data.RefreshToken", b =>
                {
                    b.Property<Guid>("Token")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("CreatedDate");

                    b.Property<DateTime>("ExpirationDate");

                    b.Property<Guid>("UserId");

                    b.HasKey("Token");

                    b.ToTable("RefreshTokens");
                });

            modelBuilder.Entity("PlayTogetherApi.Data.User", b =>
                {
                    b.Property<Guid>("UserId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("AvatarFilename");

                    b.Property<int>("DisplayId");

                    b.Property<string>("DisplayName")
                        .HasMaxLength(50);

                    b.Property<string>("Email");

                    b.Property<string>("PasswordHash");

                    b.Property<TimeSpan?>("UtcOffset");

                    b.HasKey("UserId");

                    b.HasIndex("DisplayName", "DisplayId")
                        .IsUnique();

                    b.ToTable("Users");
                });

            modelBuilder.Entity("PlayTogetherApi.Data.UserEventSignup", b =>
                {
                    b.Property<Guid>("UserId");

                    b.Property<Guid>("EventId");

                    b.Property<DateTime>("SignupDate");

                    b.Property<int>("Status");

                    b.HasKey("UserId", "EventId");

                    b.HasIndex("EventId");

                    b.ToTable("UserEventSignups");
                });

            modelBuilder.Entity("PlayTogetherApi.Data.UserRelation", b =>
                {
                    b.Property<Guid>("UserAId");

                    b.Property<Guid>("UserBId");

                    b.Property<DateTime>("CreatedDate");

                    b.Property<int>("Status");

                    b.HasKey("UserAId", "UserBId");

                    b.HasIndex("UserBId");

                    b.ToTable("UserRelations");
                });

            modelBuilder.Entity("PlayTogetherApi.Data.Event", b =>
                {
                    b.HasOne("PlayTogetherApi.Data.User", "CreatedByUser")
                        .WithMany()
                        .HasForeignKey("CreatedByUserId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("PlayTogetherApi.Data.Game", "Game")
                        .WithMany()
                        .HasForeignKey("GameId");
                });

            modelBuilder.Entity("PlayTogetherApi.Data.UserEventSignup", b =>
                {
                    b.HasOne("PlayTogetherApi.Data.Event", "Event")
                        .WithMany("Signups")
                        .HasForeignKey("EventId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("PlayTogetherApi.Data.User", "User")
                        .WithMany("Signups")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("PlayTogetherApi.Data.UserRelation", b =>
                {
                    b.HasOne("PlayTogetherApi.Data.User", "UserA")
                        .WithMany()
                        .HasForeignKey("UserAId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("PlayTogetherApi.Data.User", "UserB")
                        .WithMany()
                        .HasForeignKey("UserBId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
#pragma warning restore 612, 618
        }
    }
}
