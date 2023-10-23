using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomScopesAuthorizationExample.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class PhotoController : ControllerBase
    {

        [HttpGet]
        [Authorize(Policy = "ReadPhotos")]
        public IActionResult Get()
        {
            return Ok();
        }

        [HttpPost]
        [Authorize(Policy = "WritePhotos")]
        public IActionResult Post()
        {
            return Ok();
        }
    }
}