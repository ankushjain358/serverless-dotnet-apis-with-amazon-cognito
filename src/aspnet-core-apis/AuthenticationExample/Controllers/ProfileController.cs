using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthenticationExample.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class ProfileController : ControllerBase
    {

        [HttpGet]
        public IEnumerable<KeyValuePair<string, string>> Get()
        {
            return User.Claims.Select(item => new KeyValuePair<string, string>(item.Type, item.Value)).ToList();
        }
    }
}