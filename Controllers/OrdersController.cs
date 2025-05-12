using Microsoft.AspNetCore.Mvc;
using BookMart.Services;
using BookMart.Areas.Identity.Data;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using BookMart.Models;
using BookMart.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BookMart.Controllers
{
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrdersController(ApplicationDbContext context, IEmailService emailService, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _emailService = emailService;
            _userManager = userManager;
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
        public async Task<IActionResult> CreateOrder(
            [Bind("OrderId,UserId,CreatedDate,TotalAmount,DiscountApplied,Status")]
            Order order)
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
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "User not logged in.";
                return RedirectToAction("Login", "Account");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
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

            // Load order with items for email
            order.OrderItems = await _context.OrderItem
                .Where(oi => oi.OrderId == order.OrderId)
                .Include(oi => oi.Book)
                .ToListAsync();

            // Send confirmation email
            try
            {
                var emailSubject = $"BookMart Order Confirmation - Order #{order.OrderId}";
                var emailBody = $@"<h2>Thank You for Your Order!</h2>
                    <p>Dear {user.FirstName} {user.LastName},</p>
                    <p>Your order #{order.OrderId} has been successfully placed on {order.CreatedDate:dd MMM yyyy}.</p>
                    <h3>Order Details</h3>
                    <ul>
                        {string.Join("", order.OrderItems.Select(oi => $"<li>{oi.Book.BookTitle} (x{oi.Quantity}) - ${oi.UnitPrice * oi.Quantity}</li>"))}
                    </ul>
                    {(order.DiscountApplied > 0 ? $"<p>Discount Applied: ${order.DiscountApplied}</p>" : "")}
                    <p><strong>Total: ${order.TotalAmount}</strong></p>
                    <p>Status: {order.Status}</p>
                    <p>We will notify you when your order is shipped.</p>
                    <p>Thank you for shopping with BookMart!</p>";

                await _emailService.SendEmailAsync(user.Email, emailSubject, emailBody);
                TempData["SuccessMessage"] = "Order created successfully! A confirmation email has been sent.";
            }
            catch (Exception ex)
            {
                // Log the error (consider using ILogger)
                Console.WriteLine($"Failed to send email: {ex.Message}");
                TempData["SuccessMessage"] = "Order created successfully, but failed to send confirmation email.";
            }

            return RedirectToAction(nameof(GetOrders));
        }

        // POST: Orders/CheckoutSingleCartItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> CheckoutSingleCartItem(int id)
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "User not logged in.";
                return RedirectToAction("Login", "Account");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Login", "Account");
            }

            var cartItem = await _context.CartItem
                .Include(item => item.Book)
                .FirstOrDefaultAsync(item =>
                    item.CartItemId == id && item.UserId == userId && item.Status == OrderStatus.PENDING);

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

            // Load order with items for email
            order.OrderItems = await _context.OrderItem
                .Where(oi => oi.OrderId == order.OrderId)
                .Include(oi => oi.Book)
                .ToListAsync();

            // Send confirmation email
            try
            {
                var emailSubject = $"BookMart Order Confirmation - Order #{order.OrderId}";
                var emailBody = $@"<h2>Thank You for Your Order!</h2>
                    <p>Dear {user.FirstName} {user.LastName},</p>
                    <p>Your order #{order.OrderId} has been successfully placed on {order.CreatedDate:dd MMM yyyy}.</p>
                    <h3>Order Details</h3>
                    <ul>
                        {string.Join("", order.OrderItems.Select(oi => $"<li>{oi.Book.BookTitle} (x{oi.Quantity}) - ${oi.UnitPrice * oi.Quantity}</li>"))}
                    </ul>
                    {(order.DiscountApplied > 0 ? $"<p>Discount Applied: ${order.DiscountApplied}</p>" : "")}
                    <p><strong>Total: ${order.TotalAmount}</strong></p>
                    <p>Status: {order.Status}</p>
                    <p>We will notify you when your order is shipped.</p>
                    <p>Thank you for shopping with BookMart!</p>";

                await _emailService.SendEmailAsync(user.Email, emailSubject, emailBody);
                TempData["SuccessMessage"] = "Order created successfully! A confirmation email has been sent.";
            }
            catch (Exception ex)
            {
                // Log the error (consider using ILogger)
                Console.WriteLine($"Failed to send email: {ex.Message}");
                TempData["SuccessMessage"] = "Order created successfully, but failed to send confirmation email.";
            }

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
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditOrder(int id,
            [Bind("OrderId,UserId,CreatedDate,TotalAmount,DiscountApplied,Status")] Order order)
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

            // Remove the User validation error specifically
            ModelState.Remove("User");

            if (ModelState.IsValid)
            {
                try
                {
                    var existingOrder = await _context.Order.FindAsync(id);
                    if (existingOrder == null)
                    {
                        return NotFound();
                    }

                    existingOrder.UserId = order.UserId;
                    existingOrder.CreatedDate = order.CreatedDate;
                    existingOrder.TotalAmount = order.TotalAmount;
                    existingOrder.DiscountApplied = order.DiscountApplied;
                    existingOrder.Status = order.Status;

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Order updated successfully!";
                    return RedirectToAction(nameof(GetOrders));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CheckOrderExists(order.OrderId))
                    {
                        return NotFound();
                    }
                    throw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error updating order: {ex.Message}");
                    ModelState.AddModelError("", "An error occurred while updating the order: " + ex.Message);
                }
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
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (order.UserId != userId)
                {
                    TempData["ErrorMessage"] = "You are not authorized to cancel this order.";
                    return RedirectToAction(nameof(GetOrders));
                }

                if (order.Status == OrderStatus.COMPLETED)
                {
                    TempData["ErrorMessage"] = "Completed orders cannot be cancelled.";
                    return RedirectToAction(nameof(GetOrders));
                }

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