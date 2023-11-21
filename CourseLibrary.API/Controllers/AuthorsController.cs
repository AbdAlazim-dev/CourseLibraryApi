
using AutoMapper;
using CourseLibrary.API.Entities;
using CourseLibrary.API.Helpers;
using CourseLibrary.API.Models;
using CourseLibrary.API.ResourseParameters;
using CourseLibrary.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System.Dynamic;
using System.Text.Json;

namespace CourseLibrary.API.Controllers;

[ApiController]
[Route("api/authors")]
public class AuthorsController : ControllerBase
{
    private readonly ICourseLibraryRepository _courseLibraryRepository;
    private readonly IMapper _mapper;
    private readonly IProprtyMappingService _propertyMapping;
    private readonly IPropertyCheckService _propertyCheckService;
    private readonly ProblemDetailsFactory _problemDetailsFactory;

    public AuthorsController(
        ICourseLibraryRepository courseLibraryRepository,
        IMapper mapper,
        IProprtyMappingService propertyMapping,
        IPropertyCheckService propertyCheckService,
        ProblemDetailsFactory problemDetailsFactory)
    {
        _courseLibraryRepository = courseLibraryRepository ??
            throw new ArgumentNullException(nameof(courseLibraryRepository));
        _mapper = mapper ??
            throw new ArgumentNullException(nameof(mapper));
        _propertyMapping = propertyMapping ??
            throw new ArgumentNullException(nameof(propertyMapping));
        _propertyCheckService = propertyCheckService ??
            throw new ArgumentNullException(nameof(propertyCheckService)); ;
        _problemDetailsFactory = problemDetailsFactory ??
            throw new ArgumentNullException(nameof(problemDetailsFactory)); ;
    }
    /// <summary>
    /// Get All Authers
    /// </summary>
    /// <response code="200">return All Authers from the database</response>
    [HttpGet (Name = "GetAuthors")]
    [HttpHead]
    public async Task<IActionResult> GetAuthors(
        [FromQuery] AuthorResourseParameters authorResourseParameters)
    {
        //check if the request sort proprty is valid 
        if (!_propertyMapping.ValidMappingExistsFor<AuthorDto, Author>
            (authorResourseParameters.OrderBy))
        {
            return BadRequest();
        }
        if(!_propertyCheckService.TypeHasProperties<AuthorDto>(authorResourseParameters.Fields))
        {
            return BadRequest(_problemDetailsFactory.CreateProblemDetails(
                HttpContext,
                statusCode: 400,
                detail: $"the resourse {typeof(AuthorDto)} dose not contain all of these fields :" +
                $" {authorResourseParameters.Fields}"
                ));
        }

        // get authors from repo
        var authorsFromRepo = await _courseLibraryRepository
            .GetAuthorsAsync(authorResourseParameters);

        var previousPageLink = authorsFromRepo.HasPrevius ?
            CreateAuthorsResourceUri(authorResourseParameters, ResourceUriType.PreviousPage): null;

        var nextPage = authorsFromRepo.HasNext ?
            CreateAuthorsResourceUri(authorResourseParameters, ResourceUriType.NextPage): null;

        var paginatioMetaData = new
        {
            CurrentPage = authorsFromRepo.CurrentPage,
            PageSize = authorsFromRepo.PageSize,
            TotalCount = authorsFromRepo.TotalCount,
            TotalPages = authorsFromRepo.TotalPages,
            PreviousPage = previousPageLink,
            NextPage = nextPage
        };

        Response.Headers.Add("X-Pagination", JsonSerializer.Serialize(paginatioMetaData));

        var authorslinks = CreateLinksForAuthors(authorResourseParameters);

        var authorsExpandoObject = new List<ExpandoObject>();

        var shapedAuthors = _mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo)
            .ShapeData(authorResourseParameters.Fields);

        var shapedAuthorsWithLinks = shapedAuthors.Select(auther =>
        {
            var authorAsDic = auther as IDictionary<string, object?>;
            var autherLink = CreateLinks((Guid)authorAsDic["Id"], null);
            authorAsDic.Add("Links", autherLink);
            return authorAsDic;
        });

        var linkedCollectionResourse = new
        {
            value = shapedAuthorsWithLinks,
            links = authorslinks
        };

        

