using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Ghayal_Bhaag.Models;
using Microsoft.AspNet.Identity;

namespace Ghayal_Bhaag.Controllers
{
    public class BooksController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BooksController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Books
        public async Task<IActionResult> ListBooks(string searchTitle, string searchDescription, string searchISBN,
            string sortOrder)
        {
            ViewData["NameSortParm"] = String.IsNullOrEmpty(sortOrder) ? "title" : "title_desc";
            ViewData["DateSortParm"] = sortOrder == "date" ? "date_desc" : "date";
            ViewData["PriceSortParm"] = sortOrder == "price" ? "price_desc" : "price";

            Console.WriteLine(searchTitle);

            var ApplicationDbContext = _context.Book;

            var books = from a in ApplicationDbContext select a;

            if (!String.IsNullOrEmpty(searchTitle))
            {
                books = books.Where(d => d.BookTitle.Contains(searchTitle));
            }

            if (!String.IsNullOrEmpty(searchDescription))
            {
                books = books.Where(d => d.Description.Contains(searchDescription));
            }

            if (!String.IsNullOrEmpty(searchISBN))
            {
                books = books.Where(d => d.ISBN.Contains(searchISBN));
            }

            switch (sortOrder)
            {
                case "title":
                    books = books.OrderBy(s => s.BookTitle);
                    break;
                case "title_desc":
                    books = books.OrderByDescending(s => s.BookTitle);
                    break;
                case "date":
                    books = books.OrderBy(s => s.DateReleased);
                    break;
                case "date_desc":
                    books = books.OrderByDescending(s => s.DateReleased);
                    break;
                case "price_desc":
                    books = books.OrderByDescending(s => s.Price);
                    break;
                case "price":
                    books = books.OrderBy(s => s.Price);
                    break;
                case "popularity":
                    books = books.OrderByDescending(s => s.ISBN);
                    break;
                default:
                    break;
            }

            return View(await books.ToListAsync());
        }

        // GET: Books/Details/5
        public async Task<IActionResult> GetBookDetail(int? id)
        {
            //Check if the user has purchased this book
            List<Order> orders = _context.Order.Where(o => o.UserId == User.Identity.GetUserId()).ToList();

            bool has_purchased = false;


            foreach (Order order in orders)
            {
                List<OrderItem> order_items = _context.OrderItem.Where(o => o.OrderId == order.OrderId).ToList();

                foreach (OrderItem item in order_items)
                {
                    if (item.BookId == id)
                    {
                        has_purchased = true;
                    }
                }
            }

            ViewData["has_purchased"] = has_purchased.ToString();
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Book
                .FirstOrDefaultAsync(m => m.BookId == id);
            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }

        // GET: Books/Create
        public IActionResult CreateBook()
        {
            return View();
        }

        // POST: Books/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBook(
            [Bind(
                "BookId,BookTitle,ISBN,DateReleased,Description,Publisher,Genre,PhysicalAccess,Sale,NewArrival,Stock,Price,Discount,Language,Format,Author,Image")]
            Book book)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(book);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Book created successfully!";
                    return RedirectToAction(nameof(ListBooks));
                }
                catch (DbUpdateException ex)
                {
                    ModelState.AddModelError("",
                        "Database error occurred while saving the book: " + ex.InnerException?.Message);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An unexpected error occurred: " + ex.Message);
                }
            }

            // If ModelState is invalid or save fails, return the view with errors
            return View(book);
        }

        // GET: Books/Edit/5
        public async Task<IActionResult> EditBook(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Book.FindAsync(id);
            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }

        // POST: Books/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBook(int id,
            [Bind(
                "BookId,BookTitle,ISBN,DateReleased,Description,Publisher,Genre,PhysicalAccess,Sale,NewArrival,Stock,Price,Discount,Language,Format,Author,Image")]
            Book book)
        {
            if (id != book.BookId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(book);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CheckBookExists(book.BookId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                return RedirectToAction(nameof(ListBooks));
            }

            return View(book);
        }

        // GET: Books/Delete/5
        public async Task<IActionResult> DeleteBook(int? id)
        {
            Console.WriteLine("this is the deletebook title");
            Console.WriteLine(id);

            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Book
                .FirstOrDefaultAsync(m => m.BookId == id);
            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }

        // POST: Books/Delete/5
        [HttpPost, ActionName("DeleteBook")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmDeletion(int id)
        {
            var book = await _context.Book.FindAsync(id);
            if (book != null)
            {
                _context.Book.Remove(book);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ListBooks));
        }

        private bool CheckBookExists(int id)
        {
            return _context.Book.Any(e => e.BookId == id);
        }
    }
}