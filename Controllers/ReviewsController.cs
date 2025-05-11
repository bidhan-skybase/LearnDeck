using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BookMart.Models;

namespace BookMart.Controllers
{
    public class ReviewsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReviewsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Reviews
        public async Task<IActionResult> GetReviews()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            Console.WriteLine("current user id: " + userId);
            Console.WriteLine("current user is admin: " + isAdmin);

            IQueryable<Review> reviewsQuery = _context.Review
                .Include(r => r.Book)
                .Include(r => r.User);

            if (!isAdmin)
            {
                reviewsQuery = reviewsQuery.Where(r => r.UserId == userId);
            }

            var reviews = await reviewsQuery.ToListAsync();
            return View(reviews);
        }

        // GET: Reviews/Details/5
        public async Task<IActionResult> GetReviewDetail(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var review = await _context.Review
                .Include(r => r.Book)
                .Include(r => r.User)
                .FirstOrDefaultAsync(m => m.ReviewId == id);
            if (review == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin && review.UserId != userId)
            {
                return Forbid();
            }


            return View(review);
        }

        // GET: Reviews/Create
        public IActionResult CreateReview(int? id)
        {
            if (id != null)
            {
                ViewData["BookId"] = id;
            }
            else
            {
                ViewData["BookId"] = new SelectList(_context.Book, "BookId", "BookId");
            }

            ViewData["BookId"] = new SelectList(_context.Book, "BookId", "BookId");
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id");
            return View();
        }

        // POST: Reviews/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateReview([Bind("ReviewId,BookId,description,CreatedDate")] Review review)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            review.UserId = userId;

            review.CreatedDate = DateTime.UtcNow;

            _context.Add(review);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(GetReviews));
        }

        // GET: Reviews/Edit/5
        public async Task<IActionResult> EditReview(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }


            var review = await _context.Review
                .Include(r => r.Book)
                .FirstOrDefaultAsync(r => r.ReviewId == id);

            if (review == null)
            {
                return NotFound();
            }

            ViewData["BookId"] = new SelectList(_context.Book, "BookId", "BookId", review.BookId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", review.UserId);
            ViewBag.BookTitle = review.Book?.BookTitle;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin && review.UserId != userId)
            {
                return Forbid();
            }

            return View(review);
        }

        // POST: Reviews/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditReview(int id,
            [Bind("ReviewId,UserId,BookId,description,CreatedDate")] Review review)
        {
            if (id != review.ReviewId)
            {
                return NotFound();
            }

            try
            {
                _context.Update(review);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CheckReviewExists(review.ReviewId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToAction(nameof(GetReviews));
        }

        // GET: Reviews/Delete/5
        public async Task<IActionResult> DeleteReview(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var review = await _context.Review
                .Include(r => r.Book)
                .Include(r => r.User)
                .FirstOrDefaultAsync(m => m.ReviewId == id);
            if (review == null)
            {
                return NotFound();
            }
            
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin && review.UserId != userId)
            {
                return Forbid(); 
            }

            return View(review);
        }

        // POST: Reviews/Delete/5
        [HttpPost, ActionName("DeleteReview")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmReviewDeletion(int id)
        {
            var review = await _context.Review.FindAsync(id);
            if (review != null)
            {
                _context.Review.Remove(review);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(GetReviews));
        }

        private bool CheckReviewExists(int id)
        {
            return _context.Review.Any(e => e.ReviewId == id);
        }
    }
}