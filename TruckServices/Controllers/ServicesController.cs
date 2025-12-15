using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Logging;
using System;
using System.Text.RegularExpressions;
using TruckServices.Data;

namespace TruckServices.Controllers
{
    public class ServicesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ServicesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ======================
        // LIST
        // ======================
        public async Task<IActionResult> Index()
        {
            var services = await _context.Services
                .OrderBy(s => s.Name)
                .ToListAsync();

            return View(services);
        }

        // ======================
        // CREATE
        // ======================
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Service model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
                return BadRequest("Service name is required.");

            string slug = Generate(model.Name);

            bool exists = await _context.Services
                .AnyAsync(x => x.Slug == slug);

            if (exists)
                return BadRequest("Service already exists.");

            var service = new Service
            {
                Name = model.Name.Trim(),
                Slug = slug,
                IsActive = model.IsActive
            };

            _context.Services.Add(service);
            await _context.SaveChangesAsync();

            return Ok(service);
        }

        // ======================
        // UPDATE
        // ======================
        [HttpPost]
        public async Task<IActionResult> Update([FromBody] Service model)
        {
            var service = await _context.Services.FindAsync(model.Id);
            if (service == null) return NotFound();

            service.Name = model.Name.Trim();
            service.Slug = Generate(model.Name);
            service.IsActive = model.IsActive;

            await _context.SaveChangesAsync();
            return Ok();
        }

        // ======================
        // DELETE
        // ======================
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var service = await _context.Services
                .Include(s => s.CompanyServices)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (service == null) return NotFound();

            if (service.CompanyServices.Any())
                return BadRequest("Service is assigned to companies.");

            _context.Services.Remove(service);
            await _context.SaveChangesAsync();

            return Ok();
        }

        public static string Generate(string text)
        {
            text = text.ToLowerInvariant();
            text = Regex.Replace(text, @"[^a-z0-9\s-]", "");
            text = Regex.Replace(text, @"\s+", "-").Trim('-');
            return text;
        }
    }

}
