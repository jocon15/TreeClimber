using Blazored.Toast;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TreeClimber;
using TreeClimberCore.Services.JSON;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddScoped<JSONFileDataService>();
builder.Services.AddBlazorContextMenu();
builder.Services.AddBlazoredToast();
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
