using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevStudioDomain.Migrations
{
    /// <inheritdoc />
    public partial class Phase3Kanban : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkItems_ProjectId",
                table: "WorkItems");

            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "WorkItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // Backfill: remap legacy 6-state agent values to the new 5-lane Kanban model.
            migrationBuilder.Sql("UPDATE WorkItems SET Status = 'planned' WHERE Status = 'planning';");
            migrationBuilder.Sql("UPDATE WorkItems SET Status = 'in-progress' WHERE Status = 'executing';");
            migrationBuilder.Sql("UPDATE WorkItems SET Status = 'review' WHERE Status IN ('waiting-for-review', 'in-review');");
            migrationBuilder.Sql(
                "UPDATE WorkItems SET Status = 'backlog' " +
                "WHERE Status NOT IN ('backlog', 'planned', 'in-progress', 'review', 'done');");

            // Seed Order per (ProjectId, Status) lane based on insertion order so existing rows get stable indexes.
            migrationBuilder.Sql(@"
UPDATE WorkItems
SET ""Order"" = (
    SELECT COUNT(*)
    FROM WorkItems AS Inner
    WHERE Inner.ProjectId = WorkItems.ProjectId
      AND Inner.Status = WorkItems.Status
      AND Inner.Id < WorkItems.Id
);");

            migrationBuilder.CreateIndex(
                name: "IX_WorkItems_ProjectId_Status_Order",
                table: "WorkItems",
                columns: new[] { "ProjectId", "Status", "Order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkItems_ProjectId_Status_Order",
                table: "WorkItems");

            migrationBuilder.DropColumn(
                name: "Order",
                table: "WorkItems");

            migrationBuilder.CreateIndex(
                name: "IX_WorkItems_ProjectId",
                table: "WorkItems",
                column: "ProjectId");
        }
    }
}
