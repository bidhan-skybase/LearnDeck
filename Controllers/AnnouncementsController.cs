using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BookMart.Models;

namespace BookMart.Controllers
{
    public class AnnouncementsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AnnouncementsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Anouncements
        public async Task<IActionResult> GetAnnouncements()
        {
            return View(await _context.Anouncement.ToListAsync());
        }

        // GET: Anouncements/Details/5
        public async Task<IActionResult> GetAnnouncementDetails(int? id)
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
        public IActionResult CreateAnnouncement()
        {
            return View();
        }

        // POST: Anouncements/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAnnouncement([Bind("AnouncementId,Title,Description,StartDate,EndDate")] Anouncement anouncement)
        {
            if (ModelState.IsValid)
            {
                _context.Add(anouncement);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(GetAnnouncements));
            }
            return View(anouncement);
        }

        // GET: Anouncements/Edit/5
        public async Task<IActionResult> EditAnnouncement(int? id)
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
        public async Task<IActionResult> EditAnnouncement(int id, [Bind("AnouncementId,Title,Description,StartDate,EndDate")] Anouncement anouncement)
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
                    if (!CheckAnnouncementExists(anouncement.AnouncementId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(GetAnnouncements));
            }
            return View(anouncement);
        }

        // GET: Anouncements/Delete/5
        public async Task<IActionResult> DeleteAnnouncement(int? id)
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
        [HttpPost, ActionName("DeleteAnnouncement")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmDeletion(int id)
        {
            var anouncement = await _context.Anouncement.FindAsync(id);
            if (anouncement != null)
            {
                _context.Anouncement.Remove(anouncement);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(GetAnnouncements));
        }

        private bool CheckAnnouncementExists(int id)
        {
            return _context.Anouncement.Any(e => e.AnouncementId == id);
        }
    }
}
