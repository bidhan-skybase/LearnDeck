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
    public class AnouncementsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AnouncementsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Anouncements
        public async Task<IActionResult> Index()
        {
            return View(await _context.Anouncement.ToListAsync());
        }

        // GET: Anouncements/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var anouncement = await _context.Anouncement
                .FirstOrDefaultAsync(m => m.AnouncementId == id);
            if (anouncement == null)
            {
                return NotFound();
            }

            return View(anouncement);
        }

        // GET: Anouncements/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Anouncements/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AnouncementId,title,description,StartDate,EndDate")] Anouncement anouncement)
        {
            if (ModelState.IsValid)
            {
                _context.Add(anouncement);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(anouncement);
        }

        // GET: Anouncements/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var anouncement = await _context.Anouncement.FindAsync(id);
            if (anouncement == null)
            {
                return NotFound();
            }
            return View(anouncement);
        }

        // POST: Anouncements/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("AnouncementId,title,description,StartDate,EndDate")] Anouncement anouncement)
        {
            if (id != anouncement.AnouncementId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(anouncement);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AnouncementExists(anouncement.AnouncementId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(anouncement);
        }

        // GET: Anouncements/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var anouncement = await _context.Anouncement
                .FirstOrDefaultAsync(m => m.AnouncementId == id);
            if (anouncement == null)
            {
                return NotFound();
            }

            return View(anouncement);
        }

        // POST: Anouncements/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var anouncement = await _context.Anouncement.FindAsync(id);
            if (anouncement != null)
            {
                _context.Anouncement.Remove(anouncement);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AnouncementExists(int id)
        {
            return _context.Anouncement.Any(e => e.AnouncementId == id);
        }
    }
}
