
using AutoMapper;
using CourseLibrary.API.ActionsConsrains;
using CourseLibrary.API.Entities;
using CourseLibrary.API.Helpers;
using CourseLibrary.API.Models;
using CourseLibrary.API.ResourseParameters;
using CourseLibrary.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Net.Http.Headers;
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
        [FromQuery] AuthorResourseParameters authorResourseParameters,
        [FromHeader(Name = "Accept")] string mediaType)
    {
        //check if the request sort proprty is valid 
        if (!_propertyMapping.ValidMappingExistsFor<AuthorDto, Author>
            (authorResourseParameters.OrderBy))
        {
            return BadRequest();
        }
        if(!MediaTypeHeaderValue.TryParse(mediaType, out var parsedValue))
        {
            return BadRequest(_problemDetailsFactory.CreateProblemDetails(HttpContext,
                detail: "the accept header you entered is not valid media type",
                statusCode: 400));
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
        var includeLinks = parsedValue.SubTypeWithoutSuffix
            .EndsWith("hateoas", StringComparison.InvariantCultureIgnoreCase);


        var links = new List<LinkDto>();

        // get authors from repo
        var authorsFromRepo = await _courseLibraryRepository
            .GetAuthorsAsync(authorResourseParameters);

        if (includeLinks)
        {
            links.AddRange(CreateLinksForAuthors(authorResourseParameters,
            authorsFromRepo.HasPrevius,
            authorsFromRepo.HasNext));
        }

        var paginatioMetaData = new
        {
            CurrentPage = authorsFromRepo.CurrentPage,
            PageSize = authorsFromRepo.PageSize,
            TotalCount = authorsFromRepo.TotalCount,
            TotalPages = authorsFromRepo.TotalPages
        };

        Response.Headers.Add("X-Pagination", JsonSerializer.Serialize(paginatioMetaData));

        var authorsExpandoObject = new List<ExpandoObject>();

        var shapedAuthors = _mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo)
            .ShapeData(authorResourseParameters.Fields);

        var shapedAuthorsWithLinks = shapedAuthors.Select(auther =>
        {
            var authorAsDic = auther as IDictionary<string, object?>;
            if(includeLinks)
            {
                var autherLink = CreateLinksForAuthor((Guid)authorAsDic["Id"], null);
                authorAsDic.Add("Links", autherLink);
            }
            return authorAsDic;
        });


        dynamic linkedCollectionResourse;

        if (includeLinks)
        {
            linkedCollectionResourse = new
            {
                value = shapedAuthorsWithLinks,
                links = links
            };
        }
        else
        {
            linkedCollectionResourse = new
            {
                value = shapedAuthorsWithLinks
            };
        }

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
                        SearchQurey = autherResourseParameter.SearchQuery,
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
                        SearchQurey = autherResourseParameter.SearchQuery,
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
                        SearchQuery = autherResourseParameter.SearchQuery,
                        MainCategory = autherResourseParameter.MainCategory
                    });
        }
    }
    [HttpGet("{authorId}", Name = "GetAuthor")]
    [HeaderMatchsMediaType("Accept", "application/json",
        "application/vnd.marvin.author.friendly+json")]
    [Produces("application/json",
        "application/vnd.marvin.author.friendly+json")]
    public async Task<IActionResult> GetAuthorWithouLinks(Guid authorId, string? fields)
    {
        if (!_propertyCheckService.TypeHasProperties<AuthorDto>(fields))
        {
            return BadRequest(_problemDetailsFactory.CreateProblemDetails(
                HttpContext,
                statusCode: 400,
                detail: $"the resourse dose not contain all of these fields :" +
                $" {fields}"
                ));
        }

        var authorEntity = await _courseLibraryRepository.GetAuthorAsync(authorId);

        if(authorEntity == null)
        {
            return NotFound();
        }

        var freindlyAuthorsEntities = _mapper.Map<AuthorDto>(authorEntity).ShapeData(fields);

        return Ok(freindlyAuthorsEntities);
    }
    [HttpGet("{authorId}", Name = "GetAuthorWithLinks")]
    [HeaderMatchsMediaType("Accept",
        "application/vnd.marvin.hateoas+json",
        "application/vnd.marvin.author.friendly.hateoas+json")]
    [Produces("application/vnd.marvin.hateoas+json",
        "application/vnd.marvin.author.friendly.hateoas+json")]
    public async Task<IActionResult> GetAuthorWithLinks(Guid authorId, string? fields)
    {
        if (!_propertyCheckService.TypeHasProperties<AuthorDto>(fields))
        {
            return BadRequest(_problemDetailsFactory.CreateProblemDetails(
                HttpContext,
                statusCode: 400,
                detail: $"the resourse dose not contain all of these fields :" +
                $" {fields}"
                ));
        }
        var authorEntity = await _courseLibraryRepository.GetAuthorAsync(authorId);

        if( authorEntity == null)
        {
            return NotFound();
        }

        IEnumerable<LinkDto> links = CreateLinksForAuthor(authorId, fields);

        var freindlyResourseToReturn = _mapper.Map<AuthorDto>(authorEntity).ShapeData(fields) as IDictionary<string, object?>;

        freindlyResourseToReturn.Add("Links", links);

        return Ok(freindlyResourseToReturn);
    }
    [HttpGet("{authorId}",Name = "GetAuthorFull")]
    [HeaderMatchsMediaType("Accept",
        "application/vnd.marvin.author.full+json")]
    [Produces("application/vnd.marvin.author.full+json")]
    public async Task<IActionResult> GetAuthorFull(Guid authorId, string? fields)
    {
        if(!_propertyCheckService.TypeHasProperties<AuthorDto>(fields))
        {
            return BadRequest(_problemDetailsFactory.CreateProblemDetails(
                HttpContext,
                statusCode: 400,
                detail: $"the resourse dose not contain all of these fields :" +
                $" {fields}"
                ));
        }

        var authorEntity = await _courseLibraryRepository.GetAuthorAsync(authorId);

        if(authorEntity == null)
        {
            return NotFound();
        }

        var authorToReturn = _mapper.Map<FullAuthorDto>(authorEntity);

        return Ok(authorToReturn);
    }
    [HttpGet("{authorId}", Name = "GetAuthorFullWithLinks")]
    [HeaderMatchsMediaType("Accept",
    "application/vnd.marvin.author.full.hateoas+json")]
    [Produces("application/vnd.marvin.author.full.hateoas+json")]
    public async Task<IActionResult> GetAuthorFullWithLinks(Guid authorId, string? fields)
    {
        if (!_propertyCheckService.TypeHasProperties<AuthorDto>(fields))
        {
            return BadRequest(_problemDetailsFactory.CreateProblemDetails(
                HttpContext,
                statusCode: 400,
                detail: $"the resourse dose not contain all of these fields :" +
                $" {fields}"
                ));
        }

        var authorEntity = await _courseLibraryRepository.GetAuthorAsync(authorId);

        if( authorEntity == null)
        {
            return NotFound();
        }
        IEnumerable<LinkDto> links = CreateLinksForAuthor(authorId, fields);

        var authorFullToReturn = _mapper.Map<FullAuthorDto>(authorEntity).ShapeData(fields) as IDictionary<string, object?>;

        authorFullToReturn.Add("Links", links);

        return Ok(authorFullToReturn);
    }
    private IEnumerable<LinkDto> CreateLinksForAuthor(Guid authorId,  string? fields)
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
    private IEnumerable<LinkDto> CreateLinksForAuthors(
        AuthorResourseParameters authorResourseParameters,
        bool hasPrevious,
        bool hasNext)
    {
        var links = new List<LinkDto>();

        //link for the same page
        links.Add(
            new(CreateAuthorsResourceUri(authorResourseParameters,
            ResourceUriType.Current),
            "self",
            "get"
            ));


        //links for previous page
        if(hasPrevious)
        {
            links.Add(
                new(CreateAuthorsResourceUri(authorResourseParameters,
                ResourceUriType.PreviousPage),
                "previous-page",
                "get"
                )) ;
        }

        //link for the next page
        if(hasNext)
        {
            links.Add(
                new(CreateAuthorsResourceUri(authorResourseParameters,
                ResourceUriType.NextPage),
                "next-page",
                "get"
                ));
        }

        return links;
    }
    [HttpPost(Name = "CreateAuthorWithDateOfDeath")]
    [HeaderMatchsMediaType("Content-Type",
    "application/vnd.marvin.authorforcreationwithdateofdeath+json")]
    [Consumes("application/vnd.marvin.authorforcreationwithdateofdeath+json")]
    public async Task<ActionResult<AuthorDto>> CreateAuthorWithDateOfDeath(
    AuthorForCreationWithDateOfDeathDto author)
    {
        var authorEntity = _mapper.Map<Entities.Author>(author);

        _courseLibraryRepository.AddAuthor(authorEntity);
        await _courseLibraryRepository.SaveAsync();

        var authorToReturn = _mapper.Map<AuthorDto>(authorEntity);

        var links = CreateLinksForAuthor(authorToReturn.Id, null);

        var linkedResoursesToReturn = authorToReturn
            .ShapeData(null) as IDictionary<string, object?>;

        linkedResoursesToReturn.Add("Links", links);

        return CreatedAtRoute("GetAuthor",
            new { authorId = linkedResoursesToReturn["Id"] },
            linkedResoursesToReturn);
    }
    [HttpPost(Name = "CreateAuthor")]
    [HeaderMatchsMediaType("Content-Type",
        "application/json",
        "application/vnd.marvin.authorforcreation+json")]
    [Consumes("application/json",
        "application/vnd.marvin.authorforcreation+json")]
    public async Task<ActionResult<AuthorDto>> CreateAuthor(AutherForCreationDto author)
    {
        var authorEntity = _mapper.Map<Entities.Author>(author);

        _courseLibraryRepository.AddAuthor(authorEntity);
        await _courseLibraryRepository.SaveAsync();

        var authorToReturn = _mapper.Map<AuthorDto>(authorEntity);

        var links = CreateLinksForAuthor(authorToReturn.Id, null);

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
