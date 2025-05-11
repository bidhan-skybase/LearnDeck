using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNet.Identity;
using BookMart.Models;
using BookMart.Enums;

namespace BookMart.Controllers
{
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Orders
        public async Task<IActionResult> GetOrders()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            IQueryable<Order> ordersQuery = _context.Order.Include(o => o.User);
            
            if (!isAdmin)
            {
                ordersQuery = ordersQuery.Where(o => o.UserId == userId);
            }

            return View(await ordersQuery.ToListAsync());
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> GetOrderDetail(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Order
                .Include(o => o.User)
                .FirstOrDefaultAsync(m => m.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); 
            var isAdmin = User.IsInRole("Admin");
            
            if (!isAdmin && order.UserId != userId)
            {
                return Forbid(); 
            }
            
            order.OrderItems = await _context.OrderItem
                .Where(o => o.OrderId == id)
                .Include(item => item.Book)
                .ToListAsync();

            return View(order);
        }


        // GET: Orders/Create
        public IActionResult CreateOrder()
        {
            
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "UserName");
            return View();
        }

        // POST: Orders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> CreateOrder([Bind("OrderId,UserId,CreatedDate,TotalAmount,DiscountApplied,Status")] Order order)
        {
            
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); 
            var isAdmin = User.IsInRole("Admin");
            
            if (!isAdmin && order.UserId != userId)
            {
                return Forbid(); 
            }
            if (ModelState.IsValid)
            {
                _context.Add(order);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Order created successfully!";
                return RedirectToAction(nameof(GetOrders));
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "UserName", order.UserId);
            TempData["ErrorMessage"] = "Please correct the errors and try again.";
            return View(order);
        }

        // POST: Orders/AddOrdertoCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> AddOrdertoCart()
        {
            string userId = User.Identity.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "User not logged in.";
                return RedirectToAction("Login", "Account");
            }

            var cartItems = await _context.CartItem
                .Where(item => item.Status == OrderStatus.PENDING && item.UserId == userId)
                .Include(item => item.Book)
                .ToListAsync();

            if (!cartItems.Any())
            {
                TempData["ErrorMessage"] = "No pending cart items to checkout.";
                return RedirectToAction("ListCartItems", "CartItems");
            }

            var order = new Order
            {
                UserId = userId,
                CreatedDate = DateTime.UtcNow,
                Status = OrderStatus.PENDING
            };

            _context.Add(order);
            await _context.SaveChangesAsync();

            decimal totalAmount = 0;
            foreach (var cartItem in cartItems)
            {
                var orderItem = new OrderItem
                {
                    OrderId = order.OrderId,
                    BookId = cartItem.BookId,
                    Quantity = cartItem.Quantity,
                    UnitPrice = cartItem.UnitPrice
                };

                decimal totalBookPrice = orderItem.Quantity * orderItem.UnitPrice;
                if (cartItem.Book.Sale)
                {
                    totalBookPrice -= cartItem.Book.DiscountAmount * orderItem.Quantity;
                }
                totalAmount += totalBookPrice;

                cartItem.Status = OrderStatus.COMPLETED;
                _context.CartItem.Update(cartItem);
                _context.OrderItem.Add(orderItem);
            }

            // Apply discounts
            if (cartItems.Count >= 5)
            {
                decimal discount = totalAmount * 0.05m; // 5% discount
                totalAmount -= discount;
                order.DiscountApplied = (order.DiscountApplied ?? 0) + discount;
            }

            var successfulOrders = await _context.Order
                .Where(o => o.UserId == userId && o.Status == OrderStatus.COMPLETED)
                .CountAsync();
            if (successfulOrders >= 10)
            {
                decimal discount = totalAmount * 0.10m; // 10% discount
                totalAmount -= discount;
                order.DiscountApplied = (order.DiscountApplied ?? 0) + discount;
            }

            order.TotalAmount = totalAmount;
            _context.Order.Update(order);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Order created successfully!";
            return RedirectToAction(nameof(GetOrders));
        }

        // POST: Orders/CheckoutSingleCartItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> CheckoutSingleCartItem(int id)
        {
            
            string userId = User.Identity.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "User not logged in.";
                return RedirectToAction("Login", "Account");
            }

            var cartItem = await _context.CartItem
                .Include(item => item.Book)
                .FirstOrDefaultAsync(item => item.CartItemId == id && item.UserId == userId && item.Status == OrderStatus.PENDING);

            if (cartItem == null)
            {
                TempData["ErrorMessage"] = "Cart item not found or already processed.";
                return RedirectToAction("ListCartItems", "CartItems");
            }

            var order = new Order
            {
                UserId = userId,
                CreatedDate = DateTime.UtcNow,
                Status = OrderStatus.PENDING
            };

            _context.Add(order);
            await _context.SaveChangesAsync();

            var orderItem = new OrderItem
            {
                OrderId = order.OrderId,
                BookId = cartItem.BookId,
                Quantity = cartItem.Quantity,
                UnitPrice = cartItem.UnitPrice
            };

            decimal totalAmount = orderItem.Quantity * orderItem.UnitPrice;
            if (cartItem.Book.Sale)
            {
                totalAmount -= cartItem.Book.DiscountAmount * orderItem.Quantity;
            }

            // Apply loyalty discount
            var successfulOrders = await _context.Order
                .Where(o => o.UserId == userId && o.Status == OrderStatus.COMPLETED)
                .CountAsync();
            if (successfulOrders >= 10)
            {
                decimal discount = totalAmount * 0.10m; // 10% discount
                totalAmount -= discount;
                order.DiscountApplied = discount;
            }

            order.TotalAmount = totalAmount;
            cartItem.Status = OrderStatus.COMPLETED;
            _context.Order.Update(order);
            _context.CartItem.Update(cartItem);
            _context.OrderItem.Add(orderItem);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Order created successfully for the selected item!";
            return RedirectToAction(nameof(GetOrders));
        }

        // GET: Orders/Edit/5
        public async Task<IActionResult> EditOrder(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Order.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); 
            var isAdmin = User.IsInRole("Admin");
            
            if (!isAdmin && order.UserId != userId)
            {
                return Forbid(); 
            }
            
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "UserName", order.UserId);
            return View(order);
        }

        // POST: Orders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> EditOrder(int id, [Bind("OrderId,UserId,CreatedDate,TotalAmount,DiscountApplied,Status")] Order order)
        {
            if (id != order.OrderId)
            {
                return NotFound();
            }
            
            
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); 
            var isAdmin = User.IsInRole("Admin");
            
            if (!isAdmin && order.UserId != userId)
            {
                return Forbid(); 
            }
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(order);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Order updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CheckOrderExists(order.OrderId))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction(nameof(GetOrders));
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "UserName", order.UserId);
            TempData["ErrorMessage"] = "Please correct the errors and try again.";
            return View(order);
        }

        // GET: Orders/Delete/5
        public async Task<IActionResult> DeleteOrder(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            
            var order = await _context.Order
                .Include(o => o.User)
                .FirstOrDefaultAsync(m => m.OrderId == id);
            if (order == null)
            {
                return NotFound();
            }
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); 
            var isAdmin = User.IsInRole("Admin");
            
            if (!isAdmin && order.UserId != userId)
            {
                return Forbid(); 
            }
            return View(order);
        }

        // POST: Orders/Delete/5
        [HttpPost, ActionName("DeleteOrder")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ConfirmOrderDeletion(int id)
        {
            var order = await _context.Order.FindAsync(id);
            if (order == null)
            {
                TempData["ErrorMessage"] = "Order not found.";
            }
            else
            {
                _context.Order.Remove(order);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Order deleted successfully!";
            }

            return RedirectToAction(nameof(GetOrders));
        }
        
        
                // GET: Orders/Cancel/5
        [Authorize]
        public async Task<IActionResult> CancelOrder(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Order
                .Include(o => o.User)
                .FirstOrDefaultAsync(m => m.OrderId == id);
            if (order == null)
            {
                return NotFound();
            }

            // Ensure the logged-in user can only cancel their own orders
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (order.UserId != userId)
            {
                return Forbid();
            }

            return View(order);
            
        }

        // POST: Orders/Cancel/5
        [HttpPost, ActionName("CancelOrder")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ConfirmOrderCancellation(int id)
        {
            var order = await _context.Order.FindAsync(id);
            if (order == null)
            {
                TempData["ErrorMessage"] = "Order not found.";
            }
            else
            {
                // Ensure the logged-in user can only cancel their own orders
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (order.UserId != userId)
                {
                    TempData["ErrorMessage"] = "You are not authorized to cancel this order.";
                    return RedirectToAction(nameof(GetOrders));
                }

                // Prevent cancelling completed orders
                if (order.Status == OrderStatus.COMPLETED)
                {
                    TempData["ErrorMessage"] = "Completed orders cannot be cancelled.";
                    return RedirectToAction(nameof(GetOrders));
                }

                // Update the order status to CANCELLED
                order.Status = OrderStatus.CANCELLED;
                _context.Update(order);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Order cancelled successfully!";
            }

            return RedirectToAction(nameof(GetOrders));
        }
    

        private bool CheckOrderExists(int id)
        {
            return _context.Order.Any(e => e.OrderId == id);
        }
        
        
    }
}