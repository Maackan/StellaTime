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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<StellaBookingContext>(opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("AZURE")));

builder.Services.AddHttpClient<WashingMachineClient>(client =>
{
    //client.BaseAddress = new Uri("https://localhost:7061");
    //client.BaseAddress = new Uri("http://localhost:5049");

    //client.BaseAddress = new Uri("https://stellawashingmachines"); // behöver inte ange portnummer för 443 är default på https.
    client.BaseAddress = new Uri("http://stellawashingmachines"); // behöver inte ange portnummer för 80 är default på http.
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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<StellaBookingContext>();
    db.Database.Migrate();
}

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

const string USER_ROLE = "user";
const string ADMIN_ROLE = "admin";

app.MapGet("/booking/{bookingId}", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{USER_ROLE}, {ADMIN_ROLE}")] async (string bookingId, StellaBookingContext db) =>
{
    var booking = db.Bookings.FirstOrDefault(booking => booking.Id.ToString() == bookingId);

    if (booking == null)
    {
        return Results.NotFound("Payment not found");
    }

    return Results.Ok(booking);
});

app.MapPost("/booking/{washingMachineId}", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{USER_ROLE}, {ADMIN_ROLE}")] async (string washingMachineId, BookingDto bookingDto, StellaBookingContext db, WashingMachineClient washingMachineClient, HttpContext http) =>
{
    var washingMachine = await washingMachineClient.GetWashingMachineAsync(washingMachineId);
    var duration = bookingDto.DurationInMinutes;
    var appartmentName = bookingDto.AppartmentName;

    if(washingMachine == null)
    {
        return Results.NotFound("Washingmachine not found");
    }

    var userId = http.User.Claims.First(claim => claim.Type == ClaimTypes.NameIdentifier).Value;
    var userEmail = http.User.Claims.First(claim => claim.Type == ClaimTypes.Email).Value;
    var userGuidId = new Guid(userId);
    if(userId == null) 
    {
        return Results.BadRequest("Bad Token!");
    }

    var booking = new Booking();

    booking.WashingMachineId = washingMachine.Id;
    booking.UserId = userGuidId;
    booking.UserEmail = userEmail;
    booking.ApartmentComplexName = appartmentName;
    booking.Start = DateTime.UtcNow;
    booking.End = booking.Start + TimeSpan.FromMinutes(duration);

    await washingMachineClient.UpdateWashingMachineAsync(washingMachine.Id.ToString());

    await db.Bookings.AddAsync(booking);
    await db.SaveChangesAsync();

    return Results.Created($"/booking/{booking.Id}", "Thank you for booking a washing machine with 'StellaTime'");
});

app.Run();
