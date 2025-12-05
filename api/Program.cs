using MyApp.Namespace.DataAccess;
using MyApp.Namespace.ModelUtility;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Initialize DBUtility
DBUtility.Initialize(builder.Configuration);

// Register Database service
builder.Services.AddScoped<Database>(provider => 
    new Database(connectionString ?? throw new InvalidOperationException("Connection string not found")));

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();


// Add CORS
builder.Services.AddCors(options => 
    { options.AddPolicy("OpenPolicy", builder => 
        { builder.AllowAnyOrigin() .AllowAnyMethod() .AllowAnyHeader(); 
        }); 
        });


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.MapControllers();

app.Run();