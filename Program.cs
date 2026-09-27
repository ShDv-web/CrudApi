using System.ComponentModel.DataAnnotations;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddValidation();
builder.Services.AddProblemDetails();

var app = builder.Build();

// Order matters: error handling wraps everything, then authentication, then logging closest to the endpoints.
app.UseErrorHandling();
app.UseTokenAuthentication();
app.UseRequestResponseLogging();

// Dictionary keyed by Id gives O(1) lookups/inserts/removes instead of O(n) List scans.
Dictionary<int, User> users = new()
{
    [1] = new User { Id = 1, Name = "John Doe" },
    [2] = new User { Id = 2, Name = "Jane Smith" },
    [3] = new User { Id = 3, Name = "Bob Johnson" },
};

app.MapGet("/GetAllUsers", () => Results.Ok(users.Values));
app.MapGet("/GetUserById/{id}", (int id) =>
    users.TryGetValue(id, out var user) ? Results.Ok(user) : Results.NotFound());
app.MapPost("/CreateUser", (User user) => {
    try
    {
        users.Add(user.Id, user);
        return Results.Created($"/GetUserById/{user.Id}", user);
    }
    catch (ArgumentException)
    {
        return Results.Conflict($"A user with ID {user.Id} already exists.");
    }
});
app.MapPut("/UpdateUser", (User user) => {
    if (!users.TryGetValue(user.Id, out var existingUser)) return Results.NotFound();
    existingUser.Name = user.Name;
    return Results.Ok(existingUser);
});
app.MapDelete("/DeleteUserById/{id}", (int id) =>
    users.Remove(id, out var user) ? Results.Ok(user) : Results.NotFound());

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();

class User
{
    [Range(1, int.MaxValue, ErrorMessage = "Id must be a positive number.")]
    public int Id { get; set; }

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters.")]
    public required string Name { get; set; }
}