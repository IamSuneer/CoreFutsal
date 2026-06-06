using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreFutsal.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokensAndProposals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StadiumMatchProposals",
                columns: table => new
                {
                    ProposalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StadiumId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SlotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HomeTeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AwayTeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HomeTeamStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AwayTeamStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HomeTeamRespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AwayTeamRespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StadiumMatchProposals", x => x.ProposalId);
                    table.ForeignKey(
                        name: "FK_StadiumMatchProposals_StadiumSlots_SlotId",
                        column: x => x.SlotId,
                        principalTable: "StadiumSlots",
                        principalColumn: "SlotId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StadiumMatchProposals_Stadiums_StadiumId",
                        column: x => x.StadiumId,
                        principalTable: "Stadiums",
                        principalColumn: "StadiumId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StadiumMatchProposals_Teams_AwayTeamId",
                        column: x => x.AwayTeamId,
                        principalTable: "Teams",
                        principalColumn: "TeamId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StadiumMatchProposals_Teams_HomeTeamId",
                        column: x => x.HomeTeamId,
                        principalTable: "Teams",
                        principalColumn: "TeamId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StadiumMatchProposals_AwayTeamId",
                table: "StadiumMatchProposals",
                column: "AwayTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_StadiumMatchProposals_HomeTeamId",
                table: "StadiumMatchProposals",
                column: "HomeTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_StadiumMatchProposals_SlotId",
                table: "StadiumMatchProposals",
                column: "SlotId");

            migrationBuilder.CreateIndex(
                name: "IX_StadiumMatchProposals_StadiumId",
                table: "StadiumMatchProposals",
                column: "StadiumId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "StadiumMatchProposals");
        }
    }
}
