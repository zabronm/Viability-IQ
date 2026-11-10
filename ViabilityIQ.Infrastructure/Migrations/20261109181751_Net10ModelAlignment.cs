using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViabilityIQ.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Net10ModelAlignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "BranchId",
                table: "tblApplicationUsers",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLogoutAt",
                table: "tblApplicationUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MustChangePassword",
                table: "tblApplicationUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PreviousLoginAt",
                table: "tblApplicationUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ProvinceId",
                table: "tblApplicationUsers",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "tblApplicationUsers");

            migrationBuilder.DropColumn(
                name: "LastLogoutAt",
                table: "tblApplicationUsers");

            migrationBuilder.DropColumn(
                name: "MustChangePassword",
                table: "tblApplicationUsers");

            migrationBuilder.DropColumn(
                name: "PreviousLoginAt",
                table: "tblApplicationUsers");

            migrationBuilder.DropColumn(
                name: "ProvinceId",
                table: "tblApplicationUsers");
        }
    }
}
