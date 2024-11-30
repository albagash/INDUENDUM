using INDUENDUM_API.Data;
using INDUENDUM_API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models; // Swagger p�r konfigurimin
using System.Data.SqlClient; // P�r p�rdorimin e procedurave t� ruajtura
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Shto sh�rbimet n� konteiner
builder.Services.AddControllers();

// Konfiguro DbContext p�r SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// Regjistro SqlConnection p�r p�rdorim n� Controllers
builder.Services.AddScoped<SqlConnection>(_ =>
    new SqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")));

// Konfiguro identitetin
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true; // K�rko numra n� fjal�kalim
    options.Password.RequiredLength = 6; // Minimumi 6 karaktere
    options.Password.RequireNonAlphanumeric = false; // Jo karaktere speciale
    options.Password.RequireUppercase = true; // Nj� shkronj� e madhe
    options.Password.RequireLowercase = true; // Nj� shkronj� e vog�l
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders(); // P�r token p�r resetimin e fjal�kalimit dhe email konfirmimet

// Regjistro UserManager dhe RoleManager
builder.Services.AddScoped<UserManager<ApplicationUser>>();
builder.Services.AddScoped<RoleManager<IdentityRole>>();

// Konfiguro JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "Bearer";
    options.DefaultChallengeScheme = "Bearer";
})
.AddJwtBearer("Bearer", options =>
{
    var jwtKey = builder.Configuration["Jwt:Key"];
    if (string.IsNullOrEmpty(jwtKey))
    {
        throw new InvalidOperationException("JWT Key is not set in the configuration.");
    }

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
    };
});

// Shto autorizimin
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin")); // Politika q� lejon vet�m rolin Admin
    options.AddPolicy("UserOnly", policy =>
        policy.RequireRole("User")); // Politika q� lejon vet�m rolin User
});

// Konfiguro Swagger p�r dokumentimin e API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "INDUENDUM_API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' followed by a space and your JWT token below."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Konfiguro CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Inicializo rolet baze
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var roles = new[] { "Admin", "User", "Company" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}

// Konfiguro middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll"); // Aktivizo CORS
app.UseHttpsRedirection(); // Aktivizo HTTPS
app.UseAuthentication(); // Aktivizo JWT Authentication
app.UseAuthorization(); // Aktivizo autorizimin

app.MapControllers(); // Mapo kontrollet

app.Run(); // Fillo aplikacionin
