﻿using System;
using System.IO;
using System.Linq;
using System.ComponentModel;
using System.Collections.Generic;
using TShockAPI;
using TShockAPI.Extensions;
using Terraria;
using Newtonsoft.Json;
using TerrariaApi;
using TerrariaApi.Server;
using TShockAPI.DB;

namespace VoteKick
{
    [ApiVersion(1, 16)]
    public class Votekick : TerrariaPlugin
    {
        private static VoteKickTimer _timers;

        public override Version Version
        {
            get { return new Version("1.2"); }
        }

        public override string Name
        {
            get { return "Votekick"; }
        }

        public override string Author
        {
            get { return "CAWCAWCAW"; }
        }

        public override string Description
        {
            get { return "Vote to kick, mute, or ban players"; }
        }

        public Votekick(Main game)
            : base(game)
        {
        }

        public override void Initialize()
        {
            ReadConfig();
            _timers = new VoteKickTimer();
            _timers.Start();

            TShockAPI.Commands.ChatCommands.Add(new Command("caw.votekick", Vote, "votekick", "vk"));
            TShockAPI.Commands.ChatCommands.Add(new Command("caw.reloadvotekick", Reload_Config, "reloadvotekick", "rvk"));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
            base.Dispose(disposing);
        }
        public static bool VoteKickRunning = false;
        public static bool VoteMuteRunning = false;
        public static bool VoteBanRunning = false;
        public static Poll poll = new Poll();
        
        public class Poll
        {
            public List<TSPlayer> voters;
            public List<TSPlayer> votedyes;
            public List<TSPlayer> votedno;
            public TSPlayer votedplayer;
            public Poll()
            {
                voters = new List<TSPlayer>();
                votedyes = new List<TSPlayer>();
                votedno = new List<TSPlayer>();
            }
        }

