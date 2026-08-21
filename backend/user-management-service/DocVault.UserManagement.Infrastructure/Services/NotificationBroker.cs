using System.Collections.Concurrent;
using System.Threading.Channels;

namespace DocVault.UserManagement.Infrastructure.Services;

public class NotificationBroker
{
    private readonly ConcurrentDictionary<string, List<Channel<string>>> _userChannels = new();
    private readonly List<Channel<string>> _adminChannels = new();
    private readonly object _adminLock = new();

    public ChannelReader<string> SubscribeUser(string userId)
    {
        var ch = Channel.CreateUnbounded<string>();
        var list = _userChannels.GetOrAdd(userId, _ => new List<Channel<string>>());
        lock (list)
        {
            list.Add(ch);
        }
        return ch.Reader;
    }

    public void UnsubscribeUser(string userId, ChannelReader<string> reader)
    {
        if (_userChannels.TryGetValue(userId, out var list))
        {
            lock (list)
            {
                var toRemove = list.FirstOrDefault(c => c.Reader == reader);
                if (toRemove != null) list.Remove(toRemove);
            }
        }
    }

    public ChannelReader<string> SubscribeAdmin()
    {
        var ch = Channel.CreateUnbounded<string>();
        lock (_adminLock)
        {
            _adminChannels.Add(ch);
        }
        return ch.Reader;
    }

    public void UnsubscribeAdmin(ChannelReader<string> reader)
    {
        lock (_adminLock)
        {
            var toRemove = _adminChannels.FirstOrDefault(c => c.Reader == reader);
            if (toRemove != null) _adminChannels.Remove(toRemove);
        }
    }

    public void PublishToUser(string userId, string message)
    {
        if (_userChannels.TryGetValue(userId, out var list))
        {
            List<Channel<string>> copy;
            lock (list) { copy = list.ToList(); }
            foreach (var ch in copy)
            {
                ch.Writer.TryWrite(message);
            }
        }
    }

    public void PublishToAdmins(string message)
    {
        List<Channel<string>> copy;
        lock (_adminLock)
        {
            copy = _adminChannels.ToList();
        }
        foreach (var ch in copy)
        {
            ch.Writer.TryWrite(message);
        }
    }
}
