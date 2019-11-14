using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using CommandHandler;
using Discord.WebSocket;

namespace JoinQuitMessages.Configuration
{
    // This class is the handler for all discord messages sent to this plugin.
    class DiscordConfigurationHandler
    {
        [Command("joinquitmessages", "Configures Join and Quit messages for your discord guild.")]
        [Alias("jqm")]
        public async void JoinQuitMessages(string[] parameters, SocketMessage sktMessage,
            DiscordSocketClient discordSocketClient)
        {
            if (parameters.Length == 1)
            {
                await sktMessage.Channel.SendMessageAsync("`JoinQuitMessages System`\r\n" +
                                                          "Try '!jqm help' for commands");
                return;
            }

            switch (parameters[1].ToLower())
            {
                case "help":
                    await sktMessage.Channel.SendMessageAsync("\r\n`JoinQuitMessages System Help`\r\n" +
                                                              "`!jqm assign`\r\nAssigns the channel you type this command in to be the area where the bot will send Join and Quit Messages");
                    break;
                case "assign":
                    await HandleAssign(parameters, sktMessage, discordSocketClient);
                    break;
            }
        }

        private async Task HandleAssign(string[] parameters, SocketMessage sktMessage, DiscordSocketClient discordSocketClient)
        {
            if (sktMessage.Channel is SocketGuildChannel sktGuildChannel)
            {
                var channelId = sktMessage.Channel.Id;
                var guildId = sktGuildChannel.Guild.Id;

                try
                {
                    ConfigurationHandler.Instance.AssignChannel(guildId, channelId);
                    await sktMessage.Channel.SendMessageAsync(
                        $"Successfully set the channel {sktMessage.Channel.Name} to be the channel to recieve Join & Quit messages!");
                }
                catch (Exception ex)
                {
                    await sktMessage.Channel.SendMessageAsync(
                        $"Unable to set the channel {sktMessage.Channel.Name} to be the channel to recieve Join & Quit messages, error below:\r\n{ex.Message}");
                }
            }
        }
    }
}