        public static void Vote(CommandArgs args)
        {
            if (args.Parameters.Count == 0)
            {
                args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /votekick [kick/mute/ban/voteyes/voteno/info/cancel]");
                return;
            }
            switch (args.Parameters[0])
            {
                case "voteyes":
                    if (!poll.voters.Contains(args.Player) && (VoteKickRunning || VoteMuteRunning || VoteBanRunning))
                    {
                        args.Player.SendSuccessMessage("You have voted yes in the vote kick.");
                        poll.voters.Add(args.Player);
                        poll.votedyes.Add(args.Player);
                    }
                    else if (poll.voters.Contains(args.Player))
                    {
                        args.Player.SendErrorMessage("You have already voted for this vote!");
                    }
                    else
                    {
                        args.Player.SendInfoMessage("A vote is not running at this time.");
                    }
                    break;
                case "voteno":
                    if (!poll.voters.Contains(args.Player) && (VoteKickRunning || VoteMuteRunning || VoteBanRunning))
                    {
                        args.Player.SendSuccessMessage("You have voted no in the vote kick.");
                        poll.voters.Add(args.Player);
                        poll.votedno.Add(args.Player);
                    }
                    else if (poll.voters.Contains(args.Player))
                    {
                        args.Player.SendErrorMessage("You have already voted for this vote!");
                    }
                    else
                    {
                        args.Player.SendInfoMessage("A vote is not running at this time.");
                    }
                    break;

                case "kick":
                    if (config.CanPlayersVoteKick)
                    {
                        if (TShock.Utils.ActivePlayers() >= Votekick.config.AmountofPlayersForVotesToTakeEffect)
                        {
                            if (args.Parameters.Count > 1 && !VoteKickRunning && !VoteMuteRunning && !VoteBanRunning)
                            {
                                string playerstring = String.Join(" ", args.Parameters.GetRange(1, args.Parameters.Count - 1));
                                var players = TShock.Utils.FindPlayer(playerstring);
                                var plyr = players[0];
                                if (players.Count == 0)
                                {
                                    args.Player.SendErrorMessage("No player matched your query '{0}'", playerstring);
                                }
                                else if (players.Count > 1)
                                {
                                    TShock.Utils.SendMultipleMatchError(args.Player, players.Select(p => p.Name));
                                }

                                TSPlayer.All.SendWarningMessage(args.Player.Name + " has started a votekick against " + plyr.Name);
                                TSPlayer.All.SendSuccessMessage("[VoteKick] To vote type in /votekick <voteyes/voteno>");
                                VoteKickRunning = true;
                                poll.votedplayer = plyr;
                                _timers.Start();
                            }
                            else if (VoteKickRunning || VoteMuteRunning || VoteBanRunning)
                            {
                                args.Player.SendErrorMessage("A player has already started a vote on " + poll.votedplayer.Name);
                            }
                            else
                            {
                                args.Player.SendErrorMessage("Error! Please use /votekick kick <playername>");
                            }
                        }
                        else
                        {
                            args.Player.SendErrorMessage("There is not enough players to enable a vote! {0}/{1} need to be on the server.", TShock.Utils.ActivePlayers(), config.AmountofPlayersForVotesToTakeEffect);
                        }
                    }
                    else
                    {
                        args.Player.SendErrorMessage("The vote ban feature has been turned off by the server owner.");
                    }
                    break;

                case "mute":
                    if (config.CanPlayersVoteMute)
                    {
                        if (TShock.Utils.ActivePlayers() >= Votekick.config.AmountofPlayersForVotesToTakeEffect)
                        {
                            if (args.Parameters.Count > 1 && !VoteKickRunning && !VoteMuteRunning && !VoteBanRunning)
                            {
                                string playerstring = String.Join(" ", args.Parameters.GetRange(1, args.Parameters.Count - 1));
                                var players = TShock.Utils.FindPlayer(playerstring);
                                var plyr = players[0];
                                if (players.Count == 0)
                                {
                                    args.Player.SendErrorMessage("No player matched your query '{0}'", playerstring);
                                }
                                else if (players.Count > 1)
                                {
                                    TShock.Utils.SendMultipleMatchError(args.Player, players.Select(p => p.Name));
                                }

                                TSPlayer.All.SendWarningMessage(args.Player.Name + " has started a vote to mute " + plyr.Name);
                                TSPlayer.All.SendSuccessMessage("[VoteMute] To vote type in /votekick <voteyes/voteno>");
                                VoteMuteRunning = true;
                                poll.votedplayer = plyr;
                                _timers.Start();
                            }
                            else if (VoteMuteRunning || VoteKickRunning || VoteBanRunning)
                            {
                                args.Player.SendErrorMessage("A player has already started a vote on " + poll.votedplayer.Name);
                            }
                            else
                            {
                                args.Player.SendErrorMessage("Error! Please use /votekick mute <playername>");
                            }
                        }
                        else
                        {
                            args.Player.SendErrorMessage("There is not enough players to enable a vote! {0}/{1} need to be on the server.", TShock.Utils.ActivePlayers(), config.AmountofPlayersForVotesToTakeEffect);
                        }
                    }
                        else
                        {
                            args.Player.SendErrorMessage("The vote mute feature has been turned off by the server owner.");
                        }
                    break;

                case "ban":
                    if (config.CanPlayersVoteBan)
                    {
                        if (TShock.Utils.ActivePlayers() >= Votekick.config.AmountofPlayersForVotesToTakeEffect)
                        {
                            if (args.Parameters.Count > 1 && !VoteKickRunning && !VoteMuteRunning && !VoteBanRunning)
                            {
                                string playerstring = String.Join(" ", args.Parameters.GetRange(1, args.Parameters.Count - 1));
                                var players = TShock.Utils.FindPlayer(playerstring);
                                var plyr = players[0];
                                if (players.Count == 0)
                                {
                                    args.Player.SendErrorMessage("No player matched your query '{0}'", playerstring);
                                }
                                else if (players.Count > 1)
                                {
                                    TShock.Utils.SendMultipleMatchError(args.Player, players.Select(p => p.Name));
                                }

                                TSPlayer.All.SendWarningMessage(args.Player.Name + " has started a vote to ban " + plyr.Name + " for {0} days.", config.BanTimeInDays);
                                TSPlayer.All.SendSuccessMessage("[VoteBan] To vote type in /votekick <voteyes/voteno>");
                                VoteBanRunning = true;
                                poll.votedplayer = plyr;
                                _timers.Start();
                            }
                            else if (VoteMuteRunning || VoteKickRunning || VoteBanRunning)
                            {
                                args.Player.SendErrorMessage("A player has already started a vote on " + poll.votedplayer.Name);
                            }
                            else
                            {
                                args.Player.SendErrorMessage("Error! Please use /votekick ban <playername>");
                            }
                        }
                        else
                        {
                            args.Player.SendErrorMessage("There is not enough players to enable a vote! {0}/{1} need to be on the server.", TShock.Utils.ActivePlayers(), config.AmountofPlayersForVotesToTakeEffect);
                        }
                    }
                        else
                        {
                            args.Player.SendErrorMessage("The vote ban feature has been turned off by the server owner.");
                        }
                    break;

                case "info":
                    if (VoteKickRunning)
                    {
                        args.Player.SendInfoMessage("Total Players: {0} Player to be kicked: {1}, Votes Yes: {2}, Votes No: {3}", TShock.Utils.ActivePlayers(), poll.votedplayer.Name, poll.votedyes.Count, poll.votedno.Count);
                    }
                    if (VoteMuteRunning)
                    {
                        args.Player.SendInfoMessage("Total Players: {0} Player to be muted: {1}, Votes Yes: {2}, Votes No: {3}", TShock.Utils.ActivePlayers(), poll.votedplayer.Name, poll.votedyes.Count, poll.votedno.Count);
                    }
                    if (VoteBanRunning)
                    {
                        args.Player.SendInfoMessage("Total Players: {0} Player to be ban: {1}, Votes Yes: {2}, Votes No: {3}", TShock.Utils.ActivePlayers(), poll.votedplayer.Name, poll.votedyes.Count, poll.votedno.Count);
                    }
                    else
                        args.Player.SendErrorMessage("There is no vote running at this time.");
                    break;

                case "cancel":
                    if (args.Player.Group.HasPermission("caw.cancelvotekick"))
                    {
                        if (VoteKickRunning || VoteMuteRunning || VoteBanRunning)
                        {
                            poll.voters.Clear();
                            poll.votedno.Clear();
                            poll.votedyes.Clear();
                            poll.votedplayer = null;
                            VoteKickRunning = false;
                            VoteMuteRunning = false;
                            VoteBanRunning = false;
                            Votekick._timers.VKTimer.Stop();
                            Votekick._timers.VKTimerNotify.Stop();
                            TSPlayer.All.SendInfoMessage(args.Player.Name + " has canceled the vote against " + poll.votedplayer.Name);
                        }
                        else
                        {
                            args.Player.SendErrorMessage("There is no vote running that you can cancel.");
                        }
                    }
                    else
                    {
                        args.Player.SendErrorMessage("You do not have permission to use that command!");
                    }
                    break;
            }
        }

