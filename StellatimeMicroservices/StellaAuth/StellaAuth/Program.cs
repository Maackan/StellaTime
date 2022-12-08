using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StellaAuth.Models;
using StellaAuth.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<StellaAuthContext>(opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("AZURE")));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<StellaAuthContext>(); 
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

const string USER_ROLE = "user";
const string ADMIN_ROLE = "admin";

app.MapPost("/register", async (RegisterDto newUser, StellaAuthContext db) =>
{
    
    var userAlreadyExists = await db.Users.AnyAsync(user => user.Email.ToLower().Equals(newUser.Email.ToLower()));

    if (userAlreadyExists)
    {
        return Results.BadRequest($"{newUser.Email} is already registered");
    }

    if (string.IsNullOrEmpty(newUser.Role))
    {
        newUser.Role = USER_ROLE;
    }
        
    if(newUser.Role.ToLower() != USER_ROLE && newUser.Role.ToLower() != ADMIN_ROLE)
    {
        return Results.BadRequest($"{newUser.Role} is not a valid Role");
    }

    if(newUser.Phone == null)
    {
        newUser.Phone = "";
    }

    User userToBeRegistered = new User()
    {
        Name = newUser.Name,
        Password = newUser.Password.ToLower(),
        Email = newUser.Email,
        Phone = newUser.Phone,
        Role = newUser.Role.ToLower(),
    };

    await db.Users.AddAsync(userToBeRegistered);
    await db.SaveChangesAsync();

    return Results.Created("/login", "New user is registered");
});

app.MapPost("/login", async (UserLoginDto loginAttempt, StellaAuthContext db) =>
{
    User? user = await db.Users.FirstOrDefaultAsync(user => user.Email.ToLower().Equals(loginAttempt.Email.ToLower()) && user.Password.Equals(loginAttempt.Password));

    if(user == null) 
    { 
        return Results.BadRequest("Email or Password was incorrect.");
    }

    string secretKey = builder.Configuration["Jwt:Key"];

    if (secretKey == null)
    {
        return Results.StatusCode(500);
    }

    Claim[] claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.GivenName, user.Name),
        new Claim(ClaimTypes.Surname, user.Name),
        new Claim(ClaimTypes.Role, user.Role),
    };

    var token = new JwtSecurityToken(
        issuer: builder.Configuration["Jwt:Issuer"],
        audience: builder.Configuration["Jwt:Audience"],
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(20),
        notBefore: DateTime.UtcNow,
        signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        SecurityAlgorithms.HmacSha256)
    );

    var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

    return Results.Ok(tokenString);
});

app.Run();
