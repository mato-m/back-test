public async Task InvokeAsync(HttpContext context, IServiceProvider serviceProvider)
{
    string ipAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
    if (string.IsNullOrEmpty(ipAddress))
    {
        ipAddress = context.Connection.RemoteIpAddress?.ToString();
    }

    if (string.IsNullOrEmpty(ipAddress))
    {
        context.Response.StatusCode = 400; // Bad Request
        await context.Response.WriteAsync("IP address not found.");
        return;
    }

    using (var scope = serviceProvider.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<YourDbContext>();

        // Log the request in the database first
        var record = new IpAddressRecord
        {
            IpAddress = ipAddress,
            VisitTime = DateTime.UtcNow
        };

        dbContext.IpAddressRecords.Add(record);
        await dbContext.SaveChangesAsync();

        // Then, check if the IP address is whitelisted
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
    }

    await _next(context);
}