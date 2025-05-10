using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Ghayal_Bhaag.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ghayal_Bhaag.Models;

namespace Ghayal_Bhaag.Controllers
{
    public class CartItemsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CartItemsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: CartItems
        public async Task<IActionResult> ListCartItems()
        {
            var cartItems = _context.CartItem
                .Include(c => c.User)
                .Include(c => c.Book)
                .Where(item => item.Status == OrderStatus.PENDING);
            return View(await cartItems.ToListAsync());
        }

        // POST: CartItems/AddToCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> AddToCart(int id, int quantity)
        {
            var book = await _context.Book.FindAsync(id);
            if (book == null)
            {
                TempData["ErrorMessage"] = "Book not found.";
                return RedirectToAction("Index", "Books");
            }

            if (quantity <= 0)
            {
                quantity = 1;
            }
            else if (quantity > book.Stock)
            {
                quantity = book.Stock;
                TempData["ErrorMessage"] = $"Adjusted quantity to available stock ({book.Stock}).";
            }

            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "User not logged in.";
                return RedirectToAction("Login", "Account");
            }

            CartItem cartItem = new CartItem
            {
                UserId = userId,
                BookId = id,
                Quantity = quantity,
                UnitPrice = (decimal)book.Price,
                Status = OrderStatus.PENDING
            };

            _context.Add(cartItem);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Item added to cart!";
            return RedirectToAction(nameof(ListCartItems));
        }

        // GET: CartItems/Details/5
        public async Task<IActionResult> GetCartDetails(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cartItem = await _context.CartItem
                .Include(c => c.User)
                .Include(c => c.Book)
                .FirstOrDefaultAsync(m => m.CartItemId == id);
            if (cartItem == null)
            {
                return NotFound();
            }

            return View(cartItem);
        }

        // GET: CartItems/Edit
        [Authorize]
        public async Task<IActionResult> EditCartItem()
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "User not logged in.";
                return RedirectToAction("Login", "Account");
            }

            var cartItems = await _context.CartItem
                .Include(c => c.User)
                .Include(c => c.Book)
                .Where(c => c.UserId == userId && c.Status == OrderStatus.PENDING)
                .ToListAsync();

            return View(cartItems);
        }
        

        // GET: CartItems/Delete/5
        public async Task<IActionResult> DeleteCartItem(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cartItem = await _context.CartItem
                .Include(c => c.User)
                .Include(c => c.Book)
                .FirstOrDefaultAsync(m => m.CartItemId == id);
            if (cartItem == null)
            {
                return NotFound();
            }

            return View(cartItem);
        }

        // POST: CartItems/Delete/5
        [HttpPost, ActionName("DeleteCartItem")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ConfirmCartItemDeletion(int id)
        {
            var cartItem = await _context.CartItem.FindAsync(id);
            if (cartItem == null)
            {
                TempData["ErrorMessage"] = "Cart item not found.";
            }
            else
            {
                _context.CartItem.Remove(cartItem);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cart item deleted successfully!";
            }

            return RedirectToAction(nameof(ListCartItems));
        }

        private bool CheckCartItemExists(int id)
        {
            return _context.CartItem.Any(e => e.CartItemId == id);
        }
    }
}