        public static Config config;
        private static void CreateConfig()
        {
            string filepath = Path.Combine(TShock.SavePath, "Votekick.json");
            try
            {
                using (var stream = new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.Write))
                {
                    using (var sr = new StreamWriter(stream))
                    {
                        config = new Config();
                        var configString = JsonConvert.SerializeObject(config, Formatting.Indented);
                        sr.Write(configString);
                    }
                    stream.Close();
                }
            }
            catch (Exception ex)
            {
                Log.ConsoleError(ex.Message);
            }
        }

        private static bool ReadConfig()
        {
            string filepath = Path.Combine(TShock.SavePath, "Votekick.json");
            try
            {
                if (File.Exists(filepath))
                {
                    using (var stream = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        using (var sr = new StreamReader(stream))
                        {
                            var configString = sr.ReadToEnd();
                            config = JsonConvert.DeserializeObject<Config>(configString);
                        }
                        stream.Close();
                    }
                    return true;
                }
                else
                {
                    Log.ConsoleError("Votekick config not found. Creating new one...");
                    CreateConfig();
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.ConsoleError(ex.Message);
            }
            return false;
        }

        public class Config
        {
            public double PercentofPlayersVoteYesToKick = 75;
            public double PercentofPlayersVoteYesToMute = 75;
            public double PercentofPlayersVoteYesToBan = 75;
            public int AmountofPlayersForVotesToTakeEffect = 2;
            public string seconds = "Vote time below is in seconds.";
            public double VoteTime = 10;
            public int BanTimeInDays = 2;
            public bool CanPlayersVoteKick = true;
            public string KickMessage = "You have been vote kicked from the server.";
            public bool CanPlayersVoteMute = true;
            public string MuteMessage = "You have been muted by a server vote.";
            public bool CanPlayersVoteBan = true;
            public string BanMessage = "Ban by vote from the server.";

        }

        private void Reload_Config(CommandArgs args)
        {
            if (ReadConfig())
            {
                args.Player.SendMessage("Votekick config reloaded sucessfully.", Color.Yellow);
            }
            else
            {
                args.Player.SendErrorMessage("Votekick config reloaded unsucessfully. Check logs for details.");
            }
        }
    }
}
