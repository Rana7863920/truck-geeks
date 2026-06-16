using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using TruckServices.Data;
using TruckServices.Models;
using TruckServices.Services;
using static System.Net.WebRequestMethods;

namespace TruckServices.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly EmailSender.EmailSender _emailSender;
        private static readonly MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
        private readonly GoogleMapsService _googleMapsService;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, EmailSender.EmailSender emailSender, GoogleMapsService googleMapsService)
        {
            _context = context;
            _logger = logger;
            _emailSender = emailSender;
            _googleMapsService = googleMapsService;
        }

        public async Task<IActionResult> Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> SearchCities(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return Json(new List<object>());

            string cacheKey = term.ToLower();
            if (_cache.TryGetValue(cacheKey, out List<object> cached))
                return Json(cached);

            var cities = await _googleMapsService.AutocompleteCitiesAsync(term);

            var results = cities.Select(c => new
            {
                City = c.City,
                State = c.State,
                Country = c.Country
            }).ToList();

            _cache.Set(cacheKey, results, TimeSpan.FromHours(6));

            return Json(results);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ServicesResults(string location, string service, int page = 1, int radius = 50)
        {
            return RedirectToAction(
                nameof(ServicesResult),
                new
                {
                    location,
                    service,
                    page,
                    radius
                });
        }


        [HttpGet]
        public async Task<IActionResult> ServicesResult(string location, string service, int page = 1, int radius = 50)
        {
            return await BuildServicesResults(location, service, page, radius);
        }



        private async Task<IActionResult> BuildServicesResults(string location, string service, int page, int radius)
        {
            try
            {
                int pageSize = 10;

                // -----------------------------
                // Parse location (city, state, country)
                // -----------------------------
                string city = null;
                IReadOnlyCollection<string> stateVariants = null;
                IReadOnlyCollection<string> countryVariants = null;

                if (!string.IsNullOrWhiteSpace(location))
                {
                    var parts = location.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                        .Select(p => p.Trim())
                                        .ToArray();

                    if (parts.Length > 0)
                        city = parts[0];

                    if (parts.Length > 1)
                        stateVariants = LocationMapper.GetStateVariants(parts[1]);

                    if (parts.Length > 2)
                        countryVariants = LocationMapper.GetCountryVariants(parts[2]);
                }


                var query = _context.CustomersData.AsQueryable();


                if (!string.IsNullOrEmpty(city))
                    query = query.Where(x => x.City != null && x.City.ToLower() == city.ToLower());

                if (stateVariants?.Count() > 0)
                    query = query.Where(x => x.State != null && stateVariants.Contains(x.State));

                if (countryVariants?.Count() > 0)
                    query = query.Where(x => x.Country != null && countryVariants.Contains(x.Country));

                //if (serviceId.HasValue)
                //    query = query.Where(x => x.CompanyServices.Any(cs => cs.ServiceId == serviceId));

                // ---------------------------------------------------------------
                // If no direct match → try nearest city with Geoapify service
                // ---------------------------------------------------------------
                IQueryable<CustomersData> finalQuery = query;

                if (!string.IsNullOrEmpty(location))
                {
                    try
                    {
                        var nearestCities =
                            await _googleMapsService.GetNearestCitiesAsync(location, radius);

                        IQueryable<CustomersData> nearestQuery =
                            _context.CustomersData.Where(x => false);

                        foreach (var (nCity, nState, nCountry) in nearestCities)
                        {
                            string tempCity = null;
                            IReadOnlyCollection<string> tempStateVariants = null;
                            IReadOnlyCollection<string> tempCountryVariants = null;

                            if (!string.IsNullOrWhiteSpace(location))
                            {
                                var parts = location.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                    .Select(p => p.Trim())
                                                    .ToArray();

                                if (parts.Length > 0)
                                    tempCity = parts[0];

                                if (parts.Length > 1)
                                    tempStateVariants = LocationMapper.GetStateVariants(parts[1]);

                                if (parts.Length > 2)
                                    tempCountryVariants = LocationMapper.GetCountryVariants(parts[2]);
                            }

                            if (string.IsNullOrEmpty(nCity))
                                continue;

                            var tempQuery = _context.CustomersData.AsQueryable();

                            tempQuery = tempQuery.Where(x =>
                                x.City != null &&
                                x.City.ToLower() == nCity.ToLower());

                            if (tempStateVariants?.Count() > 0)
                            {
                                tempQuery = tempQuery.Where(x =>
                                    x.State != null && stateVariants.Contains(x.State));
                            }

                            if (tempCountryVariants?.Count() > 0)
                            {
                                tempQuery = tempQuery.Where(x =>
                                    x.Country != null && countryVariants.Contains(x.Country));
                            }

                            nearestQuery = nearestQuery.Union(tempQuery);
                        }

                        // Combine direct city + nearest cities
                        finalQuery = finalQuery.Union(nearestQuery);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Google Maps nearest city lookup failed.");
                    }
                }

                finalQuery = finalQuery.Include(c => c.CompanyServices).ThenInclude(cs => cs.Service);

                int totalCount = await finalQuery.CountAsync();

                // -----------------------------
                // Fetch providers
                // -----------------------------
                var list = await finalQuery
                    .OrderByDescending(x => x.IsPaid)
                    .ThenBy(x => EF.Functions.Random())
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var tasks = list.Select(async p => new ServicesProviders
                {
                    Id = p.Id,
                    CompanyName = p.CompanyName,
                    StreetAddress = p.StreetAddress,
                    City = p.City,
                    State = p.State,
                    Country = p.Country,
                    MobileNumber = p.MobileNumber,
                    SecondMobileNumber = p.SecondMobileNumber,
                    Email = p.Email,
                    Source = p.Source,
                    IsPaid = p.IsPaid,
                    ImageBase64 = p.ImageUrl != null
                        ? $"data:{GetImageMimeType(p.ImageUrl)};base64,{Convert.ToBase64String(p.ImageUrl)}"
                        : "https://www.gynprog.com.br/wp-content/uploads/2017/06/wood-blog-placeholder.jpg",
                    Status = await _googleMapsService.GetBusinessStatusByNameAsync(p.CompanyName, p.City, p.State, p.Country),
                    Services = p.CompanyServices
                    .Where(cs => cs.Service.IsActive)
                    .Select(cs => cs.Service.Name)
                    .ToList(),
                }).ToList();

                var mappedList = (await Task.WhenAll(tasks)).ToList();

                return View(new ServiceResultsViewModel
                {
                    Providers = mappedList,
                    CurrentPage = page,
                    TotalCount = totalCount,
                    PageSize = pageSize,
                    Location = location,
                    Service = service,
                    Radius = radius,
                    ErrorMessage = totalCount == 0 ? "No services found in this area.\r\nTry increasing the search radius or searching a nearby city." : ""
                });
            }
            catch (Exception ex)
            {
                // Log actual exception
                _logger.LogError(ex, "ServicesResults failed.");

                // Send safe user message
                var vm = new ServiceResultsViewModel
                {
                    Providers = new List<ServicesProviders>(),
                    CurrentPage = 1,
                    TotalCount = 0,
                    PageSize = 10,
                    Radius = radius,
                    ErrorMessage = "Something went wrong. Please try again."
                };

                return View(vm);
            }
        }


        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult ContactUs()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ContactUs(ContactFormModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _emailSender.SendClientEnquiryEmailAsync(
                            "breakdownmechanics@gmail.com",
                            model.Name,
                            model.Email,
                            model.Subject,
                            model.Message,
                            model.Phone ?? "");
                        }
                        catch (Exception ex)
                        {
                            // Log the error, e.g. _logger.LogError(ex, "Failed to send email");
                        }

                    });

                    TempData["SuccessMessage"] = "Your message has been sent successfully.";
                    return RedirectToAction("ContactUs");
                }

                return View(model);
            }
            catch (Exception ex)
            {

                throw;
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetCityStateCountry(double lat, double lng)
        {
            var result = await _googleMapsService.ReverseGeocode(lat, lng);
            if (result == null) return NotFound();
            return Json(new { city = result.Value.city, state = result.Value.state, country = result.Value.country });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private static string GetImageMimeType(byte[] bytes)
        {
            if (bytes.Length >= 4)
            {
                // PNG header
                if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
                    return "image/png";
                // JPG header
                if (bytes[0] == 0xFF && bytes[1] == 0xD8)
                    return "image/jpeg";
            }
            return "image/png"; // default
        }
    }
}
