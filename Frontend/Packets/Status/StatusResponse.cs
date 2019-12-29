using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Frontend.Packets.Status
{
    public struct StatusResponse : IWriteablePacket
    {
        private static JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions()
        {
            IgnoreNullValues = true
        };
        
        public readonly int Id => 0x00;
        public readonly MCConnectionStage Stage => MCConnectionStage.Status;
        public readonly bool IsServerbound => false;

        public readonly int CalculateSize()
        {
            var length = JsonSerializer.SerializeToUtf8Bytes(Data, _jsonSerializerOptions).Length;

            return MCPacketWriter.GetVarIntSize(length)
                 + length;
        }

        public Payload Data;

        public StatusResponse(Payload data)
        {
            Data = data;
        }

        public void Write(IPacketWriter writer)
        {
            var data = JsonSerializer.SerializeToUtf8Bytes(Data, _jsonSerializerOptions);
            writer.WriteVarInt(data.Length);
            writer.WriteBytes(data);
        }
        
        public sealed class Payload
        {
            [JsonPropertyName("version")] public VersionPayload Version { get; set; }
            [JsonPropertyName("players")] public PlayersPayload Players { get; set; }
            [JsonPropertyName("description")] public Chat Description { get; set; }
            [JsonPropertyName("favicon")] public string? FavIcon { get; set; }

            public Payload(VersionPayload version, PlayersPayload players, Chat description, string? favIcon)
            {
                Version = version;
                Players = players;
                Description = description;
                FavIcon = favIcon;
            }

            public sealed class VersionPayload
            {
                [JsonPropertyName("name")] public string Name { get; set; }
                [JsonPropertyName("protocol")] public int Version { get; set; }

                public VersionPayload(string name, int version)
                {
                    Name = name;
                    Version = version;
                }
            }

            public sealed class PlayersPayload
            {
                [JsonPropertyName("max")] public int Max { get; set; }
                [JsonPropertyName("online")] public int Online { get; set; }
                [JsonPropertyName("sample")] public PlayersPayload.Player[]? Sample { get; set; }

                public PlayersPayload(int max, int online, Player[]? sample)
                {
                    Max = max;
                    Online = online;
                    Sample = sample;
                }

                public sealed class Player
                {
                    [JsonPropertyName("name")] public string Name { get; set; }
                    [JsonPropertyName("id")] public string Id { get; set; }

                    public Player(string name, string id)
                    {
                        Name = name;
                        Id = id;
                    }
                }
            }
        }
    }
}