using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using TruckServices.Data;

namespace TruckServices.Controllers
{
    public class CustomersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CustomersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Customers
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string? search = null)
        {
            var query = _context.CustomersData.AsQueryable();

            // Apply filter ONLY if search is provided and not empty
            if (!string.IsNullOrWhiteSpace(search))
            {
                string searchLower = search.ToLower();

                query = query.Where(c =>
                    c.CompanyName.ToLower().Contains(searchLower) ||
                    c.City.ToLower().Contains(searchLower) ||
                    c.Email.ToLower().Contains(searchLower) ||
                    c.MobileNumber.Contains(search));
            }

            int totalItems = await query.CountAsync();

            var customers = await query
                .OrderBy(c => c.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.Search = search ?? ""; // so text box keeps the value

            return View(customers);
        }

        // GET: Customers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var customer = await _context.CustomersData.FirstOrDefaultAsync(c => c.Id == id);
            if (customer == null) return NotFound();

            return View(customer);
        }

        // GET: Customers/Create
        public IActionResult Create()
        {
            return PartialView("Partial/_CreateEditPartial", new CustomersData());
        }

        // POST: Customers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomersData customer, IFormFile? image)
        {
            if (ModelState.IsValid)
            {
                if (image != null)
                {
                    using var ms = new MemoryStream();
                    await image.CopyToAsync(ms);
                    customer.ImageUrl = ms.ToArray();
                }

                _context.Add(customer);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(customer);
        }

        // GET: Customers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var customer = await _context.CustomersData.FindAsync(id);
            if (customer == null) return NotFound();

            // This passes the whole model to your partial view
            return PartialView("Partial/_CreateEditPartial", customer);
        }


        // POST: Customers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CustomersData customer, IFormFile? image)
        {
            if (id != customer.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingCustomer = await _context.CustomersData.FindAsync(id);
                    if (existingCustomer == null) return NotFound();

                    // Update only what’s editable
                    existingCustomer.CompanyName = customer.CompanyName;
                    existingCustomer.StreetAddress = customer.StreetAddress;
                    existingCustomer.City = customer.City;
                    existingCustomer.State = customer.State;
                    existingCustomer.Country = customer.Country;
                    existingCustomer.MobileNumber = customer.MobileNumber;
                    existingCustomer.SecondMobileNumber = customer.SecondMobileNumber;
                    existingCustomer.Email = customer.Email;
                    existingCustomer.IsPaid = customer.IsPaid;
                    // Skip Source (not editable)

                    if (image != null)
                    {
                        using var ms = new MemoryStream();
                        await image.CopyToAsync(ms);
                        existingCustomer.ImageUrl = ms.ToArray();
                    }

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.CustomersData.Any(e => e.Id == customer.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            return PartialView("Partial/_CreateEditPartial", customer);
        }

        // GET: Customers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var customer = await _context.CustomersData.FindAsync(id);
            if (customer == null) return NotFound();

            return PartialView("Partial/_DeletePartial", customer);
        }

        // POST: Customers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var customer = await _context.CustomersData.FindAsync(id);
            _context.CustomersData.Remove(customer);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // Optional: serve image
        public IActionResult GetImage(int id)
        {
            var customer = _context.CustomersData.Find(id);
            if (customer == null || customer.ImageUrl == null) return NotFound();

            return File(customer.ImageUrl, "image/jpeg"); // adjust mime type if needed
        }
    }
}
