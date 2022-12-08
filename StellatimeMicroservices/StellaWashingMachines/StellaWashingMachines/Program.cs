using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StellaWashingMachines.Models;
using StellaWashingMachines.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<StellaWashingMachinesContext>(opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("AZURE")));

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

});

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<StellaWashingMachinesContext>();
    db.Database.Migrate();
}

app.UseAuthentication();
app.UseAuthorization();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

const string USER_ROLE = "user";
const string ADMIN_ROLE = "admin";

app.MapGet("/machine/{id}", async (string id, StellaWashingMachinesContext db) =>
{
    var washingMachine = await db.WashingMachines.FirstOrDefaultAsync(machine => machine.Id.ToString() == id);
    
    if(washingMachine == null)
    {
        return Results.NotFound("No Washing Machine was found");
    }

    return Results.Ok(washingMachine);
});

app.MapGet("/machines", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{USER_ROLE}, {ADMIN_ROLE}")] async (StellaWashingMachinesContext db) =>
{
    return await db.WashingMachines.ToListAsync();
});

app.MapPost("/machine", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = ADMIN_ROLE)] async (StellaWashingMachinesContext db) => 
{

    var newWashingMachine = new WashingMachine() { Available = true };

    await db.WashingMachines.AddAsync(newWashingMachine);
    await db.SaveChangesAsync();

    return Results.Created($"/machine/{newWashingMachine.Id}", "New washingmachine added");
});

app.MapDelete("/machine/{id}", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = ADMIN_ROLE)] async (string id, StellaWashingMachinesContext db) =>
{
    var machineToDelete = await db.WashingMachines.FirstOrDefaultAsync(machine => machine.Id.ToString() == id);

    if (machineToDelete == null)
    {
        return Results.NotFound();
    }

    db.WashingMachines.Remove(machineToDelete);

    await db.SaveChangesAsync();

    return Results.Ok($"WashingMachine was removed successfully from the application!");
});

app.MapPut("/machine/{id}", async (string id, StellaWashingMachinesContext db) =>
{
    var machineToAlter = await db.WashingMachines.FirstOrDefaultAsync(machine => machine.Id.ToString() == id);

    if (machineToAlter == null)
    {
        return Results.NotFound("Could not find washing machine");
    }

    machineToAlter.Available = false;

    await db.SaveChangesAsync();

    return Results.Ok("Washingmachine had been updated");
});

app.Run();
