using InventoryService.BackgroundServices;
using InventoryService.Context;
using InventoryService.MessageBroker;
using Microsoft.EntityFrameworkCore;

namespace InventoryService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddDbContext<ServiceContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("local-server")));

            builder.Services.AddScoped<IMessageBrokerClient, RabbitMQClient>();

            builder.Services.AddSingleton<PublishMessageToQueue>();
            builder.Services.AddHostedService<PublishMessageToQueue>(
                provider => provider.GetRequiredService<PublishMessageToQueue>());

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ServiceContext>();
                dbContext.Database.Migrate();
               
                // use context
            }

            // Configure the HTTP request pipeline.

            app.UseSwagger();
            app.UseSwaggerUI();


            app.UseAuthorization();

            
            app.MapControllers();

            app.Run();
        }
    }
}