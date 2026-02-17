using Microsoft.AspNetCore.Mvc;
[ApiController]
[Route("test")]
public class Test : ControllerBase
{
    [HttpGet("/")]
    public async Task <IActionResult> T1()
    {
        string [] nevek = ["a", "b", "c"];

        return Ok(nevek);
    }
}