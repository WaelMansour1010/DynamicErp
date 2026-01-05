using AutoMapper;
using DevExpress.XtraReports.Security;
using EazyCash;
using EazyCash.Auth;
using EazyCash.Data;
using EazyCash.Models;
using HRServices.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------
// 1. ≈⁄œ«œ«  «·≈⁄œ«œ«  Ê«·Ã·”… (Configuration & Session)
// ---------------------------------------------------------
CurrentSession.Configuration = builder.Configuration;

// ---------------------------------------------------------
// 2.  ”ÃÌ· «·Œœ„«  (Services Registration)
// ---------------------------------------------------------

// ≈÷«›… AutoMapper
builder.Services.AddAutoMapper(typeof(Client).Assembly);
builder.Services.AddAutoMapper(typeof(AutoMapping).Assembly);

// Õﬁ‰ «· »⁄Ì… ·„œÌ— ﬁÊ«⁄œ «·»Ì«‰« 
builder.Services.AddScoped<dbManager>();

// ≈÷«›… MVC „⁄ Œ«’Ì… «·‹ Runtime Compilation („›Ìœ… ·· ⁄œÌ· «·”—Ì⁄ ⁄·Ï «·”Ì—›—)
builder.Services.AddControllersWithViews()
                .AddRazorRuntimeCompilation();

// ---------------------------------------------------------
// 3. ≈⁄œ«œ ﬁ«⁄œ… «·»Ì«‰«  (Connection String Management)
// ---------------------------------------------------------
var sqlConnectionString = CurrentSession.ConnectionString;

// ›Õ’ √„«‰ ·„‰⁄ NullReferenceException
if (string.IsNullOrEmpty(sqlConnectionString))
{
    // ›Ì Õ«· ›‘· «·ﬁ—«¡…° Ì „ «· Êﬁ› Ê≈ŸÂ«— —”«·… Ê«÷Õ…
    throw new InvalidOperationException("Œÿ√: ·„ Ì „ «·⁄ÀÊ— ⁄·Ï ‰’ «·« ’«· 'myconnection' ›Ì „·› appsettings.json");
}

// ≈÷«›… «·Œ’«∆’ ·÷„«‰ «· Ê«›ﬁ „⁄ SQL Server «·ÕœÌÀ Ê«·”Ì—›—«  «· Ì  ” Œœ„ SSL
if (!sqlConnectionString.Contains("TrustServerCertificate", StringComparison.OrdinalIgnoreCase))
{
    sqlConnectionString += ";TrustServerCertificate=true;";
}
if (!sqlConnectionString.Contains("Encrypt", StringComparison.OrdinalIgnoreCase))
{
    sqlConnectionString += "Encrypt=false;";
}

// —»ÿ «·‹ DbContexts »«·„ €Ì— «·„⁄œ·
builder.Services.AddDbContext<HROnlineModel.HROnlineModel>(options =>
    options.UseSqlServer(sqlConnectionString));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(sqlConnectionString));

// ---------------------------------------------------------
// 4. ≈⁄œ«œ«  «·ÂÊÌ… Ê«·ﬂÊﬂÌ“ (Identity & Auth)
// ---------------------------------------------------------
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 1;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredUniqueChars = 0;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.Cookie.Name = "HROnline@Cookie";
        o.LoginPath = "/Account/Login"; //  √ﬂœ „‰ „”«—  ”ÃÌ· «·œŒÊ· ·œÌﬂ
    });

// ---------------------------------------------------------
// 5. ≈⁄œ«œ HttpClient (Õ· „‘ﬂ·… «·‹ SSL Ê ÊÕÌœ «· ⁄—Ì›)
// ---------------------------------------------------------
builder.Services.AddHttpClient("Default")
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        // Â–« «·”ÿ— Ì”„Õ »«·« ’«· Õ Ï ·Ê ﬂ«‰  ‘Â«œ… «·‹ SSL €Ì— ’«·Õ… √Ê –« Ì… «· ÊﬁÌ⁄
        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
    });

// ---------------------------------------------------------
// 6. »‰«¡ «· ÿ»Ìﬁ Ê≈⁄œ«œ Œÿ «·≈‰ «Ã (Application Pipeline)
// ---------------------------------------------------------
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// ≈⁄œ«œ«  ’·«ÕÌ«  «· ﬁ«—Ì— ·‹ DevExpress
ScriptPermissionManager.GlobalInstance = new ScriptPermissionManager(ExecutionMode.Unrestricted);

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// «· — Ì» Â‰« „Â„ Ãœ«: «·‹ Authentication œ«∆„« ﬁ»· «·‹ Authorization
app.UseAuthentication();
app.UseAuthorization();

// ≈⁄œ«œ «· ÊÃÌÂ «·«› —«÷Ì
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// ---------------------------------------------------------
// 7.  ⁄—Ì› «·‹ AutoMapper Profiles
// ---------------------------------------------------------
public class AutoMapping : Profile
{
    public AutoMapping()
    {
        CreateMap<projectModel, HROnlineModel.project>().ReverseMap();
        CreateMap<opr_employee_detailModel, HROnlineModel.opr_employee_detail>().ReverseMap();
    }
}