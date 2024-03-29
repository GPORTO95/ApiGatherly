using Gatherly.App.Configuration;
using Gatherly.App.DependencyInjection;
using Gatherly.App.Middlewares;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

/// METODO 1
/// � necess�rio chamar todos os metodos a cada implementa��o
/// na classe static DependecyInjection
#region :: M�TODO 1 ::
//builder.Services
//    .AddCaching(builder.Configuration)
//    .AddInfrastructure(builder.Configuration)
//    .AddApplication()
//    .AddBackgroundJobs()
//    .AddPresentation()
//    .AddAuthenticationAndAuthorization();
#endregion

/// M�TODO 2
/// Uma vez configurado, basta criar suas devidas classes 
//builder.Services
//    .InstallServices(
//        builder.Configuration,
//        typeof(IServiceInstaller).Assembly);

/// M�TODO 3
builder.Services.AddApplication();

builder.Services.AddInfrastructure();

builder.Services.AddPersistence(builder.Configuration);

builder.Services.AddBackgroundJobs();

builder.Services.AddPresentation();

builder.Services.AddAuthenticationAndAuthorization();


WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

app.MapControllers();

app.Run();
