using System.Diagnostics;
using BookMart.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookMart.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> List()
        {
            // Get new arrival books for billboard section
            var newArrivalBooks = await _context.Book
                .Where(b => b.NewArrival == true)
                .ToListAsync();

            // Get all books for products section
            var allBooks = await _context.Book
                .ToListAsync();

            // Pass both lists to the view
            ViewBag.AllBooks = allBooks;

            // Return new arrival books as the primary model for billboard
            return View(newArrivalBooks);
        }
        

        public IActionResult About()
        {
            return View();
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}