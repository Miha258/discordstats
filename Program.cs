using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace DobbiKovDiscordBot
{

    class Program
    {
        static void Main(string[] args) => new Program().RunBotAsync().GetAwaiter().GetResult();

        private DiscordSocketClient client;
        private CommandService commands;
        private IServiceProvider services;
        static private Dictionary<ulong, byte> SendedMessages = new Dictionary<ulong, byte>();

        public async Task RunBotAsync()
        {

            commands = new CommandService();

            var config = new DiscordSocketConfig();
            config.GatewayIntents = GatewayIntents.All;

            client = new DiscordSocketClient(config);

            string token = "";

            client.Log += clientLog;
            client.Ready += Ready;
            client.UserJoined += UserJoined;
            client.MessageReceived += PingCheck;
            client.MessageReceived += HandleCommandAsync;
            

            await commands.AddModulesAsync(Assembly.GetEntryAssembly(), services);
            await client.LoginAsync(TokenType.Bot, token);

            await client.StartAsync();
            await Task.Delay(-1);
        }
        
        private async Task HandleCommandAsync(SocketMessage arg)
        {
            var message = arg as SocketUserMessage;
            var context = new SocketCommandContext(client, message);
            if (message.Author.IsBot)
                return;

            int argPos = 0;
            if (message.HasStringPrefix("!", ref argPos))
            {
                var result = await commands.ExecuteAsync(context, argPos, services);
                if (!result.IsSuccess)
                    Console.WriteLine(result.ErrorReason);
                if (result.Error.Equals(CommandError.UnmetPrecondition))
                    await message.Channel.SendMessageAsync(result.ErrorReason);
            }
        }


        public static async Task SetInterval(Action action, TimeSpan timeout)
        {
            action();
            await Task.Delay(timeout).ConfigureAwait(false);
            SetInterval(action, timeout);
        }

        private async Task PingCheck(SocketMessage arg)
        {   
            if (!SendedMessages.ContainsKey(arg.Author.Id))
            {
                SendedMessages.Add(arg.Author.Id, 1);
            }
            else
            {   
                SendedMessages[arg.Author.Id] += 1;
            }
            
        }

        
        private Task clientLog(LogMessage arg)
        {
            Console.WriteLine(arg);
            return Task.CompletedTask;
        }
        
        private async Task Ready()
        {
            await SetInterval(async () =>
            {
                await UpdateMembers();
                
            }, TimeSpan.FromMinutes(5d));
            
            await SetInterval(async () =>
            {
                foreach (KeyValuePair<ulong, byte> entry in SendedMessages)
                {
                    if (entry.Value > 7)
                    {
                        try
                        {
                            SocketGuild Guild = client.GetGuild(947140017770344528);
                            SocketGuildUser User = Guild.GetUser(entry.Key);
                            await User.SetTimeOutAsync(TimeSpan.FromMinutes(20d));
                        }
                        catch (Discord.Net.HttpException)
                        {
                            Console.WriteLine("Permissions Error");
                        }
                    }
                }
                SendedMessages.Clear();
            }, TimeSpan.FromSeconds(4d));
        }
        
        private async Task UserJoined(SocketGuildUser User)
        {
            var channel = client.GetChannel(947160663267106836) as SocketTextChannel;
            await channel.SendMessageAsync($"🖐 **Привіт** {User.Mention} **раді тебе бачити на нашому сервері.Приємної гри в Dota 2**");
        }
        
        private async Task UpdateMembers()
        {   
            SocketGuild Guild = client.GetGuild(947140017770344528);
            
            int MemberCount = Guild.MemberCount;
            int MemberIdle = 0;
            int MemberOffline = 0;
            int MemberOnline = 0;
            int MemberInVoice = 0;


            foreach (SocketGuildUser User in Guild.Users)
            {
                
                switch (User.Status)
                {
                    case UserStatus.Offline:
                        MemberOffline += 1;
                        break;
                    case UserStatus.Idle:
                        MemberIdle += 1;
                        break;
                    case UserStatus.DoNotDisturb:
                    case UserStatus.Online:
                        MemberOnline += 1;
                        break;
                }
                
                if (User.VoiceState != null)
                    MemberInVoice += 1;
            }

            var ChannelAllMembers = client.GetChannel(948887013275340860) as IGuildChannel;
            var ChannelOnline = client.GetChannel(948887107215188028) as IGuildChannel;
            var ChannelOffline = client.GetChannel(948887187292848168) as IGuildChannel;
            var ChannelIdle = client.GetChannel(948887217500201020) as IGuildChannel;
            var ChannelInVoice = client.GetChannel(948887249439817728) as IGuildChannel;

            await ChannelAllMembers.ModifyAsync(Channel =>
            {
                Channel.Name = $"All Members: {MemberCount}";
            });
            await ChannelOnline.ModifyAsync(Channel =>
            {
                Channel.Name = $"Online: {MemberOnline}";
            });
            await ChannelOffline.ModifyAsync(Channel =>
            {
                Channel.Name = $"Offline: {MemberOffline}";
            });
            await ChannelIdle.ModifyAsync(Channel =>
            {
                Channel.Name = $"Idle: {MemberIdle}";
            });
            await ChannelInVoice.ModifyAsync(Channel =>
            {
                Channel.Name = $"In voice: {MemberInVoice}";
            });
        }
    }
}