        // return them
        return Ok(linkedCollectionResourse);
    }
    private string? CreateAuthorsResourceUri(AuthorResourseParameters autherResourseParameter,
        ResourceUriType type
        )
    {
        switch (type)
        {
            case ResourceUriType.PreviousPage:
                return Url.Link("GetAuthors",
                    new 
                    {
                        fields = autherResourseParameter.Fields,
                        orderBy = autherResourseParameter.OrderBy,
                        PageNumber = autherResourseParameter.PageNumber - 1, 
                        PageSize = autherResourseParameter.PageSize,
                        SearchQurey = autherResourseParameter.SearchQurey,
                        MainCategory = autherResourseParameter.MainCategory
                    });
            case ResourceUriType.NextPage:
                return Url.Link("GetAuthors",
                    new 
                    {
                        fields = autherResourseParameter.Fields,
                        orderBy = autherResourseParameter.OrderBy,
                        PageNumber = autherResourseParameter.PageNumber + 1,
                        PageSize = autherResourseParameter.PageSize,
                        SearchQurey = autherResourseParameter.SearchQurey,
                        MainCategory = autherResourseParameter.MainCategory
                    });
            case ResourceUriType.Current: 
            default:
                return Url.Link("GetAuthors",
                    new
                    {
                        fields = autherResourseParameter.Fields,
                        orderBy = autherResourseParameter.OrderBy,
                        PageNumber = autherResourseParameter.PageNumber,
                        PageSize = autherResourseParameter.PageSize,
                        SearchQuery = autherResourseParameter.SearchQurey,
                        MainCategory = autherResourseParameter.MainCategory
                    });
        }
    }

    [HttpGet("{authorId}", Name = "GetAuthor")]
    public async Task<IActionResult> GetAuthor(Guid authorId, string? fields)
    {
        if (!_propertyCheckService.TypeHasProperties<AuthorDto>(fields))
        {
            return BadRequest(_problemDetailsFactory.CreateProblemDetails(
                HttpContext,
                statusCode: 400,
                detail: $"the resourse {typeof(AuthorDto)} dose not contain all of these fields :" +
                $" {fields}"
                ));
        }
        // get author from repo
        var authorFromRepo = await _courseLibraryRepository.GetAuthorAsync(authorId);

        if (authorFromRepo == null)
        {
            return NotFound();
        }
        var links = CreateLinks(authorId, fields);

        var linkedResourseToReturn = _mapper.Map<AuthorDto>(authorFromRepo)
            .ShapeData(fields) as IDictionary<string, object?>;

        linkedResourseToReturn.Add("links", links);

        // return author
        return Ok(linkedResourseToReturn);
    }
    public IEnumerable<LinkDto> CreateLinks(Guid authorId,  string fields)
    {
        var links =  new List<LinkDto>();

        if(string.IsNullOrWhiteSpace(fields))
        {
            links.Add(
               new(Url.Link("GetAuthor", new { authorId }),
               "self",
               "Get"
                ));
        }
        else
        {
            links.Add(
               new(Url.Link("GetAuthor", new { authorId, fields }),
               "self",
               "Get"
                ));
        }
        //hypermedia link to get all courses for this author
        links.Add(
               new(Url.Link("GetCoursesForTheAuthor", new { authorId }),
               "all-author-courses",
               "Get"
                ));

        //hybermedia link to create an course for this author
        links.Add(
               new(Url.Link("CreateCourse", new { authorId }),
               "create-course-for-author",
               "Post"
                ));

        return links;
    }
    public IEnumerable<LinkDto> CreateLinksForAuthors(AuthorResourseParameters authorResourseParameters)
    {
        var links = new List<LinkDto>();

        links.Add(
            new(CreateAuthorsResourceUri(authorResourseParameters,
            ResourceUriType.Current),
            "self",
            "get"
            ));

        return links;
    }
    [HttpPost]
    public async Task<ActionResult<AuthorDto>> CreateAuthor(AutherForCreationDto author)
    {
        var authorEntity = _mapper.Map<Entities.Author>(author);

        _courseLibraryRepository.AddAuthor(authorEntity);
        await _courseLibraryRepository.SaveAsync();

        var authorToReturn = _mapper.Map<AuthorDto>(authorEntity);

        var links = CreateLinks(authorToReturn.Id, null);

        var linkedResoursesToReturn = authorToReturn
            .ShapeData(null) as IDictionary<string, object?>;
        linkedResoursesToReturn.Add("Links", links);

        return CreatedAtRoute("GetAuthor",
            new { authorId = linkedResoursesToReturn["Id"] },
            linkedResoursesToReturn);
    }
    [HttpOptions]
    public IActionResult GetAuthersOptions()
    {
        Response.Headers.Add("allow", "HEAD,GET,POST,OPTIONS");
        return Ok();
    }
}
