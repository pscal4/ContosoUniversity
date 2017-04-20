using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ContosoUniversity.Data;
using ContosoUniversity.Models;
using ContosoUniversity.Models.SchoolViewModels;

namespace ContosoUniversity.Controllers
{
    public class InstructorsController : Controller
    {
        private readonly SchoolContext _context;

        public InstructorsController(SchoolContext context)
        {
            _context = context;    
        }

        // GET: Instructors
        public async Task<IActionResult> Index(int? id, int? courseID)
        {
            // This is "Eager Loading"
            //// PJS NOTE: I think this is very inefficient. It loads the whole set of data (course list, enrollment list)
            ////      for all instructor and all of the instructors courses. 
            //// The "main" part of the view shows all instructors and all courses 
            //// however the enrollment portion only shows enrollments for one course so getting all if inefficient!
            //var viewModel = new InstructorIndexData();
            //viewModel.Instructors = await _context.Instructors
            //      .Include(i => i.OfficeAssignment)
            //      .Include(i => i.CourseAssignments)
            //        .ThenInclude(i => i.Course)
            //            .ThenInclude(i => i.Enrollments)
            //                .ThenInclude(i => i.Student)
            //      .Include(i => i.CourseAssignments)
            //        .ThenInclude(i => i.Course)
            //            .ThenInclude(i => i.Department)
            //      .AsNoTracking()
            //      .OrderBy(i => i.LastName)
            //      .ToListAsync();

            //// The selected instructor is retrieved from the list of instructors in the view model.
            //// The view model's Courses property is then loaded with the Course entities from that 
            //// instructor's CourseAssignments navigation property.
            //if (id != null)
            //{
            //    ViewData["InstructorID"] = id.Value;            
            //    Instructor instructor = viewModel.Instructors.Where(
            //        i => i.ID == id.Value).Single();
            //    viewModel.Courses = instructor.CourseAssignments.Select(s => s.Course);
            //}

            //// If a course was selected, the selected course is retrieved from the list of courses 
            //// in the view model.Then the view model's Enrollments property is loaded with the Enrollment 
            //// entities from that course's Enrollments navigation property.
            //if (courseID != null)
            //{
            //    ViewData["CourseID"] = courseID.Value;
            //    viewModel.Enrollments = viewModel.Courses.Where(
            //        x => x.CourseID == courseID).Single().Enrollments;
            //}

            // This is "Explicit Loading" for the enrollments
            // The query below only gets all course assigments for all instructcors
            // as well as the office location
            var viewModel = new InstructorIndexData();
            viewModel.Instructors = await _context.Instructors
                  .Include(i => i.OfficeAssignment)
                  .Include(i => i.CourseAssignments)
                    .ThenInclude(i => i.Course)
                        .ThenInclude(i => i.Department)
                  .OrderBy(i => i.LastName)
                  .ToListAsync();

            if (id != null)
            {
                ViewData["InstructorID"] = id.Value;
                Instructor instructor = viewModel.Instructors.Where(
                    i => i.ID == id.Value).Single();
                viewModel.Courses = instructor.CourseAssignments.Select(s => s.Course);
            }

            // Retrieves Enrollment entities for the selected course, and Student entities for each Enrollment
            // PJS NOTE - I feel like there is a way to do this without the foreach so one retrieve for all 
            // Enrollment/Student data (i.e. the whole IEnumerable<Enrollment> in the view model)
            if (courseID != null)
            {
                ViewData["CourseID"] = courseID.Value;
                var selectedCourse = viewModel.Courses.Where(x => x.CourseID == courseID).Single();
                await _context.Entry(selectedCourse).Collection(x => x.Enrollments).LoadAsync();
                foreach (Enrollment enrollment in selectedCourse.Enrollments)
                {
                    await _context.Entry(enrollment).Reference(x => x.Student).LoadAsync();
                }
                viewModel.Enrollments = selectedCourse.Enrollments;
            }


            return View(viewModel);
        }
        // GET: Instructors/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var instructor = await _context.Instructors
                .SingleOrDefaultAsync(m => m.ID == id);
            if (instructor == null)
            {
                return NotFound();
            }

            return View(instructor);
        }

        public IActionResult Create()
        {
            var instructor = new Instructor();
            instructor.CourseAssignments = new List<CourseAssignment>();
            // The HttpGet Create method calls the PopulateAssignedCourseData method not because there might be courses selected 
            // but in order to provide an empty collection for the foreach loop in the view 
            PopulateAssignedCourseData(instructor);
            return View();
        }

