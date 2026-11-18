using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoctorsAppointment.Api.Migrations
{
    /// <inheritdoc />
    public partial class PreventOverlappingAppointments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
            CREATE EXTENSION IF NOT EXISTS btree_gist;

            ALTER TABLE "Appointments"
            ADD CONSTRAINT "EX_Appointments_Doctor_NoOverlap"
            EXCLUDE USING gist
            (
                "DoctorId" WITH =,

                (tsrange(
                    "StartTime",
                    "StartTime" +
                    CASE "Type"
                        WHEN 0 THEN INTERVAL '30 minutes'
                        WHEN 1 THEN INTERVAL '15 minutes'
                        WHEN 2 THEN INTERVAL '60 minutes'
                    END,
                    '[)'
                )) WITH &&
            )
            WHERE ("IsCancelled" = false);
            """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Appointments"
                DROP CONSTRAINT "EX_Appointments_Doctor_NoOverlap";
                """);
        }
    }
}
