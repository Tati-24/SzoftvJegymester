using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi;



var builder = WebApplication.CreateBuilder(args);
var jwt = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwt["Key"]!);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key),
            NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier,
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<TokenService>();


builder.Services.AddCors(options =>
        {
            options.AddPolicy("CorsPolicy", policy =>
            {
                    policy
                        .WithOrigins("http://localhost:5173")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
            });
        });


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.TagActionsBy(api => new[] { GetSwaggerTag(api) });
    options.OrderActionsBy(api => GetSwaggerOrderKey(api));
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste only the JWT token. Do not include the word Bearer."
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", document, null),
            new List<string>()
        }
    });
});
builder.Services.AddControllers();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.ConfigObject.PersistAuthorization = true;
    });
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseCors("CorsPolicy");
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

static string GetSwaggerTag(ApiDescription api)
{
    var controller = api.ActionDescriptor.RouteValues["controller"];

    return controller switch
    {
        "Auth" => "01 - Auth",
        "Films" => "02 - Films",
        "MovieHalls" => "03 - Movie Halls",
        "Screenings" => "04 - Screenings",
        "Tickets" => "05 - Tickets",
        _ => $"99 - {controller}"
    };
}

static string GetSwaggerOrderKey(ApiDescription api)
{
    var method = api.HttpMethod ?? string.Empty;
    var path = api.RelativePath ?? string.Empty;

    var actionOrder = path switch
    {
        "register" => "01",
        "login" => "02",
        "films/admin/upload" => "03",
        "films" when method == "GET" => "04",
        "films/{id}" => "05",
        "films/admin/{id}" when method == "PUT" => "06",
        "films/admin/{id}" when method == "DELETE" => "07",
        "movie-halls/admin/upload" => "08",
        "movie-halls" when method == "GET" => "09",
        "movie-halls/{id}" => "10",
        "movie-halls/admin/{id}" when method == "PUT" => "11",
        "movie-halls/admin/{id}" when method == "DELETE" => "12",
        "screenings/admin/upload" => "13",
        "screenings" when method == "GET" => "14",
        "screenings/{id}" => "15",
        "screenings/admin/{id}" when method == "PUT" => "16",
        "screenings/admin/{id}" when method == "DELETE" => "17",
        "tickets/purchase" => "18",
        "tickets/my-tickets" => "19",
        "tickets/{id}" when method == "GET" => "20",
        "tickets/{id}" when method == "DELETE" => "21",
        "tickets/cashier/purchase" => "22",
        "tickets/{id}/validate" => "23",
        "tickets" when method == "GET" => "24",
        "tickets/stats" => "25",
        _ => "99"
    };

    return $"{actionOrder}_{path}_{method}";
}
