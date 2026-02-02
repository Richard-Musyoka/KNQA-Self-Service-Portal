using KNQASelfService.Context;
using KNQASelfService.Data;
using KNQASelfService.Interfaces;
using KNQASelfService.Interfaces.UserManagement;
using KNQASelfService.Repositories;
using KNQASelfService.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using MudBlazor;
using MudBlazor.Extensions;
using MudBlazor.Services;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddHttpContextAccessor();
// Register HttpClient
builder.Services.AddHttpClient();
// Register Leave Plan Service
builder.Services.AddScoped<ILeavePlanService, LeavePlanService>();
// Add services
builder.Services.AddScoped<ITransportService, TransportService>();
builder.Services.AddScoped<IIncidentManagementService, IncidentManagementService>();
builder.Services.AddScoped<IHelpDeskService, HelpDeskService>();
// Add to Program.cs or equivalent
builder.Services.AddScoped<IJobRequisitionService, JobRequisitionService>();
builder.Services.AddScoped<IRoomBookingService, RoomBookingService>();
builder.Services.AddScoped<IVehicleMaintenanceService, VehicleMaintenanceService>();
builder.Services.AddScoped<ITrainingRequestService, TrainingRequestService>();
builder.Services.AddScoped<ITrainingEvaluationService, TrainingEvaluationService>();

builder.Services.AddHttpClient("KNQASelfServiceClient", client =>
{
    client.BaseAddress = new Uri("https://localhost:7054/");
});
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("KNQASelfServiceClient"));
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
// Authentication Service
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/login";
    });

builder.Services.AddAuthorization();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddMudServices();

//builder.Services.AddMudExtensions();

builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
    config.SnackbarConfiguration.PreventDuplicates = true;
    config.SnackbarConfiguration.NewestOnTop = true;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 5000;
    config.SnackbarConfiguration.HideTransitionDuration = 500;
    config.SnackbarConfiguration.ShowTransitionDuration = 500;
    config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
});


builder.Services.AddSingleton<WeatherForecastService>();


builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserAuthService, UserAuthService>();
builder.Services.AddAuthorizationCore();

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IOtpService, OtpService>();

builder.Services.AddSingleton<IBusinessCentralService, BusinessCentralService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = new FileExtensionContentTypeProvider
    {
        Mappings =
        {
            [".avif"] = "image/avif",
            [".jpg"] = "image/jpg",
            [".jpeg"] = "image/jpeg"
        }
    }
});
app.UseHttpsRedirection();

app.UseStaticFiles();

//Add auth middleware
app.UseAuthentication();
app.UseAuthorization();

app.UseRouting();

app.MapControllers();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
