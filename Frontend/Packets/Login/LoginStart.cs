using System;
using System.Text;
using Frontend.Packets.Play;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Frontend.Packets.Login
{
    public struct LoginStart : IReadablePacket
    {
        public readonly int Id => 0x00;
        public readonly MCConnectionStage Stage => MCConnectionStage.Login;

        public string Username;

        public void Read(IPacketReader reader)
        {
            Username = reader.ReadString().ToString();
        }

        public readonly void Process(ILogger logger, IConnectionState connectionState, IServiceProvider serviceProvider)
        {
            if (!connectionState.IsLocal)
            {
                logger.LogInformation($"{Username} is trying to log in from Remote. Refusing.");
                
                connectionState.PacketQueue.Write(new Disconnect(new ChatBuilder()
                                                     .AppendText("Connection from Remote is not supported \n\n")
                                                     .Bold()
                                                     .WithColor("red")
                                                     .Build()));
            }
            else
            {
                connectionState.PlayerEntity = new Player(serviceProvider.GetRequiredService<IEntityManager>().ReserveEntityId(), 0);
                connectionState.PlayerEntity.Username = Username;
                logger.LogInformation($"Logging {Username} in. Entity ID: {connectionState.PlayerEntity.Id.Value}");
                connectionState.Guid = Guid.NewGuid();
                connectionState.PacketQueue.WriteImmediate(new LoginSuccess(connectionState.Guid, Username));
                connectionState.ConnectionStage = MCConnectionStage.Playing;
                connectionState.PacketQueue.Write(new JoinGame(connectionState.PlayerEntity.Id.Value, 1,
                                                               connectionState.PlayerEntity.DimensionId, byte.MinValue,
                                                               "customized", 32, false));
                var brandData = new byte[3];
                MCPacketWriter.DownsizeUtf16("SM3", brandData);
                connectionState.PacketQueue.Write(new ClientboundPluginMessage("minecraft:brand", brandData));
                connectionState.PacketQueue.Write(new ServerDifficulty(1, true));
                connectionState.PacketQueue.Write(new ClientboundPlayerAbilities(
                                                      ClientboundPlayerAbilities.Flags.Flying |
                                                      ClientboundPlayerAbilities.Flags.Invulnerable |
                                                      ClientboundPlayerAbilities.Flags.AllowFlying |
                                                      ClientboundPlayerAbilities.Flags.InstantBreak, 0.5f, 0.1f));
                connectionState.PacketQueue.Write(new ClientboundHeldItemChange(0));
                connectionState.PacketQueue.Write(new DeclareRecipes());
            }
        }
    }
}