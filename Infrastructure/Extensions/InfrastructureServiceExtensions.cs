// ============================================
// Infrastructure/Extensions/InfrastructureServiceExtensions.cs
// ============================================
using AS.NotificationService.Application.Service.Interface;
using AS.NotificationService.Infrastructure.Email;
using AS.NotificationService.Infrastructure.Email.Settings;
using AS.NotificationService.Infrastructure.Messaging.Consumers;
using AS.NotificationService.Infrastructure.Messaging.Handlers;
using AS.NotificationService.Infrastructure.Messaging.Settings;
using AS.NotificationService.Infrastructure.WhatsApp;
using AS.NotificationService.Persistence.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Postino.EmailService.Persistence;

namespace AS.NotificationService.Infrastructure.Extensions
{


    public static class InfrastructureServiceExtensions
    {
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services)
        {


            // Persistence
            services.AddScoped<IEmailLogRepository, EmailLogRepository>();

            // Email Service
            services.AddHttpClient();

            services.AddScoped<IEmailSender, EmailSender>();

            // WhatsApp Service
            services.AddScoped<IWhatsAppSender, WhatsAppSender>();

            // RabbitMQ Consumer (BackgroundService)
            services.AddHostedService<NotificationQueueConsumer>();

            services.AddScoped<NotificationEventHandler>();

            return services;
        }
    }
}