        // POST: Instructors/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FirstMidName,HireDate,LastName,OfficeAssignment")] Instructor instructor, string[] selectedCourses)
        {
            if (selectedCourses != null)
            {
                // Adds each selected course to the CourseAssignments navigation property
                // Courses are added even if there are model errors so if  page is redisplayed 
                // with an error message, any course selections that were made are automatically restored
                instructor.CourseAssignments = new List<CourseAssignment>();
                foreach (var course in selectedCourses)
                {
                    var courseToAdd = new CourseAssignment { InstructorID = instructor.ID, CourseID = int.Parse(course) };
                    instructor.CourseAssignments.Add(courseToAdd);
                }
            }
            if (ModelState.IsValid)
            {
                _context.Add(instructor);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(instructor);
        }

        // GET: Instructors/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Added Include for office assignment and no tracking
            // Later added eager loading of CourseAssigments/Course
            var instructor = await _context.Instructors
                .Include(i => i.OfficeAssignment)
                .Include(i => i.CourseAssignments).ThenInclude(i => i.Course)
                .AsNoTracking()
                .SingleOrDefaultAsync(m => m.ID == id);
            if (instructor == null)
            {
                return NotFound();
            }
            PopulateAssignedCourseData(instructor);
            return View(instructor);
        }
        private void PopulateAssignedCourseData(Instructor instructor)
        {
            var allCourses = _context.Courses;
            var instructorCourses = new HashSet<int>(instructor.CourseAssignments.Select(c => c.Course.CourseID));
            var viewModel = new List<AssignedCourseData>();
            foreach (var course in allCourses)
            {
                viewModel.Add(new AssignedCourseData
                {
                    CourseID = course.CourseID,
                    Title = course.Title,
                    Assigned = instructorCourses.Contains(course.CourseID)
                });
            }
            ViewData["Courses"] = viewModel;
        }

        // Scaffold code for edit
        // POST: Instructors/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> EditPost(int id, [Bind("ID,LastName,FirstMidName,HireDate")] Instructor instructor)
        //{
        //    if (id != instructor.ID)
        //    {
        //        return NotFound();
        //    }

        //if (ModelState.IsValid)
        //    {
        //        try
        //        {
        //            _context.Update(instructor);
        //            await _context.SaveChangesAsync();
        //        }
        //        catch (DbUpdateConcurrencyException)
        //        {
        //            if (!InstructorExists(instructor.ID))
        //            {
        //                return NotFound();
        //            }
        //            else
        //            {
        //                throw;
        //            }
        //        }
        //        return RedirectToAction("Index");
        //    }
        //    return View(instructor);
        //}

        // POST: Instructors/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int? id, string[] selectedCourses)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Gets the current Instructor entity from the database using eager loading for the OfficeAssignment navigation property.
            var instructorToUpdate = await _context.Instructors
                .Include(i => i.OfficeAssignment)
                .Include(i => i.CourseAssignments)
                    .ThenInclude(i => i.Course)
                .SingleOrDefaultAsync(s => s.ID == id);

            // Updates the retrieved Instructor entity with values from the model binder.
            // The TryUpdateModel overload enables you to whitelist the properties you want to include.This prevents over - posting
            // This also means that CourseAssignments are not being updated even though they are in the instructorToUpdate
            if (await TryUpdateModelAsync<Instructor>(
                instructorToUpdate,
                "",
                i => i.FirstMidName, i => i.LastName, i => i.HireDate, i => i.OfficeAssignment))
            {
                if (String.IsNullOrWhiteSpace(instructorToUpdate.OfficeAssignment?.Location))
                {
                    instructorToUpdate.OfficeAssignment = null;
                }
                UpdateInstructorCourses(selectedCourses, instructorToUpdate);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException /* ex */)
                {
                    //Log the error (uncomment ex variable name and write a log.)
                    ModelState.AddModelError("", "Unable to save changes. " +
                        "Try again, and if the problem persists, " +
                        "see your system administrator.");
                }
                return RedirectToAction("Index");
            }
            return View(instructorToUpdate);
        }
        private void UpdateInstructorCourses(string[] selectedCourses, Instructor instructorToUpdate)
        {
            if (selectedCourses == null)
            {
                instructorToUpdate.CourseAssignments = new List<CourseAssignment>();
                return;
            }

            var selectedCoursesHS = new HashSet<string>(selectedCourses);
            var instructorCourses = new HashSet<int>
                (instructorToUpdate.CourseAssignments.Select(c => c.Course.CourseID));
            foreach (var course in _context.Courses)
            {
                if (selectedCoursesHS.Contains(course.CourseID.ToString()))
                {
                    if (!instructorCourses.Contains(course.CourseID))
                    {
                        instructorToUpdate.CourseAssignments.Add(new CourseAssignment { InstructorID = instructorToUpdate.ID, CourseID = course.CourseID });
                    }
                }
                else
                {

                    if (instructorCourses.Contains(course.CourseID))
                    {
                        CourseAssignment courseToRemove = instructorToUpdate.CourseAssignments.SingleOrDefault(i => i.CourseID == course.CourseID);
                        _context.Remove(courseToRemove);
                    }
                }
            }
        }
        // GET: Instructors/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var instructor = await _context.Instructors
                .SingleOrDefaultAsync(m => m.ID == id);
            if (instructor == null)
            {
                return NotFound();
            }

            return View(instructor);
        }

        // POST: Instructors/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            //var instructor = await _context.Instructors.SingleOrDefaultAsync(m => m.ID == id);

            //Does eager loading for the CourseAssignments navigation property.
            // You have to include this or EF won't know about related CourseAssignment entities 
            // and won't delete them.To avoid needing to read them here you could configure cascade delete in the database.
            Instructor instructor = await _context.Instructors
                .Include(i => i.CourseAssignments)
                .SingleAsync(i => i.ID == id);

            // If the instructor to be deleted is assigned as administrator of any departments, 
            // removes the instructor assignment from those departments.
            var departments = await _context.Departments
                .Where(d => d.InstructorID == id)
                .ToListAsync();
            departments.ForEach(d => d.InstructorID = null);


            _context.Instructors.Remove(instructor);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        private bool InstructorExists(int id)
        {
            return _context.Instructors.Any(e => e.ID == id);
        }
    }
}
