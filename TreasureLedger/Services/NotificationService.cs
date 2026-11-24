using TreasureLedger.Models;
using System.Text;
using System.Collections.Concurrent;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.System.String;

namespace TreasureLedger.Services;

public sealed class NotificationService(Configuration configuration, IChatGui chat, IPluginLog log)
{
    private readonly ConcurrentQueue<(NoticeChannel Channel, string Text)> pending = new();

    public void Send(NoticeChannel channel, string text)
    {
        if (channel == NoticeChannel.Off || string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        if (channel == NoticeChannel.Party && !configuration.AllowPartyMessages)
        {
            return;
        }

        var body = text.Replace('\r', ' ').Replace('\n', ' ');
        if (Encoding.UTF8.GetByteCount(body) > 450)
        {
            chat.PrintError("挖宝账本：消息超过 450 字节，请缩短文本。");
            return;
        }

        pending.Enqueue((channel, body));
    }

    public unsafe void Update()
    {
        if (!pending.TryDequeue(out var notice))
        {
            return;
        }

        if (notice.Channel == NoticeChannel.Party && !configuration.AllowPartyMessages)
        {
            return;
        }

        try
        {
            var module = UIModule.Instance();
            if (module == null)
            {
                return;
            }

            var command = notice.Channel == NoticeChannel.Party ? "/p" : "/e";
            using var message = new Utf8String($"{command} {notice.Text}");
            module->ProcessChatBoxEntry(&message);
        }
        catch (Exception exception)
        {
            log.Error(exception, "发送挖宝消息时出错");
        }
    }
}
