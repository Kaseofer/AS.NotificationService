// ============================================
// Infrastructure/Extensions/InfrastructureServiceExtensions.cs
// ============================================
using AS.NotificationService.Application.Service.Interface;
using AS.NotificationService.Domain.Repositories;
using AS.NotificationService.Infrastructure.Email;
using AS.NotificationService.Infrastructure.Email.Settings;
using AS.NotificationService.Infrastructure.Messaging.Consumers;
using AS.NotificationService.Infrastructure.Messaging.Handlers;
using AS.NotificationService.Infrastructure.Persistence.MongoDB.Settings;
using AS.NotificationService.Infrastructure.WhatsApp;
using AS.NotificationService.Infrastructure.WhatsApp.Settings;
using Infrastructure.Persistence.MongoDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AS.NotificationService.Infrastructure.Extensions
{


    public static class InfrastructureServiceExtensions
    {
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
             IConfiguration configuration)
        {

            // ============================================
            // SETTINGS
            // ============================================
            services.Configure<MongoDbSettings>(
                configuration.GetSection("MongoDbSettings"));

            services.Configure<EmailSettings>(
                configuration.GetSection("EmailSettings"));

            services.Configure<WhatsAppSettings>(
                configuration.GetSection("WhatsAppSettings"));

            // ============================================
            // HTTP CLIENT para EmailSender
            // ============================================
            services.AddHttpClient<IEmailSender, EmailSender>();

            // ============================================
            // REPOSITORIES (Solo MongoDB)
            // ============================================
            services.AddScoped<INotificationLogRepository, NotificationLogRepository>();

            // ============================================
            // SERVICES
            // ============================================
            services.AddScoped<IWhatsAppSender, WhatsAppSender>();

            return services;
        }
    }
}