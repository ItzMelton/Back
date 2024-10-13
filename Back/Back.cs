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
        public override Version Version => new Version(1, 0, 0);
        public override string Description => "Teleports you back to the last death you are on";

        Dictionary<string, (Vector2 position, string reason)> playerDeathData = new Dictionary<string, (Vector2, string)>();

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
                    PlayerDeathReason deathReason = PlayerDeathReason.FromReader(br);
                    br.ReadInt16();
                    br.ReadByte();
                    br.ReadByte();

                    var player = Main.player[playerID];
                    var deathPosition = new Vector2(player.position.X, player.position.Y);
                    var deathReasonText = deathReason.GetDeathText(player.name).ToString();

                    playerDeathData[player.name] = (deathPosition, deathReasonText);
                }
            }
        }

        private void BackCommand(CommandArgs args)
        {
            var player = args.Player;
            if (player == null || player.Active || player.RealPlayer) return;
            if (player.Dead)
            {
                player.SendErrorMessage("You can't use this command while dead.");
                return;
            }
            
            if (playerDeathData.TryGetValue(player.Name, out var deathData))
            {
                player.Teleport(deathData.position.X, deathData.position.Y);
                player.SendSuccessMessage($"You have been teleported back to your death location with the reason: {deathData.reason}");
            }
            else
            {
                player.SendErrorMessage("No death location found.");
            }
        }
    }
}
