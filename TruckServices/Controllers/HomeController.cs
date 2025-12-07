using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
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
        private readonly IHttpClientFactory _httpClientFactory;
        private static readonly MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
        private readonly GeoapifyService _geoapifyService;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, EmailSender.EmailSender emailSender, IHttpClientFactory httpClientFactory, GeoapifyService geoapifyService)
        {
            _context = context;
            _logger = logger;
            _emailSender = emailSender;
            _httpClientFactory = httpClientFactory;
            _geoapifyService = geoapifyService;
        }

        public async Task<IActionResult> Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> SearchCities(string term)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
                return Json(new List<object>());

            if (_cache.TryGetValue(term.ToLower(), out List<object> cached))
                return Json(cached);

            string apiUrl = $"https://geocoding-api.open-meteo.com/v1/search?name={term}&count=25&language=en&format=json";

            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.AcceptEncoding.Add(
                    new System.Net.Http.Headers.StringWithQualityHeaderValue("gzip"));

                var response = await httpClient.GetAsync(apiUrl);
                if (!response.IsSuccessStatusCode)
                    return Json(new List<object>());

                Stream contentStream = await response.Content.ReadAsStreamAsync();

                // 🔧 Decompress manually if it's gzip
                if (response.Content.Headers.ContentEncoding.Contains("gzip"))
                    contentStream = new System.IO.Compression.GZipStream(contentStream, System.IO.Compression.CompressionMode.Decompress);

                using var reader = new StreamReader(contentStream);
                string jsonString = await reader.ReadToEndAsync();

                var json = System.Text.Json.JsonDocument.Parse(jsonString).RootElement;
                if (!json.TryGetProperty("results", out JsonElement results))
                    return Json(new List<object>());

                var allResults = results.EnumerateArray()
                    .Where(x =>
                        x.TryGetProperty("country_code", out var codeEl) &&
                        (codeEl.GetString() == "US" || codeEl.GetString() == "CA"))
                    .Select(x => new
                    {
                        City = x.GetProperty("name").GetString(),
                        State = x.TryGetProperty("admin1", out var stateEl) ? stateEl.GetString() : "",
                        Country = x.TryGetProperty("country", out var countryEl) ? countryEl.GetString() : "",
                        CountryCode = x.GetProperty("country_code").GetString()
                    })
                    .Distinct()
                    .OrderBy(x => x.City)
                    .ToList();

                _cache.Set(term.ToLower(), allResults, TimeSpan.FromHours(6));
                return Json(allResults);
            }
            catch
            {
                return Json(new List<object>());
            }

        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ServicesResults(string location, string service, int page = 1, int radius = 50)
        {
            try
            {
                int pageSize = 10;

                // -----------------------------
                // Parse location (city, state, country)
                // -----------------------------
                string city = null, state = null, country = null;

                if (!string.IsNullOrWhiteSpace(location))
                {
                    var parts = location.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                        .Select(p => p.Trim())
                                        .ToArray();

                    if (parts.Length > 0) city = parts[0];
                    if (parts.Length > 1) state = parts[1];
                    if (parts.Length > 2) country = parts[2];
                }

                var query = _context.CustomersData.AsQueryable();

                if (!string.IsNullOrEmpty(city))
                    query = query.Where(x => x.City != null && x.City.ToLower() == city.ToLower());

                if (!string.IsNullOrEmpty(state))
                    query = query.Where(x => x.State != null && x.State.ToLower() == state.ToLower());

                if (!string.IsNullOrEmpty(country))
                    query = query.Where(x => x.Country != null && x.Country.ToLower() == country.ToLower());

                int totalCount = await query.CountAsync();

                // ---------------------------------------------------------------
                // If no direct match → try nearest city with Geoapify service
                // ---------------------------------------------------------------
                if (totalCount == 0 && !string.IsNullOrEmpty(location))
                {
                    try
                    {
                        var nearest = await _geoapifyService.GetNearestDifferentCityAsync(location, radius);

                        if (nearest != null)
                        {
                            string nearestCity = nearest.Value.city;
                            string nearestState = nearest.Value.state;
                            string nearestCountry = nearest.Value.country;

                            if (!string.IsNullOrEmpty(country) &&
                                nearestCountry?.Equals(country, StringComparison.OrdinalIgnoreCase) == true)
                            {
                                query = _context.CustomersData.Where(
                                    x => x.City != null && x.City.ToLower() == nearestCity.ToLower() &&
                                         x.Country != null && x.Country.ToLower() == nearestCountry.ToLower());

                                if (!string.IsNullOrEmpty(state) && !string.IsNullOrEmpty(nearestState))
                                {
                                    query = query.Where(x =>
                                        x.State != null &&
                                        x.State.ToLower() == nearestState.ToLower());
                                }

                                totalCount = await query.CountAsync();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log only, do NOT show technical details to user
                        _logger.LogError(ex, "Geoapify API failed.");
                    }
                }

                // -----------------------------
                // Fetch providers
                // -----------------------------
                var list = await query
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
                    ImageBase64 = p.ImageUrl != null
                        ? $"data:{GetImageMimeType(p.ImageUrl)};base64,{Convert.ToBase64String(p.ImageUrl)}"
                        : "https://www.gynprog.com.br/wp-content/uploads/2017/06/wood-blog-placeholder.jpg",
                    Status = "Open"
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
                    ErrorMessage = totalCount == 0 ? "No results found for your search." : ""
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
