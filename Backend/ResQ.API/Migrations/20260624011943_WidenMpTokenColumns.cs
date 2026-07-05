using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResQ.API.Migrations
{
    /// <inheritdoc />
    public partial class WidenMpTokenColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "AccessToken",
                table: "MerchantMpCredentials",
                type: "character varying(600)",
                maxLength: 600,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "RefreshToken",
                table: "MerchantMpCredentials",
                type: "character varying(600)",
                maxLength: 600,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Scope",
                table: "MerchantMpCredentials",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "AccessToken",
                table: "MerchantMpCredentials",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(600)",
                oldMaxLength: 600);

            migrationBuilder.AlterColumn<string>(
                name: "RefreshToken",
                table: "MerchantMpCredentials",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(600)",
                oldMaxLength: 600);

            migrationBuilder.AlterColumn<string>(
                name: "Scope",
                table: "MerchantMpCredentials",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
