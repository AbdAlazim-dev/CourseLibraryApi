
using AutoMapper;
using CourseLibrary.API.Entities;
using CourseLibrary.API.Models;
using CourseLibrary.API.Services;
using Marvin.Cache.Headers;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace CourseLibrary.API.Controllers;

[ApiController]
[Route("api/authors/{authorId}/courses")]
//[ResponseCache(CacheProfileName = "240SecondCache")]
public class CoursesController : ControllerBase
{
    private readonly ICourseLibraryRepository _courseLibraryRepository;
    private readonly IMapper _mapper;

    public CoursesController(ICourseLibraryRepository courseLibraryRepository,
        IMapper mapper)
    {
        _courseLibraryRepository = courseLibraryRepository ??
            throw new ArgumentNullException(nameof(courseLibraryRepository));
        _mapper = mapper ??
            throw new ArgumentNullException(nameof(mapper));
    }
    [HttpGet(Name = "GetCoursesForTheAuthor")]
    public async Task<ActionResult<IEnumerable<CourseDto>>> GetCoursesForAuthor(Guid authorId)
    {
        if (!await _courseLibraryRepository.AuthorExistsAsync(authorId))
        {
            return NotFound();
        }

        var coursesForAuthorFromRepo = await _courseLibraryRepository.GetCoursesAsync(authorId);
        return Ok(_mapper.Map<IEnumerable<CourseDto>>(coursesForAuthorFromRepo));
    }
    // [ResponseCache(Duration = 120)]
    [HttpCacheExpiration(CacheLocation = CacheLocation.Public, MaxAge = 1000)]
    [HttpCacheValidation(MustRevalidate = false)]
    [HttpGet("{courseId}", Name = "GetCourseForAuther")]
    public async Task<ActionResult<CourseDto>> GetCourseForAuthor(Guid authorId, Guid courseId)
    {
        if (!await _courseLibraryRepository.AuthorExistsAsync(authorId))
        {
            return NotFound();
        }

        var courseForAuthorFromRepo = await _courseLibraryRepository.GetCourseAsync(authorId, courseId);

        if (courseForAuthorFromRepo == null)
        {
            return NotFound();
        }
        return Ok(_mapper.Map<CourseDto>(courseForAuthorFromRepo));
    }


    [HttpPost(Name = "CreateCourse")]
    public async Task<ActionResult<CourseDto>> CreateCourseForAuthor(
            Guid authorId, CourseForCreationDto course)
    {
        if (!await _courseLibraryRepository.AuthorExistsAsync(authorId))
        {
            return NotFound();
        }

        var courseEntity = _mapper.Map<Entities.Course>(course);
        _courseLibraryRepository.AddCourse(authorId, courseEntity);
        await _courseLibraryRepository.SaveAsync();

        var courseToReturn = _mapper.Map<CourseDto>(courseEntity);


        return CreatedAtRoute("GetCourseForAuther", new
        {
            courseId = courseToReturn.Id,
            authorId
        }, courseToReturn);
    }
    public IEnumerable<LinkDto> CreateLinksForCourse(Guid courseId)
    {
        var links = new List<LinkDto>();

        //include a link to patch the resourse
        links.Add(
               new(Url.Link("PartillyUpdateCourse", new { courseId }),
               "partlly-update-resourse",
               "patch"
                ));

        //include a link to put a the resourse

        links.Add(
       new(Url.Link("FullyUpdateTheCourse", new { courseId }),
       "update-thefull-resouse",
       "put"
        ));

        //link to delete the resourse

        links.Add(
       new(Url.Link("DeleteCourse", new { courseId }),
       "delete",
       "Delete"
        ));

        //return all links
        return links;
    }
    [HttpPatch("{courseId}", Name = "PartillyUpdateCourse")]
    public async Task<IActionResult> UpdatePartOfTheCourse(Guid authorId,
        Guid courseId,
        JsonPatchDocument<CourseForUpdateDto> patchDocument)
    {
        if(!await _courseLibraryRepository.AuthorExistsAsync(authorId))
        {
            return NotFound();
        }

        var courseEntitiy = await _courseLibraryRepository.GetCourseAsync(authorId , courseId);
        if(courseEntitiy == null)
        {
            var courseDto = new CourseForUpdateDto();
            patchDocument.ApplyTo(courseDto, ModelState);

            if(!TryValidateModel(courseDto))
            {
                return ValidationProblem(ModelState);
            }

            var courseToAdd = _mapper.Map<Course>(courseDto);

            _courseLibraryRepository.AddCourse(authorId , courseToAdd);
            await _courseLibraryRepository.SaveAsync();

            var courseToReturn = _mapper.Map<CourseDto>(courseToAdd);

            return CreatedAtRoute("GetCourseForAuther", new
            {
                courseId = courseToReturn.Id,
                authorId
            }, courseToReturn);
        }

        var courseToPatch = _mapper.Map<CourseForUpdateDto>(courseEntitiy);

        patchDocument.ApplyTo(courseToPatch, ModelState);

        if(!TryValidateModel(courseToPatch))
        {
            return ValidationProblem(ModelState);
        }

        _mapper.Map(courseToPatch, courseEntitiy);

        _courseLibraryRepository.UpdateCourse(courseEntitiy);

        await _courseLibraryRepository.SaveAsync();

        return NoContent();

    }
    [HttpPut("{courseId}", Name = "FullyUpdateTheCourse")]
    public async Task<IActionResult> UpdateCourseForAuthor(Guid authorId,
      Guid courseId,
      CourseForUpdateDto course)
    {
        if (!await _courseLibraryRepository.AuthorExistsAsync(authorId))
        {
            return NotFound();
        }

        var courseForAuthorFromRepo = await _courseLibraryRepository.GetCourseAsync(authorId, courseId);

        if (courseForAuthorFromRepo == null)
        {
            var courseToAdd = _mapper.Map<Entities.Course>(course);
            courseToAdd.Id = courseId;
            _courseLibraryRepository.AddCourse(authorId, courseToAdd);
            await _courseLibraryRepository.SaveAsync();

            var courseToReturn = _mapper.Map<CourseDto>(courseToAdd);

            return CreatedAtRoute(
                "GetCourseForAuther",
                new { authorId, courseId = courseToReturn.Id },
                courseToReturn
                );
        }

        _mapper.Map(course, courseForAuthorFromRepo);

        _courseLibraryRepository.UpdateCourse(courseForAuthorFromRepo);

        await _courseLibraryRepository.SaveAsync();
        return NoContent();
    }

    [HttpDelete("{courseId}", Name = "DeleteCourse")]
    public async Task<ActionResult> DeleteCourseForAuthor(Guid authorId, Guid courseId)
    {
        if (!await _courseLibraryRepository.AuthorExistsAsync(authorId))
        {
            return NotFound();
        }

        var courseForAuthorFromRepo = await _courseLibraryRepository.GetCourseAsync(authorId, courseId);

        if (courseForAuthorFromRepo == null)
        {
            return NotFound();
        }

        _courseLibraryRepository.DeleteCourse(courseForAuthorFromRepo);
        await _courseLibraryRepository.SaveAsync();

        return NoContent();
    }
    public override ActionResult ValidationProblem([ActionResultObjectValue] 
    ModelStateDictionary modelStateDic)
    {
        var options = HttpContext.RequestServices
            .GetRequiredService<IOptions<ApiBehaviorOptions>>();

        return (ActionResult)options.Value.InvalidModelStateResponseFactory(ControllerContext);    
    }

}