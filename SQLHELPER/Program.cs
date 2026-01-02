using SQLHELPER.Components;
using SQLHELPER.Infrastructure.Data;
using SQLHELPER.Infrastructure.Data.Repos;
using SQLHELPER.Infrastructure.Settings;
using SQLHELPER.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection("Database"));
builder.Services.AddSingleton<DbConnectionFactory>();

builder.Services.AddSingleton<SettingsStore>();
builder.Services.AddSingleton<AppSettingsLocal>(sp => sp.GetRequiredService<SettingsStore>().LoadAsync().GetAwaiter().GetResult());

builder.Services.AddScoped<ServerRepository>();
builder.Services.AddScoped<DbTargetRepository>();
builder.Services.AddScoped<RunRepository>();
builder.Services.AddScoped<RunStepRepository>();
builder.Services.AddScoped<NoteRepository>();
builder.Services.AddScoped<QueryFolderRepository>();
builder.Services.AddScoped<SavedQueryRepository>();
builder.Services.AddScoped<QueryRunRepository>();

builder.Services.AddScoped<BootstrapService>();
builder.Services.AddScoped<ServerService>();
builder.Services.AddScoped<QueryService>();
builder.Services.AddScoped<MaintenanceService>();
builder.Services.AddScoped<HealthService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var bootstrap = scope.ServiceProvider.GetRequiredService<BootstrapService>();
    bootstrap.InitializeAsync().GetAwaiter().GetResult();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
