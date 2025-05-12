using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;
using ViewParty.Models;

namespace ViewParty.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            ViewBag.ShowLink1 = true;
            ViewBag.ShowLink2 = true;
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> InfoVideo(string Link,string roomID)
        {
            return Ok();
        }

        public IActionResult CreateRoom()
        {
            string roomID = Guid.NewGuid().ToString();
            return RedirectToAction("Room",new {roomID = roomID});

        }
        
        public IActionResult Room(string roomID)
        {
            ViewBag.ShowLink1 = false;
            ViewBag.ShowLink2 = false;
            ViewBag.roomID = roomID; 
            return View();
        }

        public IActionResult Privacy()
        {
            ViewBag.ShowLink1 = true;
            ViewBag.ShowLink2 = true;
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
