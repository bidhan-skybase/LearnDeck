using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Ghayal_Bhaag.Models;

namespace Ghayal_Bhaag.Controllers
{
    public class OrderItemsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrderItemsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: OrderItems
        public async Task<IActionResult> GetOrderItems()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            IQueryable<OrderItem> orderItemsQuery = _context.OrderItem
                .Include(o => o.Book)
                .Include(o => o.Order);

            if (!isAdmin)
            {
                orderItemsQuery = orderItemsQuery.Where(o => o.Order.UserId == userId); // Assuming Order has UserId
            }

            return View(await orderItemsQuery.ToListAsync());
        }


        // GET: OrderItems/Details/5
        public async Task<IActionResult> GetOrderItemDetail(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var orderItem = await _context.OrderItem
                .Include(o => o.Book)
                .Include(o => o.Order)
                .FirstOrDefaultAsync(m => m.OrderItemId == id);

            if (orderItem == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            
            if (!isAdmin && orderItem.Order.UserId != userId)
            {
                return Forbid(); 
            }

            return View(orderItem);
        }


        // GET: OrderItems/Create
        public IActionResult CreateOrderItem()
        {
            ViewData["BookId"] = new SelectList(_context.Book, "BookId", "BookId");
            ViewData["OrderId"] = new SelectList(_context.Set<Order>(), "OrderId", "OrderId");
            
            return View();
        }

        // POST: OrderItems/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrderItem([Bind("OrderItemId,OrderId,BookId,Quantity,UnitPrice")] OrderItem orderItem)
        {
            if (ModelState.IsValid)
            {
                _context.Add(orderItem);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(GetOrderItems));
            }
            ViewData["BookId"] = new SelectList(_context.Book, "BookId", "BookId", orderItem.BookId);
            ViewData["OrderId"] = new SelectList(_context.Set<Order>(), "OrderId", "OrderId", orderItem.OrderId);
            return View(orderItem);
        }

        // GET: OrderItems/Edit/5
        public async Task<IActionResult> EditOrderItem(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            
            

            var orderItem = await _context.OrderItem.FindAsync(id);
            if (orderItem == null)
            {
                return NotFound();
            }
            ViewData["BookId"] = new SelectList(_context.Book, "BookId", "BookId", orderItem.BookId);
            ViewData["OrderId"] = new SelectList(_context.Set<Order>(), "OrderId", "OrderId", orderItem.OrderId);
            return View(orderItem);
        }

        // POST: OrderItems/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditOrderItem(int id, [Bind("OrderItemId,OrderId,BookId,Quantity,UnitPrice")] OrderItem orderItem)
        {
            if (id != orderItem.OrderItemId)
            {
                return NotFound();
            }
            
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            
            if (!isAdmin && orderItem.Order.UserId != userId)
            {
                return Forbid(); 
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(orderItem);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CheckOrderItemExists(orderItem.OrderItemId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(GetOrderItems));
            }
            ViewData["BookId"] = new SelectList(_context.Book, "BookId", "BookId", orderItem.BookId);
            ViewData["OrderId"] = new SelectList(_context.Set<Order>(), "OrderId", "OrderId", orderItem.OrderId);
            return View(orderItem);
        }

        // GET: OrderItems/Delete/5
        public async Task<IActionResult> DeleteOrderItem(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var orderItem = await _context.OrderItem
                .Include(o => o.Book)
                .Include(o => o.Order)
                .FirstOrDefaultAsync(m => m.OrderItemId == id);
            if (orderItem == null)
            {
                return NotFound();
            }
            
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            
            if (!isAdmin && orderItem.Order.UserId != userId)
            {
                return Forbid(); 
            }

            return View(orderItem);
        }

        // POST: OrderItems/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmOrderItemDeletion(int id)
        {
            var orderItem = await _context.OrderItem.FindAsync(id);
            if (orderItem != null)
            {
                _context.OrderItem.Remove(orderItem);
            }
            
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            
            if (!isAdmin && orderItem.Order.UserId != userId)
            {
                return Forbid(); 
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(GetOrderItems));
        }

        private bool CheckOrderItemExists(int id)
        {
            return _context.OrderItem.Any(e => e.OrderItemId == id);
        }
    }
}
