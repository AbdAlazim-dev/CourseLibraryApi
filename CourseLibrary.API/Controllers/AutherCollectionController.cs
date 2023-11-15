using AutoMapper;
using CourseLibrary.API.Entities;
using CourseLibrary.API.Helpers;
using CourseLibrary.API.Models;
using CourseLibrary.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CourseLibrary.API.Controllers
{
    [Route("api/autherCollecion")]
    [ApiController]
    public class AutherCollectionController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly ICourseLibraryRepository _courseLibraryRepository;

        public AutherCollectionController(IMapper mapper, ICourseLibraryRepository courseLibraryRepository)
        {
            _mapper = mapper;
            _courseLibraryRepository = courseLibraryRepository;
        }
        [HttpGet("{authersIds}", Name = "GetAutherCollection")]
        public async Task<ActionResult<IEnumerable<AutherForCreationDto>>>
            GetCollectionOfAuthors
            ([ModelBinder(BinderType = typeof(ArrayModelBinder))]
            [FromRoute] IEnumerable<Guid> authersIds)
        {
            var authersEntities = await _courseLibraryRepository.GetAuthorsAsync(authersIds);

            if(authersIds.Count() != authersEntities.Count())
            {
                return NotFound();
            }
            var authersToReturn = _mapper.Map<IEnumerable<AuthorDto>>(authersEntities);

            return Ok(authersToReturn);

        }

        [HttpPost]
        public async Task<ActionResult<IEnumerable<AuthorDto>>> CreateACollectionOfAuthers(
            IEnumerable<AutherForCreationDto> collectionOfAuthersToAdd
            )
        {
            var collectionOfAutherEntity = _mapper.Map<ICollection<Author>>(collectionOfAuthersToAdd);
        
            foreach(var auther in collectionOfAutherEntity)
            {
                _courseLibraryRepository.AddAuthor(auther);
            }
            await _courseLibraryRepository.SaveAsync();

            var authersToReturn = _mapper.Map<IEnumerable<AuthorDto>>(collectionOfAutherEntity);

            var authersIdAsString = string.Join(",", collectionOfAutherEntity.Select(a => a.Id));

            return CreatedAtRoute("GetAutherCollection", new
            {
                authersIds = authersIdAsString
            }, authersToReturn);
        }
    }
}
