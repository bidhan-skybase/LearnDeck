using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Ghayal_Bhaag.Models;

namespace Ghayal_Bhaag.Controllers
{
    public class BookmarksController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookmarksController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Bookmarks
        public async Task<IActionResult> ListBookMarks()
        {
            var applicationDbContext = _context.Bookmark.Include(b => b.User).Include(b => b.Book);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Bookmarks/Details/5
        public async Task<IActionResult> GetBookMarkDetails(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bookmark = await _context.Bookmark
                .Include(b => b.Book) // Include Book details
                .Include(b => b.User) // Optional: Include User if needed elsewhere
                .FirstOrDefaultAsync(m => m.BookmarkId == id);

            if (bookmark == null)
            {
                return NotFound();
            }

            return View(bookmark);
        }

        // GET: Bookmarks/Create
        public IActionResult CreateBookMark(int? id)
        {
            if ( id != null)
            {
                ViewData["BookId"] = id;
            }
            else
            {
                ViewData["BookId"] = new SelectList(_context.Book, "BookId", "BookId");
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id");
            return View();
        }

        // POST: Bookmarks/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBookMark([Bind("BookmarkId,BookId,UserId")] Bookmark bookmark)
        {
            bookmark.CreatedDate = DateTime.Now;
            _context.Add(bookmark);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ListBookMarks));
        }

        // GET: Bookmarks/Edit/5
        public async Task<IActionResult> EditBookMark(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bookmark = await _context.Bookmark.FindAsync(id);
            if (bookmark == null)
            {
                return NotFound();
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", bookmark.UserId);
            return View(bookmark);
        }

        // POST: Bookmarks/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBookMark(int id, [Bind("BookmarkId,UserId,CreatedDate")] Bookmark bookmark)
        {
            if (id != bookmark.BookmarkId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(bookmark);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CheckBookMarkExists(bookmark.BookmarkId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(ListBookMarks));
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", bookmark.UserId);
            return View(bookmark);
        }

        // GET: Bookmarks/Delete/5
        public async Task<IActionResult> DeleteBookMark(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bookmark = await _context.Bookmark
                .Include(b => b.Book)
                .FirstOrDefaultAsync(m => m.BookmarkId == id);
            if (bookmark == null)
            {
                return NotFound();
            }

            return View(bookmark);
        }

        // POST: Bookmarks/Delete/5
        [HttpPost, ActionName("DeleteBookMark")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmDeletion(int id)
        {
            var bookmark = await _context.Bookmark.FindAsync(id);
            if (bookmark != null)
            {
                _context.Bookmark.Remove(bookmark);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ListBookMarks));
        }

        private bool CheckBookMarkExists(int id)
        {
            return _context.Bookmark.Any(e => e.BookmarkId == id);
        }
    }
}
