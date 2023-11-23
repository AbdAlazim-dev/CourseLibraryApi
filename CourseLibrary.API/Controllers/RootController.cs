using CourseLibrary.API.Models;
using Microsoft.AspNetCore.Mvc;

namespace CourseLibrary.API.Controllers;

[Route("api")]
[ApiController]
public class RootController : Controller
{
    [HttpGet(Name = "GetRoot")]

    public IActionResult GetRoot()
    {
        var links = new List<LinkDto>();

        links.Add(new(Url.Link("GetRoot", new { }),
            "self",
            "Get"));

        links.Add(new(Url.Link("GetAuthors", new { }),
            "Authors",
            "GET"));

        links.Add(new(Url.Link("CreateAuthor", new { }),
            "Authors",
            "POST"));

        return Ok(links);
    }
    
}
