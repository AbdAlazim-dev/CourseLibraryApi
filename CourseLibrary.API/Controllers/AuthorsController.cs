
using AutoMapper;
using CourseLibrary.API.Helpers;
using CourseLibrary.API.Models;
using CourseLibrary.API.ResourseParameters;
using CourseLibrary.API.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CourseLibrary.API.Controllers;

[ApiController]
[Route("api/authors")]
public class AuthorsController : ControllerBase
{
    private readonly ICourseLibraryRepository _courseLibraryRepository;
    private readonly IMapper _mapper;

    public AuthorsController(
        ICourseLibraryRepository courseLibraryRepository,
        IMapper mapper)
    {
        _courseLibraryRepository = courseLibraryRepository ??
            throw new ArgumentNullException(nameof(courseLibraryRepository));
        _mapper = mapper ??
            throw new ArgumentNullException(nameof(mapper));
    }
    /// <summary>
    /// Get All Authers
    /// </summary>
    /// <response code="200">return All Authers from the database</response>
    [HttpGet (Name = "GetAuthors")]
    [HttpHead]
    public async Task<ActionResult<IEnumerable<AuthorDto>>> GetAuthors(
        [FromQuery] AuthorResourseParameters authorResourseParameters)
    { 
        
        
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


        // return them
        return Ok(_mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo));
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
                        PageNumber = autherResourseParameter.PageNumber - 1, 
                        PageSize = autherResourseParameter.PageSize,
                        SearchQurey = autherResourseParameter.SearchQurey,
                        MainCategory = autherResourseParameter.MainCategory
                    });
            case ResourceUriType.NextPage:
                return Url.Link("GetAuthors",
                    new 
                    {
                        PageNumber = autherResourseParameter.PageNumber + 1,
                        PageSize = autherResourseParameter.PageSize,
                        SearchQurey = autherResourseParameter.SearchQurey,
                        MainCategory = autherResourseParameter.MainCategory
                    });
            default:
                return Url.Link("GetAuthors",
                    new
                    {
                        PageNumber = autherResourseParameter.PageNumber,
                        PageSize = autherResourseParameter.PageSize,
                        SearchQuery = autherResourseParameter.SearchQurey,
                        MainCategory = autherResourseParameter.MainCategory
                    });
        }
    }

    [HttpGet("{authorId}", Name = "GetAuthor")]
    public async Task<ActionResult<AuthorDto>> GetAuthor(Guid authorId)
    {
        // get author from repo
        var authorFromRepo = await _courseLibraryRepository.GetAuthorAsync(authorId);

        if (authorFromRepo == null)
        {
            return NotFound();
        }

        // return author
        return Ok(_mapper.Map<AuthorDto>(authorFromRepo));
    }

    [HttpPost]
    public async Task<ActionResult<AuthorDto>> CreateAuthor(AutherForCreationDto author)
    {
        var authorEntity = _mapper.Map<Entities.Author>(author);

        _courseLibraryRepository.AddAuthor(authorEntity);
        await _courseLibraryRepository.SaveAsync();

        var authorToReturn = _mapper.Map<AuthorDto>(authorEntity);

        return CreatedAtRoute("GetAuthor",
            new { authorId = authorToReturn.Id },
            authorToReturn);
    }
    [HttpOptions]
    public IActionResult GetAuthersOptions()
    {
        Response.Headers.Add("allow", "HEAD,GET,POST,OPTIONS");
        return Ok();
    }
}
