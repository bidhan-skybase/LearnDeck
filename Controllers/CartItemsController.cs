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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            IQueryable<CartItem> cartItemsQuery = _context.CartItem
                .Include(c => c.User)
                .Include(c => c.Book)
                .Where(item => item.Status == OrderStatus.PENDING);

            if (!isAdmin)
            {
                cartItemsQuery = cartItemsQuery.Where(item => item.UserId == userId);
            }

            return View(await cartItemsQuery.ToListAsync());
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
            return RedirectToAction(nameof(EditCartItem));
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

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin && cartItem.UserId != userId)
            {
                return Forbid(); // Or redirect to an error page
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
        
        // POST: CartItems/UpdateQuantity
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> UpdateCartItemQuantity(int id, int quantity)
        {
            // Find the cart item
            var cartItem = await _context.CartItem
                .Include(c => c.Book)
                .FirstOrDefaultAsync(m => m.CartItemId == id);

            if (cartItem == null)
            {
                TempData["ErrorMessage"] = "Cart item not found.";
                return RedirectToAction(nameof(EditCartItem));
            }

            // Validate the user owns this cart item
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (cartItem.UserId != userId)
            {
                TempData["ErrorMessage"] = "You don't have permission to modify this cart item.";
                return RedirectToAction(nameof(EditCartItem));
            }

            // Validate the quantity
            if (quantity <= 0)
            {
                // If quantity is 0 or negative, remove the item from cart
                _context.CartItem.Remove(cartItem);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Item removed from cart.";
                return RedirectToAction(nameof(EditCartItem));
            }
            
            // Check if quantity exceeds available stock
            if (quantity > cartItem.Book.Stock)
            {
                quantity = cartItem.Book.Stock;
                TempData["ErrorMessage"] = $"Adjusted quantity to available stock ({cartItem.Book.Stock}).";
            }

            
            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin && cartItem.UserId != userId)
            {
                return Forbid(); // Or redirect to an error page
            }
            // Update the quantity
            cartItem.Quantity = quantity;
            _context.Update(cartItem);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cart item quantity updated successfully!";
            return RedirectToAction(nameof(EditCartItem));
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
            
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin && cartItem.UserId != userId)
            {
                return Forbid(); // Or redirect to an error page
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