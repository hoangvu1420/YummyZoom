using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YummyZoom.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditAndSoftDeleteProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Created",
                table: "UserAddresses");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "UserAddresses");

            migrationBuilder.DropColumn(
                name: "LastModified",
                table: "UserAddresses");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "UserAddresses");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "RoleAssignments");

            migrationBuilder.DropColumn(
                name: "LastModified",
                table: "RoleAssignments");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "RoleAssignments");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "RoleAssignments");

            migrationBuilder.AlterColumn<string>(
                name: "LastModifiedBy",
                table: "TodoLists",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                comment: "Identifier of who last modified the entity",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "LastModified",
                table: "TodoLists",
                type: "timestamp with time zone",
                nullable: false,
                comment: "Timestamp when the entity was last modified",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "TodoLists",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                comment: "Identifier of who created the entity",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "Created",
                table: "TodoLists",
                type: "timestamp with time zone",
                nullable: false,
                comment: "Timestamp when the entity was created",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "RoleAssignments",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                comment: "Identifier of who created the entity",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "Created",
                table: "RoleAssignments",
                type: "timestamp with time zone",
                nullable: false,
                comment: "Timestamp when the entity was created",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "LastModifiedBy",
                table: "DomainUsers",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                comment: "Identifier of who last modified the entity",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "LastModified",
                table: "DomainUsers",
                type: "timestamp with time zone",
                nullable: false,
                comment: "Timestamp when the entity was last modified",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "DomainUsers",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                comment: "Identifier of who created the entity",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "Created",
                table: "DomainUsers",
                type: "timestamp with time zone",
                nullable: false,
                comment: "Timestamp when the entity was created",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "DomainUsers",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                comment: "Identifier of who deleted the entity");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedOn",
                table: "DomainUsers",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Timestamp when the entity was deleted");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "DomainUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "Indicates if the entity is soft-deleted");

            migrationBuilder.CreateIndex(
                name: "IX_TodoList_Created",
                table: "TodoLists",
                column: "Created");

            migrationBuilder.CreateIndex(
                name: "IX_TodoList_LastModified",
                table: "TodoLists",
                column: "LastModified");

            migrationBuilder.CreateIndex(
                name: "IX_RoleAssignment_Created",
                table: "RoleAssignments",
                column: "Created");

            migrationBuilder.CreateIndex(
                name: "IX_User_Created",
                table: "DomainUsers",
                column: "Created");

            migrationBuilder.CreateIndex(
                name: "IX_User_DeletedOn",
                table: "DomainUsers",
                column: "DeletedOn");

            migrationBuilder.CreateIndex(
                name: "IX_User_IsDeleted",
                table: "DomainUsers",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_User_LastModified",
                table: "DomainUsers",
                column: "LastModified");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TodoList_Created",
                table: "TodoLists");

            migrationBuilder.DropIndex(
                name: "IX_TodoList_LastModified",
                table: "TodoLists");

            migrationBuilder.DropIndex(
                name: "IX_RoleAssignment_Created",
                table: "RoleAssignments");

            migrationBuilder.DropIndex(
                name: "IX_User_Created",
                table: "DomainUsers");

            migrationBuilder.DropIndex(
                name: "IX_User_DeletedOn",
                table: "DomainUsers");

            migrationBuilder.DropIndex(
                name: "IX_User_IsDeleted",
                table: "DomainUsers");

            migrationBuilder.DropIndex(
                name: "IX_User_LastModified",
                table: "DomainUsers");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "DomainUsers");

            migrationBuilder.DropColumn(
                name: "DeletedOn",
                table: "DomainUsers");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "DomainUsers");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "Created",
                table: "UserAddresses",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "UserAddresses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastModified",
                table: "UserAddresses",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                table: "UserAddresses",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LastModifiedBy",
                table: "TodoLists",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true,
                oldComment: "Identifier of who last modified the entity");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "LastModified",
                table: "TodoLists",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldComment: "Timestamp when the entity was last modified");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "TodoLists",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true,
                oldComment: "Identifier of who created the entity");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "Created",
                table: "TodoLists",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldComment: "Timestamp when the entity was created");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "RoleAssignments",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true,
                oldComment: "Identifier of who created the entity");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "Created",
                table: "RoleAssignments",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldComment: "Timestamp when the entity was created");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "RoleAssignments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastModified",
                table: "RoleAssignments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                table: "RoleAssignments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "RoleAssignments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LastModifiedBy",
                table: "DomainUsers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true,
                oldComment: "Identifier of who last modified the entity");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "LastModified",
                table: "DomainUsers",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldComment: "Timestamp when the entity was last modified");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "DomainUsers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true,
                oldComment: "Identifier of who created the entity");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "Created",
                table: "DomainUsers",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldComment: "Timestamp when the entity was created");
        }
    }
}
