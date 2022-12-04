using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StellaBooking.Models;
using StellaBooking.Services;
using System.Data;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<StellaBookingContext>(opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHttpClient<WashingMachineClient>(client =>
{
    client.BaseAddress = new Uri("https://localhost:7061");
    //client.BaseAddress = new Uri("http://localhost:5049");
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateActor = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ClockSkew = TimeSpan.Zero,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };

    options.SaveToken = true;

});

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

const string USER_ROLE = "user";
const string ADMIN_ROLE = "admin";

app.MapGet("/booking/{bookingId}", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = USER_ROLE)] async (string bookingId, StellaBookingContext db) =>
{
    var booking = db.Bookings.FirstOrDefault(booking => booking.Id.ToString() == bookingId);

    if (booking == null)
    {
        return Results.NotFound("Payment not found");
    }

    return Results.Ok(booking);
});

app.MapPost("/booking/{washingMachineId}", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = USER_ROLE)] async (string washingMachineId, BookingDto bookingDto, StellaBookingContext db, WashingMachineClient washingMachineClient, HttpContext http) =>
{
    var washingMachine = await washingMachineClient.GetWashingMachine(washingMachineId);
    var duration = bookingDto.DurationInMinutes;
    var appartmentName = bookingDto.AppartmentName;

    if(washingMachine == null)
    {
        return Results.NotFound("Washingmachine not found");
    }

    var userId = http.User.Claims.First(claim => claim.Type == ClaimTypes.NameIdentifier).Value;
    var userEmail = http.User.Claims.First(claim => claim.Type == ClaimTypes.Email).Value;

    if(userId == null) 
    {
        return Results.BadRequest("Bad Token!");
    }

    var booking = new Booking(duration);

    booking.Id = Guid.NewGuid();
    booking.UserEmail = userEmail;
    booking.ApartmentComplexName = appartmentName;

    return Results.Created($"/booking/{booking.Id}", "Thank you for booking a washing machine with 'StellaTime'");
});

app.Run();
