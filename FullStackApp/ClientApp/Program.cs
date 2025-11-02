using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ClientApp;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient with backend API base address
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri("http://localhost:<CLIENT_APP_PORT>/"),  // Replace <CLIENT_APP_PORT> with the actual port numbers
    Timeout = TimeSpan.FromSeconds(30)
});

await builder.Build().RunAsync();
