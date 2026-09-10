using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using TerrariaApi.Server;
using TShockAPI;

namespace Back {
    [ApiVersion(2, 1)]
    public class Back : TerrariaPlugin {
        public override string Name => "Back";
        public override string Author => "Melton";
        public override Version Version => new Version(1, 0, 1);
        public override string Description => "Teleports you back to your last death location";

        Dictionary<string, Vector2> playerDeathData = new Dictionary<string, Vector2>();

        public Back(Main game) : base(game)
        {}

        public override void Initialize()
        {
            Commands.ChatCommands.Add(new Command("back.back", BackCommand, "back"));
            ServerApi.Hooks.NetGetData.Register(this, OnNetGetData);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Commands.ChatCommands.Remove(new Command("back.back", BackCommand, "back"));
                ServerApi.Hooks.NetGetData.Deregister(this, OnNetGetData);
            }

            base.Dispose(disposing);
        }

        private void OnNetGetData(GetDataEventArgs args)
        {
            PacketTypes MsgID = args.MsgID;

            if (MsgID == PacketTypes.PlayerDeathV2)
            {
                 using (BinaryReader br = new(new MemoryStream(args.Msg.readBuffer, args.Index, args.Length)))
                {
                    byte playerID = br.ReadByte();
                    PlayerDeathReason.FromReader(br);
                    br.ReadInt16();
                    br.ReadByte();
                    br.ReadByte();

                    var player = Main.player[playerID];

                    if (player == null) return;
                    
                    var deathPosition = new Vector2(player.position.X, player.position.Y);
                    playerDeathData[player.name] = deathPosition;
                }
            }
        }

        private void BackCommand(CommandArgs args)
        {
            var player = args.Player;
            if (player == null || !player.Active || !player.RealPlayer) return;
            if (player.Dead)
            {
                player.SendErrorMessage("You can't use this command while dead.");
                return;
            }
            
            if (playerDeathData.TryGetValue(player.Name, out var deathPosition))
            {
                player.Teleport(deathPosition.X, deathPosition.Y);
                player.SendSuccessMessage($"You have been teleported back to your death location.");
            }
            else
            {
                player.SendErrorMessage("No death location found.");
            }
        }
    }
}
