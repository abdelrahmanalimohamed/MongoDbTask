using Microsoft.Extensions.Options;
using MongoDbTask.API.DBContext;
using MongoDbTask.API.Options;
using MongoDbTask.API.Services;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();

builder.Services.AddSwaggerGen();

builder.Services.Configure<MongoDbOptions>(
    builder.Configuration.GetSection(MongoDbOptions.SectionName));

builder.Services.Configure<APIOption>(builder.Configuration.GetSection("APIOption"));

builder.Services.AddSingleton<MongoDbContext>();

builder.Services.AddHttpClient<DummyJsonClient>((serviceProvider, client) =>
{
    var options = serviceProvider
        .GetRequiredService<IOptions<APIOption>>()
        .Value;

    client.BaseAddress = new Uri(options.URL);
});

builder.Services.AddScoped<UserImportService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
