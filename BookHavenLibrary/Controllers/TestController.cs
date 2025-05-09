using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize(Roles = "admin")]
[Route("api/[controller]")]
[ApiController]
public class TestController : ControllerBase
{
    // GET: api/Test/admin-only
    [HttpGet("admin-only")]
    public IActionResult GetAdminOnly()
    {
        // Only admin users will get here
        return Ok("You are an Admin!");
    }
}
