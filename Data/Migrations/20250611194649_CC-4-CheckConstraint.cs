using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AspNetCoreMvcTemplate.Data.Migrations
{
    /// <inheritdoc />
    public partial class CC4CheckConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQLite does not support ALTER TABLE for adding CHECK constraints after table creation.
            // As a workaround, we can use a trigger to enforce the constraint.
            migrationBuilder.Sql(@"
                CREATE TRIGGER CostCenter_Level_Check
                BEFORE INSERT ON CostCenters
                FOR EACH ROW
                BEGIN
                    SELECT RAISE(ABORT, 'Level must be less than or equal to 5')
                    WHERE NEW.Level > 5;
                END;
            ");

            migrationBuilder.Sql(@"
                CREATE TRIGGER CostCenter_Level_Update_Check
                BEFORE UPDATE ON CostCenters
                FOR EACH ROW
                BEGIN
                    SELECT RAISE(ABORT, 'Level must be less than or equal to 5')
                    WHERE NEW.Level > 5;
                END;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS CostCenter_Level_Check;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS CostCenter_Level_Update_Check;");
        }
    }
}
