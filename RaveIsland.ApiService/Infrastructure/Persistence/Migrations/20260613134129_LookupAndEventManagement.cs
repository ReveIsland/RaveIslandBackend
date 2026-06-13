using System;
using Microsoft.EntityFrameworkCore.Migrations;
using RaveIsland.ApiService.Infrastructure.Lookups;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RaveIsland.ApiService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LookupAndEventManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Events",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AgeRestrictionId",
                table: "Events",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CancellationPolicyId",
                table: "Events",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntryPolicy",
                table: "Events",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EventCategoryId",
                table: "Events",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "EventStatusId",
                table: "Events",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "InviteCode",
                table: "Events",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaDescription",
                table: "Events",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaTitle",
                table: "Events",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizerReference",
                table: "Events",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PrimaryGenreId",
                table: "Events",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProhibitedItems",
                table: "Events",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresApproval",
                table: "Events",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "SecondaryGenreId",
                table: "Events",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Events",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SoundSystem",
                table: "Events",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tagline",
                table: "Events",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TermsAndConditions",
                table: "Events",
                type: "character varying(8000)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Theme",
                table: "Events",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VenueTypeId",
                table: "Events",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VisibilityTypeId",
                table: "Events",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "EventMedia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    MediaType = table.Column<int>(type: "integer", nullable: false),
                    StorageUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    ThumbnailUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    FileName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventMedia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventMedia_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventPromoCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DiscountType = table.Column<int>(type: "integer", nullable: false),
                    DiscountValue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UsageLimit = table.Column<int>(type: "integer", nullable: true),
                    UsageCount = table.Column<int>(type: "integer", nullable: false),
                    AppliesToTicketTypeIdsJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventPromoCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventPromoCodes_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    DayNumber = table.Column<int>(type: "integer", nullable: false),
                    EventDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    GatesOpenTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    LastEntryTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventSchedules_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LookupTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookupTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LookupValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LookupTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    IconUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    MetadataJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookupValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LookupValues_LookupTypes_LookupTypeId",
                        column: x => x.LookupTypeId,
                        principalTable: "LookupTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Artists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    StageName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Bio = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ProfileImageUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    ArtistTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    PrimaryGenreId = table.Column<Guid>(type: "uuid", nullable: true),
                    SocialLinksJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Artists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Artists_LookupValues_ArtistTypeId",
                        column: x => x.ArtistTypeId,
                        principalTable: "LookupValues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Artists_LookupValues_PrimaryGenreId",
                        column: x => x.PrimaryGenreId,
                        principalTable: "LookupValues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Artists_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventLookupSelections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    LookupValueId = table.Column<Guid>(type: "uuid", nullable: false),
                    LookupTypeCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventLookupSelections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventLookupSelections_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventLookupSelections_LookupValues_LookupValueId",
                        column: x => x.LookupValueId,
                        principalTable: "LookupValues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventTicketTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    QuantitySold = table.Column<int>(type: "integer", nullable: false),
                    SaleStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SaleEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    MaxPerUser = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultLookupValueId = table.Column<Guid>(type: "uuid", nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventTicketTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventTicketTypes_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventTicketTypes_LookupValues_DefaultLookupValueId",
                        column: x => x.DefaultLookupValueId,
                        principalTable: "LookupValues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Venues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    VenueName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Address = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    City = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DistrictId = table.Column<Guid>(type: "uuid", nullable: false),
                    Province = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    GoogleMapsUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    LandmarkInstructions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Venues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Venues_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Venues_LookupValues_DistrictId",
                        column: x => x.DistrictId,
                        principalTable: "LookupValues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventArtists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArtistId = table.Column<Guid>(type: "uuid", nullable: false),
                    StageNameOverride = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    PrimaryGenreId = table.Column<Guid>(type: "uuid", nullable: true),
                    SetStart = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    SetEnd = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventArtists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventArtists_Artists_ArtistId",
                        column: x => x.ArtistId,
                        principalTable: "Artists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventArtists_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventArtists_LookupValues_PrimaryGenreId",
                        column: x => x.PrimaryGenreId,
                        principalTable: "LookupValues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Tickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventTicketTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    QrToken = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    HolderName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    HolderEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsCheckedIn = table.Column<bool>(type: "boolean", nullable: false),
                    CheckedInAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tickets_EventTicketTypes_EventTicketTypeId",
                        column: x => x.EventTicketTypeId,
                        principalTable: "EventTicketTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tickets_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CheckInLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScannedByUserId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ScannedByName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    GateId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ScannedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckInLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CheckInLogs_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CheckInLogs_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "LookupTypes",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "IsSystem", "Name" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-4111-8111-111111110001"), "EventCategory", new DateTimeOffset(new DateTime(2026, 6, 13, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Categories for classifying events", true, "Event Category" },
                    { new Guid("11111111-1111-4111-8111-111111110002"), "MusicGenre", new DateTimeOffset(new DateTime(2026, 6, 13, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Music genres for events and artists", true, "Music Genre" },
                    { new Guid("11111111-1111-4111-8111-111111110003"), "VenueType", new DateTimeOffset(new DateTime(2026, 6, 13, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Types of event venues", true, "Venue Type" },
                    { new Guid("11111111-1111-4111-8111-111111110004"), "Facility", new DateTimeOffset(new DateTime(2026, 6, 13, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Event facilities and amenities", true, "Facility" },
                    { new Guid("11111111-1111-4111-8111-111111110005"), "ProductionFeature", new DateTimeOffset(new DateTime(2026, 6, 13, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Production and visual features", true, "Production Feature" },
                    { new Guid("11111111-1111-4111-8111-111111110006"), "TicketType", new DateTimeOffset(new DateTime(2026, 6, 13, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Default ticket type labels", true, "Ticket Type" },
                    { new Guid("11111111-1111-4111-8111-111111110007"), "AgeRestriction", new DateTimeOffset(new DateTime(2026, 6, 13, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Age restrictions for events", true, "Age Restriction" },
                    { new Guid("11111111-1111-4111-8111-111111110008"), "EventVisibility", new DateTimeOffset(new DateTime(2026, 6, 13, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Event visibility types", true, "Event Visibility" },
                    { new Guid("11111111-1111-4111-8111-111111110009"), "EventStatus", new DateTimeOffset(new DateTime(2026, 6, 13, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Event lifecycle statuses", true, "Event Status" },
                    { new Guid("11111111-1111-4111-8111-11111111000a"), "PaymentMethod", new DateTimeOffset(new DateTime(2026, 6, 13, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Supported payment methods", true, "Payment Method" },
                    { new Guid("11111111-1111-4111-8111-11111111000b"), "District", new DateTimeOffset(new DateTime(2026, 6, 13, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Sri Lanka districts", true, "District" },
                    { new Guid("11111111-1111-4111-8111-11111111000c"), "ArtistType", new DateTimeOffset(new DateTime(2026, 6, 13, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Types of performers", true, "Artist Type" },
                    { new Guid("11111111-1111-4111-8111-11111111000d"), "SocialMediaProvider", new DateTimeOffset(new DateTime(2026, 6, 13, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Social media platforms", true, "Social Media Provider" },
                    { new Guid("11111111-1111-4111-8111-11111111000e"), "CancellationPolicy", new DateTimeOffset(new DateTime(2026, 6, 13, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Refund and cancellation policies", true, "Cancellation Policy" }
                });

            foreach (var typeDef in LookupSeedData.Types)
            {
                foreach (var valueDef in typeDef.Values)
                {
                    migrationBuilder.InsertData(
                        table: "LookupValues",
                        columns: ["Id", "LookupTypeId", "Code", "Name", "DisplayOrder", "IsActive", "IsSystem"],
                        values: [
                            LookupSeeder.CreateDeterministicGuid(typeDef.Id, valueDef.Code),
                            typeDef.Id,
                            valueDef.Code,
                            valueDef.Name,
                            valueDef.DisplayOrder,
                            true,
                            true]);
                }
            }

            migrationBuilder.Sql("""
                UPDATE "Events" SET "Description" = COALESCE("Description", '');
                UPDATE "Events" SET "EventCategoryId" = (SELECT "Id" FROM "LookupValues" WHERE "LookupTypeId" = '11111111-1111-4111-8111-111111110001' AND "Code" = 'Rave')
                WHERE "EventCategoryId" = '00000000-0000-0000-0000-000000000000';
                UPDATE "Events" SET "EventStatusId" = (SELECT "Id" FROM "LookupValues" WHERE "LookupTypeId" = '11111111-1111-4111-8111-111111110009' AND "Code" = 'Draft')
                WHERE "EventStatusId" = '00000000-0000-0000-0000-000000000000';
                UPDATE "Events" SET "VisibilityTypeId" = (SELECT "Id" FROM "LookupValues" WHERE "LookupTypeId" = '11111111-1111-4111-8111-111111110008' AND "Code" = 'Public')
                WHERE "VisibilityTypeId" = '00000000-0000-0000-0000-000000000000';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Events_AgeRestrictionId",
                table: "Events",
                column: "AgeRestrictionId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_CancellationPolicyId",
                table: "Events",
                column: "CancellationPolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_EventCategoryId",
                table: "Events",
                column: "EventCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_EventStatusId",
                table: "Events",
                column: "EventStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_PrimaryGenreId",
                table: "Events",
                column: "PrimaryGenreId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_SecondaryGenreId",
                table: "Events",
                column: "SecondaryGenreId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_Slug",
                table: "Events",
                column: "Slug");

            migrationBuilder.CreateIndex(
                name: "IX_Events_VenueTypeId",
                table: "Events",
                column: "VenueTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_VisibilityTypeId",
                table: "Events",
                column: "VisibilityTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Artists_ArtistTypeId",
                table: "Artists",
                column: "ArtistTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Artists_PrimaryGenreId",
                table: "Artists",
                column: "PrimaryGenreId");

            migrationBuilder.CreateIndex(
                name: "IX_Artists_TenantId",
                table: "Artists",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CheckInLogs_EventId",
                table: "CheckInLogs",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_CheckInLogs_TicketId",
                table: "CheckInLogs",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_EventArtists_ArtistId",
                table: "EventArtists",
                column: "ArtistId");

            migrationBuilder.CreateIndex(
                name: "IX_EventArtists_EventId_ArtistId",
                table: "EventArtists",
                columns: new[] { "EventId", "ArtistId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventArtists_PrimaryGenreId",
                table: "EventArtists",
                column: "PrimaryGenreId");

            migrationBuilder.CreateIndex(
                name: "IX_EventLookupSelections_EventId_LookupValueId",
                table: "EventLookupSelections",
                columns: new[] { "EventId", "LookupValueId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventLookupSelections_LookupValueId",
                table: "EventLookupSelections",
                column: "LookupValueId");

            migrationBuilder.CreateIndex(
                name: "IX_EventMedia_EventId",
                table: "EventMedia",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventPromoCodes_EventId_Code",
                table: "EventPromoCodes",
                columns: new[] { "EventId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventSchedules_EventId_DayNumber",
                table: "EventSchedules",
                columns: new[] { "EventId", "DayNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventTicketTypes_DefaultLookupValueId",
                table: "EventTicketTypes",
                column: "DefaultLookupValueId");

            migrationBuilder.CreateIndex(
                name: "IX_EventTicketTypes_EventId",
                table: "EventTicketTypes",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_LookupTypes_Code",
                table: "LookupTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LookupValues_LookupTypeId_Code",
                table: "LookupValues",
                columns: new[] { "LookupTypeId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_EventId",
                table: "Tickets",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_EventTicketTypeId",
                table: "Tickets",
                column: "EventTicketTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_QrToken",
                table: "Tickets",
                column: "QrToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Venues_DistrictId",
                table: "Venues",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_Venues_EventId",
                table: "Venues",
                column: "EventId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Events_LookupValues_AgeRestrictionId",
                table: "Events",
                column: "AgeRestrictionId",
                principalTable: "LookupValues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Events_LookupValues_CancellationPolicyId",
                table: "Events",
                column: "CancellationPolicyId",
                principalTable: "LookupValues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Events_LookupValues_EventCategoryId",
                table: "Events",
                column: "EventCategoryId",
                principalTable: "LookupValues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Events_LookupValues_EventStatusId",
                table: "Events",
                column: "EventStatusId",
                principalTable: "LookupValues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Events_LookupValues_PrimaryGenreId",
                table: "Events",
                column: "PrimaryGenreId",
                principalTable: "LookupValues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Events_LookupValues_SecondaryGenreId",
                table: "Events",
                column: "SecondaryGenreId",
                principalTable: "LookupValues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Events_LookupValues_VenueTypeId",
                table: "Events",
                column: "VenueTypeId",
                principalTable: "LookupValues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Events_LookupValues_VisibilityTypeId",
                table: "Events",
                column: "VisibilityTypeId",
                principalTable: "LookupValues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Events_LookupValues_AgeRestrictionId",
                table: "Events");

            migrationBuilder.DropForeignKey(
                name: "FK_Events_LookupValues_CancellationPolicyId",
                table: "Events");

            migrationBuilder.DropForeignKey(
                name: "FK_Events_LookupValues_EventCategoryId",
                table: "Events");

            migrationBuilder.DropForeignKey(
                name: "FK_Events_LookupValues_EventStatusId",
                table: "Events");

            migrationBuilder.DropForeignKey(
                name: "FK_Events_LookupValues_PrimaryGenreId",
                table: "Events");

            migrationBuilder.DropForeignKey(
                name: "FK_Events_LookupValues_SecondaryGenreId",
                table: "Events");

            migrationBuilder.DropForeignKey(
                name: "FK_Events_LookupValues_VenueTypeId",
                table: "Events");

            migrationBuilder.DropForeignKey(
                name: "FK_Events_LookupValues_VisibilityTypeId",
                table: "Events");

            migrationBuilder.DropTable(
                name: "CheckInLogs");

            migrationBuilder.DropTable(
                name: "EventArtists");

            migrationBuilder.DropTable(
                name: "EventLookupSelections");

            migrationBuilder.DropTable(
                name: "EventMedia");

            migrationBuilder.DropTable(
                name: "EventPromoCodes");

            migrationBuilder.DropTable(
                name: "EventSchedules");

            migrationBuilder.DropTable(
                name: "Venues");

            migrationBuilder.DropTable(
                name: "Tickets");

            migrationBuilder.DropTable(
                name: "Artists");

            migrationBuilder.DropTable(
                name: "EventTicketTypes");

            migrationBuilder.DropTable(
                name: "LookupValues");

            migrationBuilder.DropTable(
                name: "LookupTypes");

            migrationBuilder.DropIndex(
                name: "IX_Events_AgeRestrictionId",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_CancellationPolicyId",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_EventCategoryId",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_EventStatusId",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_PrimaryGenreId",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_SecondaryGenreId",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_Slug",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_VenueTypeId",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_VisibilityTypeId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "AgeRestrictionId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "CancellationPolicyId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "EntryPolicy",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "EventCategoryId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "EventStatusId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "InviteCode",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "MetaDescription",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "MetaTitle",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "OrganizerReference",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "PrimaryGenreId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "ProhibitedItems",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "RequiresApproval",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "SecondaryGenreId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "SoundSystem",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Tagline",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "TermsAndConditions",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Theme",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "VenueTypeId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "VisibilityTypeId",
                table: "Events");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Events",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000);
        }
    }
}
