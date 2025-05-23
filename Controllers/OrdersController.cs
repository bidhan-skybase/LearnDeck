﻿using Microsoft.AspNetCore.Mvc;
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

        public OrdersController(ApplicationDbContext context, IEmailService emailService,
            UserManager<ApplicationUser> userManager)
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> CheckoutAllOrder()
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

            decimal originalTotal = 0;
            decimal fixedDiscount = 0;
            List<OrderItem> orderItems = new List<OrderItem>();

            foreach (var cartItem in cartItems)
            {
                var orderItem = new OrderItem
                {
                    OrderId = order.OrderId,
                    BookId = cartItem.BookId,
                    Quantity = cartItem.Quantity,
                    UnitPrice = cartItem.Book?.Price ?? 0 // Use original book price, handle null
                };

                // Calculate original price for this item
                decimal totalBookPrice = orderItem.Quantity * orderItem.UnitPrice;
                originalTotal += totalBookPrice;

                // Apply fixed discount: use cartItem.Book.DiscountAmount * quantity
                fixedDiscount += (cartItem.Book?.DiscountAmount ?? 0) * orderItem.Quantity;

                cartItem.Status = OrderStatus.PENDING; // Update to COMPLETED
                _context.CartItem.Update(cartItem);
                orderItems.Add(orderItem);
                _context.OrderItem.Add(orderItem);
                
                var stockResult = await ReduceBookStock(cartItem.BookId, cartItem.Quantity);
                if (!stockResult.Success)
                {
                    TempData["ErrorMessage"] = stockResult.ErrorMessage;
                    return RedirectToAction("ListCartItems", "CartItems");
                }
            }

            // Calculate total discount
            decimal totalDiscount = fixedDiscount;

            // Apply 5% discount if 5 or more items
            decimal percentageDiscount = cartItems.Count >= 5 ? originalTotal * 0.05m : 0;
            totalDiscount += percentageDiscount;

            // Apply 10% discount for 10+ successful orders
            var successfulOrders = await _context.Order
                .Where(o => o.UserId == userId &&
                            o.Status.ToString().ToLower() == OrderStatus.COMPLETED.ToString().ToLower())
                .CountAsync();
            decimal loyaltyDiscount = successfulOrders >= 10 ? originalTotal * 0.10m : 0;
            totalDiscount += loyaltyDiscount;

            // Calculate final total
            decimal finalTotal = originalTotal - totalDiscount;

            // Log for debugging
            Console.WriteLine(
                $"Order: OriginalTotal={originalTotal}, FixedDiscount={fixedDiscount}, " +
                $"PercentageDiscount={percentageDiscount}, LoyaltyDiscount={loyaltyDiscount}, " +
                $"TotalDiscount={totalDiscount}, FinalTotal={finalTotal}, ItemCount={cartItems.Count}");

            // Update order
            order.DiscountApplied = totalDiscount;
            order.TotalAmount = finalTotal;
            order.Status = OrderStatus.PENDING; // Update to COMPLETED
            _context.Order.Update(order);
            await _context.SaveChangesAsync();

            // Load order with items for email
            order.OrderItems = await _context.OrderItem
                .Where(oi => oi.OrderId == order.OrderId)
                .Include(oi => oi.Book)
                .ToListAsync();

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
                    <p>We will notify you when your order is ready for pickup.</p>
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
                Status = OrderStatus.PENDING,
            };

            _context.Add(order);
            await _context.SaveChangesAsync();

            var orderItem = new OrderItem
            {
                OrderId = order.OrderId,
                BookId = cartItem.BookId,
                Quantity = cartItem.Quantity,
                UnitPrice = cartItem.Book.Price // Use original book price
            };

            // Calculate original total: quantity * original book price
            decimal originalTotal = orderItem.Quantity * orderItem.UnitPrice;

            // Calculate discounts:
            // 1. Fixed discount: use book's discount amount * quantity
            decimal fixedDiscount = cartItem.Book.DiscountAmount * orderItem.Quantity;
            // 2. 5% discount on original total only if quantity >= 5
            decimal percentageDiscount = orderItem.Quantity >= 5 ? originalTotal * 0.05m : 0;
            // 3. 10% loyalty discount for 10+ successful orders
            var successfulOrders = await _context.Order
                .Where(o => o.UserId == userId &&
                            o.Status.ToString().ToLower() == OrderStatus.COMPLETED.ToString().ToLower())
                .CountAsync();
            decimal loyaltyDiscount = successfulOrders >= 10 ? originalTotal * 0.10m : 0;

            // Total discount: sum of all discounts
            decimal totalDiscount = fixedDiscount + percentageDiscount + loyaltyDiscount;

            // Final total: original total - total discount
            decimal finalTotal = originalTotal - totalDiscount;

            // Assign to order
            order.DiscountApplied = totalDiscount;
            order.TotalAmount = finalTotal;
            order.Status = OrderStatus.PENDING; // Update order status to reflect completion

            // Log for debugging
            Console.WriteLine(
                $"CartItem: BookPrice={cartItem.Book.Price}, UnitPrice={orderItem.UnitPrice}, " +
                $"Quantity={cartItem.Quantity}, FixedDiscount={fixedDiscount}, " +
                $"PercentageDiscount={percentageDiscount}, LoyaltyDiscount={loyaltyDiscount}, " +
                $"TotalDiscount={order.DiscountApplied}, OriginalTotal={originalTotal}, " +
                $"FinalTotal={order.TotalAmount}");
            
            
            var stockResult = await ReduceBookStock(cartItem.BookId, cartItem.Quantity);
            if (!stockResult.Success)
            {
                TempData["ErrorMessage"] = stockResult.ErrorMessage;
                return RedirectToAction("ListCartItems", "CartItems");
            }
            
            
            // Update cart item status
            cartItem.Status = OrderStatus.PENDING;
            _context.Order.Update(order);
            _context.CartItem.Update(cartItem);
            _context.OrderItem.Add(orderItem);
            await _context.SaveChangesAsync();

            // Load order with items for email
            order.OrderItems = await _context.OrderItem
                .Where(oi => oi.OrderId == order.OrderId)
                .Include(oi => oi.Book)
                .ToListAsync();
            
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
                    <p>We will notify you when your order is ready for pickup.</p>
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
            [Bind("OrderId,UserId,CreatedDate,TotalAmount,DiscountApplied,Status")]
            Order order)
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

                    var originalStatus = existingOrder.Status;
                    existingOrder.UserId = order.UserId;
                    existingOrder.CreatedDate = order.CreatedDate;
                    existingOrder.TotalAmount = order.TotalAmount;
                    existingOrder.DiscountApplied = order.DiscountApplied;
                    existingOrder.Status = order.Status;

                    await _context.SaveChangesAsync();
                    Console.WriteLine(order.Status);
                    // Send email if status changed to PACKAGED
                    if (originalStatus != OrderStatus.PACKAGED && order.Status == OrderStatus.PACKAGED)
                    {
                        var user = await _userManager.FindByIdAsync(order.UserId);
                        if (user != null)
                        {
                            try
                            {
                                var emailSubject = $"BookMart Order #{order.OrderId} - Packaged";
                                var emailBody = $@"<h2>Order Status Update</h2>
                            <p>Dear {user.FirstName} {user.LastName},</p>
                            <p>Your order #{order.OrderId} has been packaged and is ready for pick up {DateTime.UtcNow:dd MMM yyyy}.</p>
                            <p>Status: {order.Status}</p>
                       
                            <p>Thank you for shopping with BookMart!</p>";
                    
                                await _emailService.SendEmailAsync(user.Email, emailSubject, emailBody);
                                TempData["SuccessMessage"] =
                                    "Order updated successfully! A status update email has been sent.";
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Failed to send email: {ex.Message}");
                                TempData["SuccessMessage"] =
                                    "Order updated successfully, but failed to send status update email.";
                            }
                        }
                        else
                        {
                            TempData["SuccessMessage"] =
                                "Order updated successfully, but user not found for email notification.";
                        }
                    }
                    else
                    {
                        TempData["SuccessMessage"] = "Order updated successfully!";
                    }

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
        // GET: Orders/Delete/5
        [Authorize]
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
            // Load order and order items separately to avoid Include issue
            var order = await _context.Order
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                TempData["ErrorMessage"] = "Order not found.";
                return RedirectToAction(nameof(GetOrders));
            }

            // Load OrderItems explicitly
            var orderItems = await _context.OrderItem
                .Where(oi => oi.OrderId == id)
                .ToListAsync();

            // Use a transaction to ensure atomicity
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Increase stock for each order item
                foreach (var orderItem in orderItems)
                {
                    var stockResult = await IncreaseBookStock(orderItem.BookId, orderItem.Quantity);
                    if (!stockResult.Success)
                    {
                        await transaction.RollbackAsync();
                        TempData["ErrorMessage"] = stockResult.ErrorMessage;
                        return RedirectToAction(nameof(GetOrders));
                    }
                }

                // Delete the order
                _context.Order.Remove(order);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                TempData["SuccessMessage"] = "Order deleted successfully!";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Error deleting order: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while deleting the order.";
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
        
        
        // Helper function to reduce book stock
        private async Task<(bool Success, string ErrorMessage)> ReduceBookStock(int bookId, int quantity)
        {
            try
            {
                var book = await _context.Book.FindAsync(bookId);
                if (book == null)
                {
                    return (false, "Book not found.");
                }

                if (book.Stock < quantity)
                {
                    return (false, $"Insufficient stock for {book.BookTitle}. Available: {book.Stock}.");
                }

                book.Stock -= quantity;
                _context.Book.Update(book);
                await _context.SaveChangesAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reducing stock: {ex.Message}");
                return (false, "An error occurred while updating stock.");
            }
        }
        
        private async Task<(bool Success, string ErrorMessage)> IncreaseBookStock(int bookId, int quantity)
        {
            try
            {
                var book = await _context.Book.FindAsync(bookId);
                if (book == null)
                {
                    return (false, "Book not found.");
                }

                book.Stock += quantity;
                _context.Book.Update(book);
                await _context.SaveChangesAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error increasing stock: {ex.Message}");
                return (false, "An error occurred while updating stock.");
            }
        }


        private bool CheckOrderExists(int id)
        {
            return _context.Order.Any(e => e.OrderId == id);
        }
    }
}