﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using PlayTogetherApi.Domain;

namespace PlayTogetherApi.Domain.Migrations
{
    [DbContext(typeof(PlayTogetherDbContext))]
    partial class PlayTogetherDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .HasAnnotation("ProductVersion", "2.2.6-servicing-10079")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("PlayTogetherApi.Domain.Event", b =>
                {
                    b.Property<Guid>("EventId")
                        .ValueGeneratedOnAdd();

                    b.Property<Guid>("CreatedByUserId");

                    b.Property<DateTime>("CreatedDate");

                    b.Property<string>("Description");

                    b.Property<DateTime>("EventDate");

                    b.Property<DateTime>("EventEndDate");

                    b.Property<Guid?>("GameId");

                    b.Property<string>("Title")
                        .HasMaxLength(50);

                    b.HasKey("EventId");

                    b.HasIndex("CreatedByUserId");

                    b.HasIndex("GameId");

                    b.ToTable("Events");
                });

            modelBuilder.Entity("PlayTogetherApi.Domain.Game", b =>
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

            modelBuilder.Entity("PlayTogetherApi.Domain.RefreshToken", b =>
                {
                    b.Property<Guid>("Token")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("CreatedDate");

                    b.Property<DateTime>("ExpirationDate");

                    b.Property<Guid>("UserId");

                    b.HasKey("Token");

                    b.ToTable("RefreshTokens");
                });

            modelBuilder.Entity("PlayTogetherApi.Domain.User", b =>
                {
                    b.Property<Guid>("UserId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("DisplayName")
                        .HasMaxLength(50);

                    b.Property<string>("Email");

                    b.Property<string>("PasswordHash");

                    b.HasKey("UserId");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("PlayTogetherApi.Domain.UserEventSignup", b =>
                {
                    b.Property<Guid>("UserId");

                    b.Property<Guid>("EventId");

                    b.Property<DateTime>("SignupDate");

                    b.Property<int>("Status");

                    b.HasKey("UserId", "EventId");

                    b.HasIndex("EventId");

                    b.ToTable("UserEventSignups");
                });

            modelBuilder.Entity("PlayTogetherApi.Domain.Event", b =>
                {
                    b.HasOne("PlayTogetherApi.Domain.User", "CreatedByUser")
                        .WithMany()
                        .HasForeignKey("CreatedByUserId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("PlayTogetherApi.Domain.Game", "Game")
                        .WithMany()
                        .HasForeignKey("GameId");
                });

            modelBuilder.Entity("PlayTogetherApi.Domain.UserEventSignup", b =>
                {
                    b.HasOne("PlayTogetherApi.Domain.Event", "Event")
                        .WithMany("Signups")
                        .HasForeignKey("EventId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("PlayTogetherApi.Domain.User", "User")
                        .WithMany("Signups")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
#pragma warning restore 612, 618
        }
    }
}
