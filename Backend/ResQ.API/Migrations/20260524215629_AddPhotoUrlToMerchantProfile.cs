using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResQ.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPhotoUrlToMerchantProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhotoUrl",
                table: "MerchantProfiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhotoUrl",
                table: "MerchantProfiles");
        }
    }
}
