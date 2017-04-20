using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ContosoUniversity.Migrations
{
    public partial class Inheritance : Migration
    {
        // Modified Up method
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //Removes foreign key constraints and indexes that point to the Student table.
            migrationBuilder.DropForeignKey(
                name: "FK_Enrollment_Student_StudentID",
                table: "Enrollment");

            migrationBuilder.DropIndex(name: "IX_Enrollment_StudentID", table: "Enrollment");

            // Renames the Instructor table as Person 
            migrationBuilder.RenameTable(name: "Instructor", newName: "Person");
            // Make changes needed for Person to store Student data:
            migrationBuilder.AddColumn<DateTime>(name: "EnrollmentDate", table: "Person", nullable: true);
            migrationBuilder.AddColumn<string>(name: "Discriminator", table: "Person", nullable: false, maxLength: 128, defaultValue: "Instructor");
            // Make HireDate nullable since student rows won't have hire dates.
            migrationBuilder.AlterColumn<DateTime>(name: "HireDate", table: "Person", nullable: true);

            // Adds a temporary column that will be used to update foreign keys 
            // that point to students. When you copy students into the Person table they'll get new primary key values.
            migrationBuilder.AddColumn<int>(name: "OldId", table: "Person", nullable: true);

            // Copy existing Student data into new Person table.
            // This causes students to get assigned new primary key values.
            migrationBuilder.Sql("INSERT INTO dbo.Person (LastName, FirstName, HireDate, EnrollmentDate, Discriminator, OldId) SELECT LastName, FirstName, null AS HireDate, EnrollmentDate, 'Student' AS Discriminator, ID AS OldId FROM dbo.Student");
            // Fix up existing relationships to match new PK's.
            migrationBuilder.Sql("UPDATE dbo.Enrollment SET StudentId = (SELECT ID FROM dbo.Person WHERE OldId = Enrollment.StudentId AND Discriminator = 'Student')");

            // Remove temporary key
            migrationBuilder.DropColumn(name: "OldID", table: "Person");

            migrationBuilder.DropTable(
                name: "Student");

            // Re-creates foreign key constraints and indexes, now pointing them to the Person table.
            migrationBuilder.CreateIndex(
                 name: "IX_Enrollment_StudentID",
                 table: "Enrollment",
                 column: "StudentID");

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollment_Person_StudentID",
                table: "Enrollment",
                column: "StudentID",
                principalTable: "Person",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }
        // Generated Up method
        //protected override void Up(MigrationBuilder migrationBuilder)
        //{
        //    migrationBuilder.DropForeignKey(
        //        name: "FK_CourseAssignment_Instructor_InstructorID",
        //        table: "CourseAssignment");

        //    migrationBuilder.DropForeignKey(
        //        name: "FK_Department_Instructor_InstructorID",
        //        table: "Department");

        //    migrationBuilder.DropForeignKey(
        //        name: "FK_Enrollment_Student_StudentID",
        //        table: "Enrollment");

        //    migrationBuilder.DropForeignKey(
        //        name: "FK_OfficeAssignment_Instructor_InstructorID",
        //        table: "OfficeAssignment");

        //    migrationBuilder.DropTable(
        //        name: "Instructor");

        //    migrationBuilder.DropTable(
        //        name: "Student");

        //    migrationBuilder.CreateTable(
        //        name: "Person",
        //        columns: table => new
        //        {
        //            ID = table.Column<int>(nullable: false)
        //                .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
        //            Discriminator = table.Column<string>(nullable: false),
        //            FirstName = table.Column<string>(maxLength: 50, nullable: false),
        //            LastName = table.Column<string>(maxLength: 50, nullable: false),
        //            HireDate = table.Column<DateTime>(nullable: true),
        //            EnrollmentDate = table.Column<DateTime>(nullable: true)
        //        },
        //        constraints: table =>
        //        {
        //            table.PrimaryKey("PK_Person", x => x.ID);
        //        });

        //    migrationBuilder.AddForeignKey(
        //        name: "FK_CourseAssignment_Person_InstructorID",
        //        table: "CourseAssignment",
        //        column: "InstructorID",
        //        principalTable: "Person",
        //        principalColumn: "ID",
        //        onDelete: ReferentialAction.Cascade);

        //    migrationBuilder.AddForeignKey(
        //        name: "FK_Department_Person_InstructorID",
        //        table: "Department",
        //        column: "InstructorID",
        //        principalTable: "Person",
        //        principalColumn: "ID",
        //        onDelete: ReferentialAction.Restrict);

        //    migrationBuilder.AddForeignKey(
        //        name: "FK_Enrollment_Person_StudentID",
        //        table: "Enrollment",
        //        column: "StudentID",
        //        principalTable: "Person",
        //        principalColumn: "ID",
        //        onDelete: ReferentialAction.Cascade);

        //    migrationBuilder.AddForeignKey(
        //        name: "FK_OfficeAssignment_Person_InstructorID",
        //        table: "OfficeAssignment",
        //        column: "InstructorID",
        //        principalTable: "Person",
        //        principalColumn: "ID",
        //        onDelete: ReferentialAction.Cascade);
        //}

        // Note:  The Dowm method should be modified BUT the tutorial did not provide it.
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseAssignment_Person_InstructorID",
                table: "CourseAssignment");

            migrationBuilder.DropForeignKey(
                name: "FK_Department_Person_InstructorID",
                table: "Department");

            migrationBuilder.DropForeignKey(
                name: "FK_Enrollment_Person_StudentID",
                table: "Enrollment");

            migrationBuilder.DropForeignKey(
                name: "FK_OfficeAssignment_Person_InstructorID",
                table: "OfficeAssignment");

            migrationBuilder.DropTable(
                name: "Person");

            migrationBuilder.CreateTable(
                name: "Instructor",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    FirstName = table.Column<string>(maxLength: 50, nullable: false),
                    HireDate = table.Column<DateTime>(nullable: false),
                    LastName = table.Column<string>(maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Instructor", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Student",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    EnrollmentDate = table.Column<DateTime>(nullable: false),
                    FirstName = table.Column<string>(maxLength: 50, nullable: false),
                    LastName = table.Column<string>(maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Student", x => x.ID);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_CourseAssignment_Instructor_InstructorID",
                table: "CourseAssignment",
                column: "InstructorID",
                principalTable: "Instructor",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Department_Instructor_InstructorID",
                table: "Department",
                column: "InstructorID",
                principalTable: "Instructor",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollment_Student_StudentID",
                table: "Enrollment",
                column: "StudentID",
                principalTable: "Student",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OfficeAssignment_Instructor_InstructorID",
                table: "OfficeAssignment",
                column: "InstructorID",
                principalTable: "Instructor",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
