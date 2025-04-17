var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

//Register an instance of type IHttpClientFactory
//When you dispose of a HttpClient instance, the connection remains open for up to four minutes.
//Further, the number of sockets that you can open at any point in time has a limit —
//you can’t have too many sockets open at once. So when you use too many HttpClient instances,
//you might end up exhausting your supply of sockets.
//Here’s where IHttpClientFactory comes to the rescue.
//You can take advantage of IHttpClientFactory to create HttpClient instances for invoking
//HTTP API methods by adhering to the best practices to avoid issues faced with HttpClient.
//The primary goal of IHttpClientFactory in ASP.NET Core is to ensure that HttpClient instances
//are created using the factory while at the same time eliminating socket exhaustion.
builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
