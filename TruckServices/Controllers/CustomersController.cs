using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using TruckServices.Data;
using TruckServices.Models.ViewModels;

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
        public async Task<IActionResult> Create()
        {
            var vm = new CustomerServiceVM
            {
                Customer = new CustomersData(),
                Services = await _context.Services
            .Where(s => s.IsActive)
            .Select(s => new ServiceCheckboxVM
            {
                ServiceId = s.Id,
                Name = s.Name,
                IsSelected = false
            })
            .ToListAsync()
            };
            return PartialView("Partial/_CreatePartial", vm);
        }

        // POST: Customers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerServiceVM vm, IFormFile? image)
        {
            if (!ModelState.IsValid)
                return PartialView("Partial/_CreateCustomer", vm);

            if (image != null)
            {
                using var ms = new MemoryStream();
                await image.CopyToAsync(ms);
                vm.Customer.ImageUrl = ms.ToArray();
            }

            _context.CustomersData.Add(vm.Customer);
            await _context.SaveChangesAsync();

            // 🔗 Save M-M relations
            var selectedServices = vm.Services
                .Where(x => x.IsSelected)
                .Select(x => new CompanyService
                {
                    CompanyId = vm.Customer.Id,
                    ServiceId = x.ServiceId
                });

            _context.CompanyServices.AddRange(selectedServices);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Customers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            try
            {
                if (id == null) return NotFound();

                var customer = await _context.CustomersData
    .Include(c => c.CompanyServices)
    .FirstOrDefaultAsync(c => c.Id == id);

                var selectedServiceIds = customer.CompanyServices
                    .Select(cs => cs.ServiceId)
                    .ToHashSet();

                var services = await _context.Services.ToListAsync();

                var vm = new CustomerServiceVM
                {
                    Customer = customer,
                    Services = services.Select(s => new ServiceCheckboxVM
                    {
                        ServiceId = s.Id,
                        Name = s.Name,
                        IsSelected = selectedServiceIds.Contains(s.Id)
                    }).ToList()
                };


                // This passes the whole model to your partial view
                return PartialView("Partial/_EditPartial", vm);
            }
            catch (Exception ex)
            {

                throw;
            }
            
        }


        // POST: Customers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CustomerServiceVM vm, IFormFile? image)
        {
            if (id != vm.Customer.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var customer = await _context.CustomersData
                        .Include(c => c.CompanyServices)
                        .FirstOrDefaultAsync(c => c.Id == id);

                    if (customer == null) return NotFound();

                    // Update fields
                    customer.CompanyName = vm.Customer.CompanyName;
                    customer.StreetAddress = vm.Customer.StreetAddress;
                    customer.City = vm.Customer.City;
                    customer.State = vm.Customer.State;
                    customer.Country = vm.Customer.Country;
                    customer.MobileNumber = vm.Customer.MobileNumber;
                    customer.SecondMobileNumber = vm.Customer.SecondMobileNumber;
                    customer.Email = vm.Customer.Email;
                    customer.IsPaid = vm.Customer.IsPaid;

                    if (image != null)
                    {
                        using var ms = new MemoryStream();
                        await image.CopyToAsync(ms);
                        customer.ImageUrl = ms.ToArray();
                    }

                    // 🔄 Update services
                    _context.CompanyServices.RemoveRange(customer.CompanyServices);

                    var selectedServices = vm.Services
                        .Where(x => x.IsSelected)
                        .Select(x => new CompanyService
                        {
                            CompanyId = customer.Id,
                            ServiceId = x.ServiceId
                        });

                    _context.CompanyServices.AddRange(selectedServices);

                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                }
             }

            return PartialView("Partial/_CreateEditPartial", vm);
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
