using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace PluginManager
{
    public class EventRouter
    {
        private readonly List<string> _socketClientAllowedBinaries = new List<string>
            {"Auditor.dll", "GameWatcher.dll", "HARATSeATSRP.dll", "PUBGWeekly.dll"};

        internal readonly AsyncEvent<Func<SocketChannel, Task>> ChannelCreatedEvent =
            new AsyncEvent<Func<SocketChannel, Task>>();

        internal readonly AsyncEvent<Func<SocketChannel, Task>> ChannelDestroyedEvent =
            new AsyncEvent<Func<SocketChannel, Task>>();

        internal readonly AsyncEvent<Func<SocketChannel, SocketChannel, Task>> ChannelUpdatedEvent =
            new AsyncEvent<Func<SocketChannel, SocketChannel, Task>>();

        internal readonly AsyncEvent<Func<SocketGuild, Task>> GuildAvailableEvent =
            new AsyncEvent<Func<SocketGuild, Task>>();

        internal readonly AsyncEvent<Func<SocketGuild, Task>> GuildMembersDownloadedEvent =
            new AsyncEvent<Func<SocketGuild, Task>>();

        internal readonly AsyncEvent<Func<SocketGuildUser, SocketGuildUser, Task>> GuildMemberUpdatedEvent =
            new AsyncEvent<Func<SocketGuildUser, SocketGuildUser, Task>>();

        internal readonly AsyncEvent<Func<SocketGuild, Task>> GuildUnavailableEvent =
            new AsyncEvent<Func<SocketGuild, Task>>();

        internal readonly AsyncEvent<Func<SocketGuild, SocketGuild, Task>> GuildUpdatedEvent =
            new AsyncEvent<Func<SocketGuild, SocketGuild, Task>>();

        internal readonly AsyncEvent<Func<SocketGuild, Task>> JoinedGuildEvent =
            new AsyncEvent<Func<SocketGuild, Task>>();

        internal readonly AsyncEvent<Func<SocketGuild, Task>>
            LeftGuildEvent = new AsyncEvent<Func<SocketGuild, Task>>();

        internal readonly AsyncEvent<Func<Cacheable<IMessage, ulong>, ISocketMessageChannel, Task>> MessageDeletedEvent
            = new AsyncEvent<Func<Cacheable<IMessage, ulong>, ISocketMessageChannel, Task>>();

        internal readonly AsyncEvent<Func<SocketMessage, Task>> MessageReceivedEvent =
            new AsyncEvent<Func<SocketMessage, Task>>();

        internal readonly AsyncEvent<Func<IReadOnlyCollection<Cacheable<IMessage, ulong>>, ISocketMessageChannel, Task>>
            MessagesBulkDeletedEvent =
                new AsyncEvent<Func<IReadOnlyCollection<Cacheable<IMessage, ulong>>, ISocketMessageChannel, Task>>();

        internal readonly AsyncEvent<Func<Cacheable<IMessage, ulong>, SocketMessage, ISocketMessageChannel, Task>>
            MessageUpdatedEvent =
                new AsyncEvent<Func<Cacheable<IMessage, ulong>, SocketMessage, ISocketMessageChannel, Task>>();

        internal readonly AsyncEvent<Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, SocketReaction, Task>>
            ReactionAddedEvent =
                new AsyncEvent<Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, SocketReaction, Task>>();

        internal readonly AsyncEvent<Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, SocketReaction, Task>>
            ReactionRemovedEvent =
                new AsyncEvent<Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, SocketReaction, Task>>();

        internal readonly AsyncEvent<Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, Task>>
            ReactionsClearedEvent = new AsyncEvent<Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, Task>>();

        internal readonly AsyncEvent<Func<SocketGroupUser, Task>> RecipientAddedEvent =
            new AsyncEvent<Func<SocketGroupUser, Task>>();

        internal readonly AsyncEvent<Func<SocketGroupUser, Task>> RecipientRemovedEvent =
            new AsyncEvent<Func<SocketGroupUser, Task>>();

        internal readonly AsyncEvent<Func<SocketRole, Task>>
            RoleCreatedEvent = new AsyncEvent<Func<SocketRole, Task>>();

        internal readonly AsyncEvent<Func<SocketRole, Task>>
            RoleDeletedEvent = new AsyncEvent<Func<SocketRole, Task>>();

        internal readonly AsyncEvent<Func<SocketRole, SocketRole, Task>> RoleUpdatedEvent =
            new AsyncEvent<Func<SocketRole, SocketRole, Task>>();

        internal readonly AsyncEvent<Func<SocketSelfUser, SocketSelfUser, Task>> SelfUpdatedEvent =
            new AsyncEvent<Func<SocketSelfUser, SocketSelfUser, Task>>();

        internal readonly AsyncEvent<Func<SocketUser, SocketGuild, Task>> UserBannedEvent =
            new AsyncEvent<Func<SocketUser, SocketGuild, Task>>();

        internal readonly AsyncEvent<Func<SocketUser, ISocketMessageChannel, Task>> UserIsTypingEvent =
            new AsyncEvent<Func<SocketUser, ISocketMessageChannel, Task>>();

        internal readonly AsyncEvent<Func<SocketGuildUser, Task>> UserJoinedEvent =
            new AsyncEvent<Func<SocketGuildUser, Task>>();

        internal readonly AsyncEvent<Func<SocketGuildUser, Task>> UserLeftEvent =
            new AsyncEvent<Func<SocketGuildUser, Task>>();

        internal readonly AsyncEvent<Func<SocketUser, SocketGuild, Task>> UserUnbannedEvent =
            new AsyncEvent<Func<SocketUser, SocketGuild, Task>>();

        internal readonly AsyncEvent<Func<SocketUser, SocketUser, Task>> UserUpdatedEvent =
            new AsyncEvent<Func<SocketUser, SocketUser, Task>>();

        internal readonly AsyncEvent<Func<SocketUser, SocketVoiceState, SocketVoiceState, Task>>
            UserVoiceStateUpdatedEvent = new AsyncEvent<Func<SocketUser, SocketVoiceState, SocketVoiceState, Task>>();

        internal readonly AsyncEvent<Func<SocketVoiceServer, Task>> VoiceServerUpdatedEvent =
            new AsyncEvent<Func<SocketVoiceServer, Task>>();

        private DiscordSocketClient _discordSocketClient;

        public DiscordSocketClient GetDiscordSocketClient()
        {
            //var assembly = Assembly.GetCallingAssembly();
            //if (_socketClientAllowedBinaries.Contains(assembly.ManifestModule.ScopeName))
            //{
            return _discordSocketClient;
            //}
            //else
            //{
            //    throw new Exception("Plugin is not allowed to get this interface");
            //}
        }

        public event Func<SocketChannel, Task> ChannelCreated
        {
            add => ChannelCreatedEvent.Add(value);
            remove => ChannelCreatedEvent.Remove(value);
        }

        /// <summary> Fired when a channel is destroyed. </summary>
        /// <remarks>
        ///     <para>
        ///         This event is fired when a generic channel has been destroyed. The event handler must return a
        ///         <see cref="Task" /> and accept a <see cref="SocketChannel" /> as its parameter.
        ///     </para>
        ///     <para>
        ///         The destroyed channel is passed into the event handler parameter. The given channel type may
        ///         include, but not limited to, Private Channels (DM, Group), Guild Channels (Text, Voice, Category);
        ///         see the derived classes of <see cref="SocketChannel" /> for more details.
        ///     </para>
        /// </remarks>
        /// <example>
        ///     <code language="cs" region="ChannelDestroyed"
        ///         source="..\Discord.Net.Examples\WebSocket\BaseSocketClient.Events.Examples.cs" />
        /// </example>
        public event Func<SocketChannel, Task> ChannelDestroyed
        {
            add => ChannelDestroyedEvent.Add(value);
            remove => ChannelDestroyedEvent.Remove(value);
        }

        /// <summary> Fired when a channel is updated. </summary>
        /// <remarks>
        ///     <para>
        ///         This event is fired when a generic channel has been destroyed. The event handler must return a
        ///         <see cref="Task" /> and accept 2 <see cref="SocketChannel" /> as its parameters.
        ///     </para>
        ///     <para>
        ///         The original (prior to update) channel is passed into the first <see cref="SocketChannel" />, while
        ///         the updated channel is passed into the second. The given channel type may include, but not limited
        ///         to, Private Channels (DM, Group), Guild Channels (Text, Voice, Category); see the derived classes of
        ///         <see cref="SocketChannel" /> for more details.
        ///     </para>
        /// </remarks>
        /// <example>
        ///     <code language="cs" region="ChannelUpdated"
        ///         source="..\Discord.Net.Examples\WebSocket\BaseSocketClient.Events.Examples.cs" />
        /// </example>
        public event Func<SocketChannel, SocketChannel, Task> ChannelUpdated
        {
            add => ChannelUpdatedEvent.Add(value);
            remove => ChannelUpdatedEvent.Remove(value);
        }

        //Messages
        /// <summary> Fired when a message is received. </summary>
        /// <remarks>
        ///     <para>
        ///         This event is fired when a message is received. The event handler must return a
        ///         <see cref="Task" /> and accept a <see cref="SocketMessage" /> as its parameter.
        ///     </para>
        ///     <para>
        ///         The message that is sent to the client is passed into the event handler parameter as
        ///         <see cref="SocketMessage" />. This message may be a system message (i.e.
        ///         <see cref="SocketSystemMessage" />) or a user message (i.e. <see cref="SocketUserMessage" />. See the
        ///         derived classes of <see cref="SocketMessage" /> for more details.
        ///     </para>
        /// </remarks>
        /// <example>
        ///     <para>The example below checks if the newly received message contains the target user.</para>
        ///     <code language="cs" region="MessageReceived"
        ///         source="..\Discord.Net.Examples\WebSocket\BaseSocketClient.Events.Examples.cs" />
        /// </example>
        public event Func<SocketMessage, Task> MessageReceived
        {
            add => MessageReceivedEvent.Add(value);
            remove => MessageReceivedEvent.Remove(value);
        }

        /// <summary> Fired when a message is deleted. </summary>
        /// <remarks>
        ///     <para>
        ///         This event is fired when a message is deleted. The event handler must return a
        ///         <see cref="Task" /> and accept a <see cref="Cacheable{TEntity,TId}" /> and
        ///         <see cref="ISocketMessageChannel" /> as its parameters.
        ///     </para>
        ///     <para>
        ///         <note type="important">
        ///             It is not possible to retrieve the message via
        ///             <see cref="Cacheable{TEntity,TId}.DownloadAsync" />; the message cannot be retrieved by Discord
        ///             after the message has been deleted.
        ///         </note>
        ///         If caching is enabled via <see cref="DiscordSocketConfig" />, the
        ///         <see cref="Cacheable{TEntity,TId}" /> entity will contain the deleted message; otherwise, in event
        ///         that the message cannot be retrieved, the snowflake ID of the message is preserved in the
        ///         <see cref="ulong" />.
        ///     </para>
        ///     <para>
        ///         The source channel of the removed message will be passed into the
        ///         <see cref="ISocketMessageChannel" /> parameter.
        ///     </para>
        /// </remarks>
        /// <example>
        ///     <code language="cs" region="MessageDeleted"
        ///         source="..\Discord.Net.Examples\WebSocket\BaseSocketClient.Events.Examples.cs" />
        /// </example>
        public event Func<Cacheable<IMessage, ulong>, ISocketMessageChannel, Task> MessageDeleted
        {
            add => MessageDeletedEvent.Add(value);
            remove => MessageDeletedEvent.Remove(value);
        }

        /// <summary> Fired when multiple messages are bulk deleted. </summary>
        /// <remarks>
        ///     <note>
        ///         The <see cref="MessageDeleted" /> event will not be fired for individual messages contained in this event.
        ///     </note>
        ///     <para>
        ///         This event is fired when multiple messages are bulk deleted. The event handler must return a
        ///         <see cref="Task" /> and accept an <see cref="IReadOnlyCollection{Cacheable}" /> and
        ///         <see cref="ISocketMessageChannel" /> as its parameters.
        ///     </para>
        ///     <para>
        ///         <note type="important">
        ///             It is not possible to retrieve the message via
        ///             <see cref="Cacheable{TEntity,TId}.DownloadAsync" />; the message cannot be retrieved by Discord
        ///             after the message has been deleted.
        ///         </note>
        ///         If caching is enabled via <see cref="DiscordSocketConfig" />, the
        ///         <see cref="Cacheable{TEntity,TId}" /> entity will contain the deleted message; otherwise, in event
        ///         that the message cannot be retrieved, the snowflake ID of the message is preserved in the
        ///         <see cref="ulong" />.
        ///     </para>
        ///     <para>
        ///         The source channel of the removed message will be passed into the
        ///         <see cref="ISocketMessageChannel" /> parameter.
        ///     </para>
        /// </remarks>
        public event Func<IReadOnlyCollection<Cacheable<IMessage, ulong>>, ISocketMessageChannel, Task>
            MessagesBulkDeleted
            {
                add => MessagesBulkDeletedEvent.Add(value);
                remove => MessagesBulkDeletedEvent.Remove(value);
            }

        /// <summary> Fired when a message is updated. </summary>
        /// <remarks>
        ///     <para>
        ///         This event is fired when a message is updated. The event handler must return a
        ///         <see cref="Task" /> and accept a <see cref="Cacheable{TEntity,TId}" />, <see cref="SocketMessage" />,
        ///         and <see cref="ISocketMessageChannel" /> as its parameters.
        ///     </para>
        ///     <para>
        ///         If caching is enabled via <see cref="DiscordSocketConfig" />, the
        ///         <see cref="Cacheable{TEntity,TId}" /> entity will contain the original message; otherwise, in event
        ///         that the message cannot be retrieved, the snowflake ID of the message is preserved in the
        ///         <see cref="ulong" />.
        ///     </para>
        ///     <para>
        ///         The updated message will be passed into the <see cref="SocketMessage" /> parameter.
        ///     </para>
        ///     <para>
        ///         The source channel of the updated message will be passed into the
        ///         <see cref="ISocketMessageChannel" /> parameter.
        ///     </para>
        /// </remarks>
        public event Func<Cacheable<IMessage, ulong>, SocketMessage, ISocketMessageChannel, Task> MessageUpdated
        {
            add => MessageUpdatedEvent.Add(value);
            remove => MessageUpdatedEvent.Remove(value);
        }

        /// <summary> Fired when a reaction is added to a message. </summary>
        /// <remarks>
        ///     <para>
        ///         This event is fired when a reaction is added to a user message. The event handler must return a
        ///         <see cref="Task" /> and accept a <see cref="Cacheable{TEntity,TId}" />, an
        ///         <see cref="ISocketMessageChannel" />, and a <see cref="SocketReaction" /> as its parameter.
        ///     </para>
        ///     <para>
        ///         If caching is enabled via <see cref="DiscordSocketConfig" />, the
        ///         <see cref="Cacheable{TEntity,TId}" /> entity will contain the original message; otherwise, in event
        ///         that the message cannot be retrieved, the snowflake ID of the message is preserved in the
        ///         <see cref="ulong" />.
        ///     </para>
        ///     <para>
        ///         The source channel of the reaction addition will be passed into the
        ///         <see cref="ISocketMessageChannel" /> parameter.
        ///     </para>
        ///     <para>
        ///         The reaction that was added will be passed into the <see cref="SocketReaction" /> parameter.
        ///     </para>
        ///     <note>
        ///         When fetching the reaction from this event, a user may not be provided under
        ///         <see cref="SocketReaction.User" />. Please see the documentation of the property for more
        ///         information.
        ///     </note>
        /// </remarks>
        /// <example>
        ///     <code language="cs" region="ReactionAdded"
        ///         source="..\Discord.Net.Examples\WebSocket\BaseSocketClient.Events.Examples.cs" />
        /// </example>
        public event Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, SocketReaction, Task> ReactionAdded
        {
            add => ReactionAddedEvent.Add(value);
            remove => ReactionAddedEvent.Remove(value);
        }

        /// <summary> Fired when a reaction is removed from a message. </summary>
        public event Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, SocketReaction, Task> ReactionRemoved
        {
            add => ReactionRemovedEvent.Add(value);
            remove => ReactionRemovedEvent.Remove(value);
        }

        /// <summary> Fired when all reactions to a message are cleared. </summary>
        public event Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, Task> ReactionsCleared
        {
            add => ReactionsClearedEvent.Add(value);
            remove => ReactionsClearedEvent.Remove(value);
        }

        //Roles
        /// <summary> Fired when a role is created. </summary>
        public event Func<SocketRole, Task> RoleCreated
        {
            add => RoleCreatedEvent.Add(value);
            remove => RoleCreatedEvent.Remove(value);
        }

        /// <summary> Fired when a role is deleted. </summary>
        public event Func<SocketRole, Task> RoleDeleted
        {
            add => RoleDeletedEvent.Add(value);
            remove => RoleDeletedEvent.Remove(value);
        }

        /// <summary> Fired when a role is updated. </summary>
        public event Func<SocketRole, SocketRole, Task> RoleUpdated
        {
            add => RoleUpdatedEvent.Add(value);
            remove => RoleUpdatedEvent.Remove(value);
        }

        //Guilds
        /// <summary> Fired when the connected account joins a guild. </summary>
        public event Func<SocketGuild, Task> JoinedGuild
        {
            add => JoinedGuildEvent.Add(value);
            remove => JoinedGuildEvent.Remove(value);
        }

        /// <summary> Fired when the connected account leaves a guild. </summary>
        public event Func<SocketGuild, Task> LeftGuild
        {
            add => LeftGuildEvent.Add(value);
            remove => LeftGuildEvent.Remove(value);
        }

        /// <summary> Fired when a guild becomes available. </summary>
        public event Func<SocketGuild, Task> GuildAvailable
        {
            add => GuildAvailableEvent.Add(value);
            remove => GuildAvailableEvent.Remove(value);
        }

        /// <summary> Fired when a guild becomes unavailable. </summary>
        public event Func<SocketGuild, Task> GuildUnavailable
        {
            add => GuildUnavailableEvent.Add(value);
            remove => GuildUnavailableEvent.Remove(value);
        }

        /// <summary> Fired when offline guild members are downloaded. </summary>
        public event Func<SocketGuild, Task> GuildMembersDownloaded
        {
            add => GuildMembersDownloadedEvent.Add(value);
            remove => GuildMembersDownloadedEvent.Remove(value);
        }

        /// <summary> Fired when a guild is updated. </summary>
        public event Func<SocketGuild, SocketGuild, Task> GuildUpdated
        {
            add => GuildUpdatedEvent.Add(value);
            remove => GuildUpdatedEvent.Remove(value);
        }

        //Users
        /// <summary> Fired when a user joins a guild. </summary>
        public event Func<SocketGuildUser, Task> UserJoined
        {
            add => UserJoinedEvent.Add(value);
            remove => UserJoinedEvent.Remove(value);
        }

        /// <summary> Fired when a user leaves a guild. </summary>
        public event Func<SocketGuildUser, Task> UserLeft
        {
            add => UserLeftEvent.Add(value);
            remove => UserLeftEvent.Remove(value);
        }

        /// <summary> Fired when a user is banned from a guild. </summary>
        public event Func<SocketUser, SocketGuild, Task> UserBanned
        {
            add => UserBannedEvent.Add(value);
            remove => UserBannedEvent.Remove(value);
        }

        /// <summary> Fired when a user is unbanned from a guild. </summary>
        public event Func<SocketUser, SocketGuild, Task> UserUnbanned
        {
            add => UserUnbannedEvent.Add(value);
            remove => UserUnbannedEvent.Remove(value);
        }

        /// <summary> Fired when a user is updated. </summary>
        public event Func<SocketUser, SocketUser, Task> UserUpdated
        {
            add => UserUpdatedEvent.Add(value);
            remove => UserUpdatedEvent.Remove(value);
        }

        /// <summary> Fired when a guild member is updated, or a member presence is updated. </summary>
        public event Func<SocketGuildUser, SocketGuildUser, Task> GuildMemberUpdated
        {
            add => GuildMemberUpdatedEvent.Add(value);
            remove => GuildMemberUpdatedEvent.Remove(value);
        }

        /// <summary> Fired when a user joins, leaves, or moves voice channels. </summary>
        public event Func<SocketUser, SocketVoiceState, SocketVoiceState, Task> UserVoiceStateUpdated
        {
            add => UserVoiceStateUpdatedEvent.Add(value);
            remove => UserVoiceStateUpdatedEvent.Remove(value);
        }

        /// <summary> Fired when the bot connects to a Discord voice server. </summary>
        public event Func<SocketVoiceServer, Task> VoiceServerUpdated
        {
            add => VoiceServerUpdatedEvent.Add(value);
            remove => VoiceServerUpdatedEvent.Remove(value);
        }

        /// <summary> Fired when the connected account is updated. </summary>
        public event Func<SocketSelfUser, SocketSelfUser, Task> CurrentUserUpdated
        {
            add => SelfUpdatedEvent.Add(value);
            remove => SelfUpdatedEvent.Remove(value);
        }

        /// <summary> Fired when a user starts typing. </summary>
        public event Func<SocketUser, ISocketMessageChannel, Task> UserIsTyping
        {
            add => UserIsTypingEvent.Add(value);
            remove => UserIsTypingEvent.Remove(value);
        }

        /// <summary> Fired when a user joins a group channel. </summary>
        public event Func<SocketGroupUser, Task> RecipientAdded
        {
            add => RecipientAddedEvent.Add(value);
            remove => RecipientAddedEvent.Remove(value);
        }

        /// <summary> Fired when a user is removed from a group channel. </summary>
        public event Func<SocketGroupUser, Task> RecipientRemoved
        {
            add => RecipientRemovedEvent.Add(value);
            remove => RecipientRemovedEvent.Remove(value);
        }

        public void SetupEventRouter(DiscordSocketClient dsc)
        {
            _discordSocketClient = dsc;

            dsc.RecipientRemoved += user =>
            {
                if (RecipientRemovedEvent.HasSubscribers)
                    foreach (var sub in RecipientRemovedEvent.Subscriptions)
                        sub.Invoke(user);

                return Task.CompletedTask;
            };

            dsc.RecipientAdded += user =>
            {
                if (RecipientAddedEvent.HasSubscribers)
                    foreach (var sub in RecipientAddedEvent.Subscriptions)
                        sub.Invoke(user);

                return Task.CompletedTask;
            };

            dsc.UserIsTyping += (user, channel) =>
            {
                if (UserIsTypingEvent.HasSubscribers)
                    if (channel is SocketGuildChannel socketChannel)
                    {
                        var guildId = socketChannel.Guild.Id;
                        foreach (var sub in UserIsTypingEvent.Subscriptions)
                        {
                            var moduleName = sub.Method.Module.Name;
                            if (PluginHandler.Instance.ShouldExecutePlugin(moduleName, guildId))
                                sub.Invoke(user, channel);
                        }
                    }

                return Task.CompletedTask;
            };

            dsc.CurrentUserUpdated += (user, selfUser) =>
            {
                if (SelfUpdatedEvent.HasSubscribers)
                    foreach (var sub in SelfUpdatedEvent.Subscriptions)
                        sub.Invoke(user, selfUser);

                return Task.CompletedTask;
            };

            dsc.VoiceServerUpdated += server =>
            {
                if (VoiceServerUpdatedEvent.HasSubscribers)
                    foreach (var sub in VoiceServerUpdatedEvent.Subscriptions)
                        sub.Invoke(server);

                return Task.CompletedTask;
            };

            dsc.UserUpdated += (user, socketUser) =>
            {
                if (UserUpdatedEvent.HasSubscribers)
                    if (user is SocketGuildUser socketGuildUser)
                    {
                        var guildId = socketGuildUser.Guild.Id;

                        foreach (var sub in UserUpdatedEvent.Subscriptions)
                        {
                            var moduleName = sub.Method.Module.Name;
                            if (PluginHandler.Instance.ShouldExecutePlugin(moduleName, guildId))
                                sub.Invoke(user, socketUser);
                        }
                    }

                return Task.CompletedTask;
            };

            /*
            dsc.UserUnbanned += (user, guild) => { };

            dsc.UserBanned += (user, guild) => { };

            dsc.GuildUpdated += (guild, socketGuild) => { };

            dsc.GuildMembersDownloaded += guild => { };

            dsc.GuildUnavailable += guild => { };

            dsc.GuildAvailable += guild => { };

            dsc.LeftGuild += guild => { };

            dsc.JoinedGuild += guild => { };

            dsc.RoleUpdated += (role, socketRole) => { };

            dsc.RoleDeleted += role => { };

            dsc.RoleCreated += role => { };

            dsc.ReactionAdded += (cacheable, channel, arg3) => { };

            dsc.ReactionRemoved += (cacheable, channel, arg3) => { };

            dsc.ReactionsCleared += (cacheable, channel) => { };
            */

            dsc.UserVoiceStateUpdated += (user, sktVoiceStateOld, sktVoiceStateNew) =>
            {
                if (UserVoiceStateUpdatedEvent.HasSubscribers)
                    if (sktVoiceStateNew.VoiceChannel != null && sktVoiceStateNew.VoiceChannel.Guild != null)
                    {
                        var guildId = sktVoiceStateNew.VoiceChannel.Guild.Id;

                        foreach (var sub in UserVoiceStateUpdatedEvent.Subscriptions)
                        {
                            var moduleName = sub.Method.Module.Name;
                            if (PluginHandler.Instance.ShouldExecutePlugin(moduleName, guildId))
                                sub.Invoke(user, sktVoiceStateOld, sktVoiceStateNew);
                        }
                    }

                return Task.CompletedTask;
            };

            dsc.UserJoined += user =>
            {
                if (UserJoinedEvent.HasSubscribers)
                {
                    var guildId = user.Guild.Id;

                    foreach (var sub in UserJoinedEvent.Subscriptions)
                    {
                        var moduleName = sub.Method.Module.Name;
                        if (PluginHandler.Instance.ShouldExecutePlugin(moduleName, guildId))
                            sub.Invoke(user);
                    }
                }

                return Task.CompletedTask;
            };

            dsc.MessageReceived += message =>
            {
                if (MessageReceivedEvent.HasSubscribers)
                    if (message.Channel is SocketGuildChannel socketChannel)
                    {
                        var guildId = socketChannel.Guild.Id;
                        foreach (var sub in MessageReceivedEvent.Subscriptions)
                        {
                            var moduleName = sub.Method.Module.Name;
                            if (PluginHandler.Instance.ShouldExecutePlugin(moduleName, guildId))
                                sub.Invoke(message);
                        }
                    }

                return Task.CompletedTask;
            };

            dsc.MessageDeleted += (cacheable, channel) =>
            {
                if (MessageDeletedEvent.HasSubscribers)
                    if (channel is SocketGuildChannel socketChannel)
                    {
                        var guildId = socketChannel.Guild.Id;
                        foreach (var sub in MessageDeletedEvent.Subscriptions)
                        {
                            var moduleName = sub.Method.Module.Name;
                            if (PluginHandler.Instance.ShouldExecutePlugin(moduleName, guildId))
                                sub.Invoke(cacheable, channel);
                        }
                    }

                return Task.CompletedTask;
            };

            dsc.GuildMemberUpdated += (oldUser, newUser) =>
            {
                if (GuildMemberUpdatedEvent.HasSubscribers)
                {
                    var guildId = newUser.Guild.Id;
                    foreach (var sub in GuildMemberUpdatedEvent.Subscriptions)
                    {
                        var moduleName = sub.Method.Module.Name;
                        if (PluginHandler.Instance.ShouldExecutePlugin(moduleName, guildId))
                            sub.Invoke(oldUser, newUser);
                    }
                }

                return Task.CompletedTask;
            };

            dsc.UserLeft += (guild, user) =>
            {
                if (UserLeftEvent.HasSubscribers)
                {
                    var guildId = guild.Id;
                    foreach (var sub in UserLeftEvent.Subscriptions)
                    {
                        var moduleName = sub.Method.Module.Name;
                        if (PluginHandler.Instance.ShouldExecutePlugin(moduleName, guildId))
                            sub.Invoke(user);
                    }
                }

                return Task.CompletedTask;
            };
        }

        private Task Dsc_UserLeft(SocketGuild arg1, SocketUser arg2)
        {
            throw new NotImplementedException();
        }
    }
}