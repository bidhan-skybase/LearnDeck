using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Ghayal_Bhaag.Models;
using Microsoft.AspNet.Identity;
using static System.Reflection.Metadata.BlobBuilder;

namespace Ghayal_Bhaag.Controllers
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
            var applicationDbContext = _context.Order.Include(o => o.User);
            return View(await applicationDbContext.ToListAsync());
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
            order.OrderItems = _context.OrderItem.Where(o => o.OrderId == id).Include(item => item.Book);

            return View(order);
        }

        // GET: Orders/Create
        public IActionResult CreateOrder()
        {
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id");
            return View();
        }

        // POST: Orders/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrder([Bind("OrderId,UserId,CreatedDate,TotalAmount,DiscountApplied,status")] Order order)
        {
            if (ModelState.IsValid)
            {
                _context.Add(order);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(GetOrders));
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", order.UserId);
            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        //Create a order and query the order
        //Get all the cart items that are in pending state
        //Convert all the cart item to order items in a loop and add to the database
        public async Task<IActionResult> AddOrdertoCart()
        {
            List<CartItem> cartItems = _context.CartItem.Where(item => item.Status == Enums.OrderStatus.PENDING).ToList();

            if (cartItems.Count > 0)
            {
                Order order = new Order();
                order.UserId = User.Identity.GetUserId();
                DateTime now = DateTime.UtcNow;
                order.CreatedDate = now;

                _context.Add(order);
                await _context.SaveChangesAsync();

                Order? added_order = _context.Order.Where(item => item.CreatedDate == now).First();

                float total_amount = 0;

                if(added_order != null)
                {
                    foreach(CartItem cart_item in  cartItems)
                    {
                        OrderItem order_item = new OrderItem();
                        order_item.Order = added_order;
                        order_item.OrderId = added_order.OrderId;
                        order_item.BookId = cart_item.BookId;
                        order_item.Quantity = (int)cart_item.Quantity;
                        order_item.UnitPrice = (float)cart_item.UnitPrice;

                        Book? book = _context.Book.Find(cart_item.BookId);

                        float total_book_price = order_item.Quantity * order_item.UnitPrice;


                        if (book != null)
                        {
                            if (book.Sale)
                            {
                                total_book_price -= book.DiscountAmount * order_item.Quantity;
                            }
                            total_amount += total_book_price;
                        }

                        cart_item.Status = Enums.OrderStatus.COMPLETED;
                        _context.CartItem.Update(cart_item);
                        _context.OrderItem.Add(order_item);
                    }

                    //Member can get 5 % discounts for an order of 5 + books
                    if (cartItems.Count >= 5)
                    {
                        float discount = 0;
                        discount = 5 / 100 * total_amount;
                        total_amount -= 5;
                    }

                    // 10% stackable discount after 10 successful orders

                    List<Order> succ_orders = _context.Order.Where(o=> o.UserId == User.Identity.GetUserId()).Where(o=>o.status == Enums.OrderStatus.COMPLETED).ToList();

                    if (succ_orders.Count == 10)
                    {
                        float discount = 0;
                        discount = 10 / 100 * total_amount;
                        total_amount -= 5;
                    }

                    added_order.TotalAmount = total_amount;
                    _context.Order.Update(added_order);
                }
                await _context.SaveChangesAsync();
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
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", order.UserId);
            return View(order);
        }

        // POST: Orders/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditOrder(int id, [Bind("OrderId,UserId,CreatedDate,TotalAmount,DiscountApplied,status")] Order order)
        {
            if (id != order.OrderId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(order);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CheckOrderExists(order.OrderId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(GetOrders));
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", order.UserId);
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

            return View(order);
        }

        // POST: Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmOrderDeletion(int id)
        {
            var order = await _context.Order.FindAsync(id);
            if (order != null)
            {
                _context.Order.Remove(order);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(GetOrders));
        }

        private bool CheckOrderExists(int id)
        {
            return _context.Order.Any(e => e.OrderId == id);
        }
    }
}
