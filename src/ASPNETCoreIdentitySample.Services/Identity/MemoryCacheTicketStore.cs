﻿using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Memory;

namespace ASPNETCoreIdentitySample.Services.Identity;

/// <summary>
///     Adapted from https://github.com/aspnet/Security/blob/dev/samples/CookieSessionSample/MemoryCacheTicketStore.cs
///     to manage large identity cookies.
///     More info: http://www.dntips.ir/post/2581
///     And http://www.dntips.ir/post/2575
/// </summary>
public class MemoryCacheTicketStore(IMemoryCache cache) : ITicketStore
{
    private const string KeyPrefix = "AuthSessionStore-";
    private readonly IMemoryCache _cache = cache ?? throw new ArgumentNullException(nameof(cache));

    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var key = $"{KeyPrefix}{Guid.NewGuid():N}";
        await RenewAsync(key, ticket);

        return key;
    }

    public Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        if (ticket == null)
        {
            throw new ArgumentNullException(nameof(ticket));
        }

        var options = new MemoryCacheEntryOptions().SetSize(size: 1);
        var expiresUtc = ticket.Properties.ExpiresUtc;

        if (expiresUtc.HasValue)
        {
            options.SetAbsoluteExpiration(expiresUtc.Value);
        }

        if (ticket.Properties.AllowRefresh ?? false)
        {
            options.SetSlidingExpiration(TimeSpan.FromMinutes(minutes: 60)); //TODO: configurable.
        }

        _cache.Set(key, ticket, options);

        return Task.FromResult(result: 0);
    }

    public Task<AuthenticationTicket> RetrieveAsync(string key)
    {
        _cache.TryGetValue(key, out AuthenticationTicket ticket);

        return Task.FromResult(ticket);
    }

    public Task RemoveAsync(string key)
    {
        _cache.Remove(key);

        return Task.FromResult(result: 0);
    }
}