using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Ghayal_Bhaag.Models;

namespace Ghayal_Bhaag.Controllers
{
    public class BooksController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BooksController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Books/ListBooks
        public async Task<IActionResult> ListBooks(
            string searchTitle, string searchDescription, string searchISBN, string searchGenre,
            bool? newArrival, bool? sale, bool? physicalAccess, string sortOrder)
        {
            // Set ViewData for sorting parameters
            ViewData["NameSortParm"] = string.IsNullOrEmpty(sortOrder) ? "title_desc" : "title";
            ViewData["DateSortParm"] = sortOrder == "date" ? "date_desc" : "date";
            ViewData["PriceSortParm"] = sortOrder == "price" ? "price_desc" : "price";

            // Populate ViewBag for genre dropdown and filter persistence
            ViewBag.Genres = await _context.Book
                .Select(b => b.Genre)
                .Distinct()
                .OrderBy(g => g)
                .ToListAsync();
            ViewBag.SearchTitle = searchTitle;
            ViewBag.SearchDescription = searchDescription;
            ViewBag.SearchISBN = searchISBN;
            ViewBag.SearchGenre = searchGenre;
            ViewBag.NewArrival = newArrival;
            ViewBag.Sale = sale;
            ViewBag.PhysicalAccess = physicalAccess;

            // Start with all books
            IQueryable<Book> booksQuery = _context.Book;

            // Apply text filters (case-insensitive)
            if (!string.IsNullOrEmpty(searchTitle))
            {
                booksQuery = booksQuery.Where(b => b.BookTitle.ToLower().Contains(searchTitle.ToLower()));
            }

            if (!string.IsNullOrEmpty(searchDescription))
            {
                booksQuery = booksQuery.Where(b => b.Description != null && b.Description.ToLower().Contains(searchDescription.ToLower()));
            }

            if (!string.IsNullOrEmpty(searchISBN))
            {
                booksQuery = booksQuery.Where(b => b.ISBN.ToLower().Contains(searchISBN.ToLower()));
            }

            // Apply categorical filters
            if (!string.IsNullOrEmpty(searchGenre))
            {
                booksQuery = booksQuery.Where(b => b.Genre == searchGenre);
            }

            if (newArrival == true)
            {
                booksQuery = booksQuery.Where(b => b.NewArrival);
            }

            if (sale == true)
            {
                booksQuery = booksQuery.Where(b => b.Sale);
            }

            if (physicalAccess == true)
            {
                booksQuery = booksQuery.Where(b => b.PhysicalAccess);
            }

            // Apply sorting
            switch (sortOrder)
            {
                case "title":
                    booksQuery = booksQuery.OrderBy(s => s.BookTitle);
                    break;
                case "title_desc":
                    booksQuery = booksQuery.OrderByDescending(s => s.BookTitle);
                    break;
                case "date":
                    booksQuery = booksQuery.OrderBy(s => s.DateReleased);
                    break;
                case "date_desc":
                    booksQuery = booksQuery.OrderByDescending(s => s.DateReleased);
                    break;
                case "price":
                    booksQuery = booksQuery.OrderBy(s => s.Price);
                    break;
                case "price_desc":
                    booksQuery = booksQuery.OrderByDescending(s => s.Price);
                    break;
                default:
                    booksQuery = booksQuery.OrderBy(s => s.BookTitle);
                    break;
            }

            var books = await booksQuery.ToListAsync();
            return View(books);
        }

        // GET: Books/GetBookDetail/5
        public async Task<IActionResult> GetBookDetail(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Book.FirstOrDefaultAsync(m => m.BookId == id);
            if (book == null)
            {
                return NotFound();
            }

            // Check if the user has purchased this book (optimized)
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool hasPurchased = false;
            if (!string.IsNullOrEmpty(userId))
            {
                hasPurchased = await _context.Order
                    .Where(o => o.UserId == userId)
                    .Join(_context.OrderItem, o => o.OrderId, oi => oi.OrderId, (o, oi) => oi)
                    .AnyAsync(oi => oi.BookId == id);
            }
            ViewData["has_purchased"] = hasPurchased.ToString().ToLower();

            return View(book);
        }

        // GET: Books/Create
        [Authorize(Roles = "Admin")]
        public IActionResult CreateBook()
        {
            return View();
        }

        // POST: Books/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateBook(
            [Bind("BookId,BookTitle,ISBN,DateReleased,Description,Publisher,Genre,PhysicalAccess,Sale,NewArrival,Stock,Price,DiscountAmount,Language,Format,Author,Image")]
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
                    TempData["ErrorMessage"] = $"Database error occurred while saving the book: {ex.InnerException?.Message}";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"An unexpected error occurred: {ex.Message}";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Please correct the errors and try again.";
            }

            return View(book);
        }

        // GET: Books/Edit/5
        [Authorize(Roles = "Admin")]
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditBook(int id,
            [Bind("BookId,BookTitle,ISBN,DateReleased,Description,Publisher,Genre,PhysicalAccess,Sale,NewArrival,Stock,Price,DiscountAmount,Language,Format,Author,Image")]
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
                    TempData["SuccessMessage"] = "Book updated successfully!";
                    return RedirectToAction(nameof(ListBooks));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CheckBookExists(book.BookId))
                    {
                        TempData["ErrorMessage"] = "Book not found.";
                        return NotFound();
                    }
                    throw;
                }
                catch (DbUpdateException ex)
                {
                    TempData["ErrorMessage"] = $"Database error occurred while updating the book: {ex.InnerException?.Message}";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"An unexpected error occurred: {ex.Message}";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Please correct the errors and try again.";
            }

            return View(book);
        }

        // GET: Books/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteBook(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Book.FirstOrDefaultAsync(m => m.BookId == id);
            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }

        // POST: Books/Delete/5
        [HttpPost, ActionName("DeleteBook")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ConfirmDeletion(int id)
        {
            var book = await _context.Book.FindAsync(id);
            if (book == null)
            {
                TempData["ErrorMessage"] = "Book not found.";
                return NotFound();
            }

            try
            {
                _context.Book.Remove(book);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Book deleted successfully!";
            }
            catch (DbUpdateException ex)
            {
                TempData["ErrorMessage"] = $"Database error occurred while deleting the book: {ex.InnerException?.Message}";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An unexpected error occurred: {ex.Message}";
            }

            return RedirectToAction(nameof(ListBooks));
        }

        private bool CheckBookExists(int id)
        {
            return _context.Book.Any(e => e.BookId == id);
        }
    }
}