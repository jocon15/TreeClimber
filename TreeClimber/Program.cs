using Blazored.Toast;
using TreeClimber.Components;
using TreeClimberCore.Services.JSON;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddMvc();  // need this for cache busting in App.razor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddScoped<JSONFileDataService>();
builder.Services.AddBlazorContextMenu();
builder.Services.AddBlazoredToast();

var app = builder.Build();

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
