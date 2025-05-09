using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Ghayal_Bhaag.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
            var applicationDbContext = _context.CartItem.Include(c => c.User).Where(item=>item.Status == Enums.OrderStatus.PENDING);
            return View(await applicationDbContext.ToListAsync());
        }
        
        public IActionResult CreateCartItem(int? id)
        {
            if (id != null)
            {
                Book? book = _context.Book.Find(id);
                if (book != null)
                {
                    ViewData["UnitPrice"] = book.Price;
                }
                ViewData["BookId"] = id;
            }
            else
            {
                ViewData["BookId"] = new SelectList(_context.Book, "BookId", "BookId");
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id");
            return View();
        }
        
        // IMPORTANT: Keep this method as it might still be referenced from other places
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCartItem([Bind("CartItemId,UserId,BookId,Quantity,UnitPrice")] CartItem cartItem)
        {
            cartItem.Status = OrderStatus.PENDING;
            _context.Add(cartItem);
            await _context.SaveChangesAsync();
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
                .FirstOrDefaultAsync(m => m.CartItemId == id);
            if (cartItem == null)
            {
                return NotFound();
            }

            return View(cartItem);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int id, int quantity)
        {
            // Find the book
            Book? book = await _context.Book.FindAsync(id);
            if (book == null)
            {
                return NotFound();
            }
            
            // Validate quantity against stock
            if (quantity <= 0)
            {
                quantity = 1;
            }
            else if (quantity > book.Stock)
            {
                quantity = book.Stock;
            }
            
            // Get current user ID - replace this with your actual user ID retrieval method
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // If using Identity
            
            // Create cart item
            CartItem cartItem = new CartItem
            {
                UserId = userId,
                BookId = id,
                Quantity = quantity,
                UnitPrice = book.Price,
                Status = OrderStatus.PENDING
            };
            
            // Add to database
            _context.Add(cartItem);
            await _context.SaveChangesAsync();
            
            // Redirect to cart
            return RedirectToAction(nameof(ListCartItems));
        }

        // GET: CartItems/Edit/5
        public async Task<IActionResult> EditCartItem(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cartItem = await _context.CartItem.FindAsync(id);
            if (cartItem == null)
            {
                return NotFound();
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", cartItem.UserId);
            return View(cartItem);
        }

        // POST: CartItems/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCartItem(int id, [Bind("CartItemId,UserId,BookId,Quantity,UnitPrice")] CartItem cartItem)
        {
            if (id != cartItem.CartItemId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(cartItem);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CheckCartItemExists(cartItem.CartItemId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(ListCartItems));
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", cartItem.UserId);
            return View(cartItem);
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
                .FirstOrDefaultAsync(m => m.CartItemId == id);
            if (cartItem == null)
            {
                return NotFound();
            }

            return View(cartItem);
        }

        // POST: CartItems/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmCartItemDeletion(int id)
        {
            var cartItem = await _context.CartItem.FindAsync(id);
            if (cartItem != null)
            {
                _context.CartItem.Remove(cartItem);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ListCartItems));
        }

        private bool CheckCartItemExists(int id)
        {
            return _context.CartItem.Any(e => e.CartItemId == id);
        }
    }
}
