using Cms0053ClaimAttachmentDemo.Data;
using Cms0053ClaimAttachmentDemo.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
var dbPath = Path.Combine(builder.Environment.ContentRootPath, "cms0053.db");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));
builder.Services.AddScoped<FileStorageService>();
builder.Services.AddScoped<ClaimMatchingService>();
builder.Services.AddScoped<AttachmentProcessingService>();
builder.Services.AddScoped<MockClearinghouseService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    SeedData.Initialize(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}
app.UseRouting();
app.UseAuthorization();
app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

app.Run();
