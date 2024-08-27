using Microsoft.EntityFrameworkCore;
using System;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

public class DatabaseTimeMiddleware
{
    private readonly RequestDelegate _next;

    public DatabaseTimeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IServiceProvider serviceProvider)
    {
        string ipAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (string.IsNullOrEmpty(ipAddress))
        {
            ipAddress = context.Connection.RemoteIpAddress.ToString();
        }

        using (var scope = serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<YourDbContext>();

            bool isWhitelisted = await dbContext.WhitelistedIPs.AnyAsync(ip => ip.Address == ipAddress);
            if (!isWhitelisted)
            {
                context.Response.StatusCode = 403; // Forbidden
                await context.Response.WriteAsync("Access denied. Your IP address is not whitelisted.");
                return;
            }

            // Proceed with the original logic if IP is whitelisted
            var connection = dbContext.Database.GetDbConnection();
            await connection.OpenAsync();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT SYSDATETIME()";
                var currentTime = await command.ExecuteScalarAsync();

                Console.WriteLine($"Database Time: {currentTime}, Request IP Address: {ipAddress}");

                var record = new IpAddressRecord
                {
                    IpAddress = ipAddress,
                    VisitTime = DateTime.UtcNow
                };

                dbContext.IpAddressRecords.Add(record);
                await dbContext.SaveChangesAsync();
            }
        }

        await _next(context);
    }
}