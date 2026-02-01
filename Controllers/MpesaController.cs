using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace KNQASelfService.Controllers
{
    [ApiController]
    [Route("api/mpesa")]
    public class MpesaController : ControllerBase
    {
        [HttpPost("callback")]
        public async Task<IActionResult> Callback()
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            // Optional: Pretty-print JSON
            var parsed = JsonConvert.DeserializeObject(body);
            var formatted = JsonConvert.SerializeObject(parsed, Formatting.Indented);

            System.IO.File.WriteAllText("mpesa-callback.json", formatted);

            return Ok();
        }

    }

}
