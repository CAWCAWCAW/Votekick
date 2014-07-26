﻿using System;
using System.IO;
using System.Linq;
﻿using System.Collections.Generic;
using TShockAPI;
﻿using Terraria;
using Newtonsoft.Json;
﻿using TerrariaApi.Server;
﻿using TShockAPI.DB;

namespace Voting
{
    public enum VoteType
    {
        kick = 0,
        mute,
        ban
    }

    public class Vote
    {
        public readonly List<TSPlayer> voters;
        public readonly List<TSPlayer> votedyes;
        public readonly List<TSPlayer> votedno;
        public DateTime timestarted;
        public User votedplayer;
        public TSPlayer votestarter;
        public VoteType voteType;
        public bool active;

        public Vote()
        {
            voters = new List<TSPlayer>();
            votedyes = new List<TSPlayer>();
            votedno = new List<TSPlayer>();
            active = true;
        }

        public void Kill(string reason = null)
        {
            var msg = reason ?? string.Format("Vote to {0} {1} has ended", voteType, votedplayer.Name);
            TShock.Utils.Broadcast(msg,
                Convert.ToByte(TShock.Config.BroadcastRGB[0]),
                Convert.ToByte(TShock.Config.BroadcastRGB[1]),
                Convert.ToByte(TShock.Config.BroadcastRGB[2]));

            Voting.Votes.Remove(votedplayer.Name);
            voters.Clear();
            votedyes.Clear();
            votedno.Clear();
            votedplayer = null;
            votestarter = null;
            active = false;
        }
    }

    #region Config

    public class Config
    {
        public double PercentofPlayersVoteYesToKick = 75;
        public double PercentofPlayersVoteYesToMute = 75;
        public double PercentofPlayersVoteYesToBan = 75;
        public int AmountofPlayersForVotesToTakeEffect = 2;
        public double VoteTimeInSeconds = 30;
        public int VoteBroadCastInterval = 10;
        public int BanTimeInMinutes = 1680;
        public bool CanPlayersVoteKick = true;
        public string KickMessage = "Kicked by server vote.";
        public bool CanPlayersVoteMute = true;
        public string MuteMessage = "Muted by server vote.";
        public bool CanPlayersVoteBan = true;
        public string BanMessage = "Banned by server vote.";

        public bool ReadConfig()
        {
            var filepath = Path.Combine(TShock.SavePath, "Votekick.json");
            try
            {
                if (File.Exists(filepath))
                {
                    using (var stream = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        using (var sr = new StreamReader(stream))
                        {
                            var configString = sr.ReadToEnd();
                            Voting.config = JsonConvert.DeserializeObject<Config>(configString);
                        }
                        stream.Close();
                    }
                    return true;
                }

                Log.ConsoleError("Voting config not found. Creating new one...");
                CreateConfig();
                return false;
            }
            catch (Exception ex)
            {
                Log.ConsoleError(ex.Message);
            }
            return false;
        }

        public void CreateConfig()
        {
            var filepath = Path.Combine(TShock.SavePath, "Votekick.json");
            try
            {
                using (var stream = new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.Write))
                {
                    using (var sr = new StreamWriter(stream))
                    {
                        var configString = JsonConvert.SerializeObject(Voting.config, Formatting.Indented);
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
    }

    #endregion

    [ApiVersion(1, 16)]
    public class Voting : TerrariaPlugin
    {
        private VotingTimer _timers;
        public static Config config = new Config();

        public override Version Version
        {
            get { return new Version(1, 3); }
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

        public Voting(Main game)
            : base(game)
        {
        }

        public override void Initialize()
        {
            config.ReadConfig();
            _timers = new VotingTimer();
            _timers.Start();

            Commands.ChatCommands.Add(new Command("caw.vote", Vote, "voting", "v"));
            Commands.ChatCommands.Add(new Command("caw.reloadvote", Reload_Config, "reloadvoting", "rv"));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _timers.Stop();
            base.Dispose(disposing);
        }

        public static readonly Dictionary<string, Vote> Votes = new Dictionary<string, Vote>();

        private void Vote(CommandArgs args)
        {
            if (args.Parameters.Count < 2)
            {
                args.Player.SendErrorMessage("Invalid syntax!" +
                                             " Proper syntax: /voting [kick/mute/ban/yes/no/info/cancel] [vote (name or index)]");
                return;
            }

            var voteIndex = args.Parameters[1];
            Vote vote;

            switch (args.Parameters[0])
            {
                #region Yes

                case "yes":
                    vote = Votes.GetVote(voteIndex);
                    if (vote == null)
                    {
                        args.Player.SendErrorMessage("Vote not found");
                        return;
                    }
                    if (!vote.voters.Contains(args.Player) && vote.active)
                    {
                        args.Player.SendSuccessMessage("You have voted yes to {0} {1}", vote.voteType, vote.votedplayer.Name);
                        vote.voters.Add(args.Player);
                        vote.votedyes.Add(args.Player);
                        return;
                    }
                    if (vote.voters.Contains(args.Player))
                    {
                        args.Player.SendErrorMessage("You have already voted for this vote!");
                        return;
                    }

                    args.Player.SendInfoMessage("A vote is not running at this time.");
                    return;

                    #endregion

                #region No

                case "no":
                    vote = Votes.GetVote(voteIndex);
                    if (vote == null)
                    {
                        args.Player.SendErrorMessage("Vote not found!");
                        return;
                    }
                    if (!vote.voters.Contains(args.Player) && vote.active)
                    {
                        args.Player.SendSuccessMessage("You have voted no to {0} {1}", vote.voteType, vote.votedplayer.Name);
                        vote.voters.Add(args.Player);
                        vote.votedno.Add(args.Player);
                        return;
                    }
                    if (vote.voters.Contains(args.Player))
                    {
                        args.Player.SendErrorMessage("You have already voted for this vote!");
                        return;
                    }

                    args.Player.SendInfoMessage("A vote is not running at this time.");
                    return;

                    #endregion

                #region Kick

                case "kick":
                    if (config.CanPlayersVoteKick)
                    {
                        if (TShock.Utils.ActivePlayers() >= config.AmountofPlayersForVotesToTakeEffect)
                        {
                            if (args.Parameters.Count > 1)
                            {
                                var plStr = args.Parameters.Count > 3
                                    ? String.Join(" ",
                                        args.Parameters.GetRange(1, args.Parameters.Count - 2)).ToLower()
                                    : String.Join(" ",
                                        args.Parameters.GetRange(1, args.Parameters.Count - 1)).ToLower();

                                var players = TShock.Utils.FindPlayer(plStr);

                                if (players.Count == 0)
                                {
                                    args.Player.SendErrorMessage("No users matched your query '{0}'", plStr);
                                    return;
                                }

                                if (players.Count > 1)
                                {
                                    TShock.Utils.SendMultipleMatchError(args.Player, players.Select(p => p.Name));
                                    return;
                                }

                                var users = new List<User>();

                                foreach (
                                    var u in TShock.Users.GetUsers().FindAll(u => u.Name.ToLower().StartsWith(plStr)))
                                {
                                    if (u.Name.ToLower() == plStr)
                                    {
                                        users = new List<User> {u};
                                        break;
                                    }
                                    if (u.Name.ToLower().StartsWith(plStr))
                                        users.Add(u);
                                }

                                if (users.Count == 0)
                                    args.Player.SendErrorMessage("No users matched your query '{0}'", plStr);

                                else if (users.Count > 1)
                                    TShock.Utils.SendMultipleMatchError(args.Player, users.Select(p => p.Name));

                                var user = users[0];

                                if (Votes.ContainsKey(user.Name))
                                {
                                    args.Player.SendErrorMessage("Someone has already started a vote to {0} {1}",
                                        Votes[user.Name].voteType.ToString(), user.Name);
                                    return;
                                }


                                vote = new Vote
                                {
                                    active = true,
                                    votedplayer = user,
                                    voteType = VoteType.kick,
                                    timestarted = DateTime.Now,
                                    votestarter = args.Player
                                };
                                vote.votedyes.Add(args.Player);
                                vote.voters.Add(args.Player);

                                Votes.Add(user.Name, vote);

                                TShock.Utils.Broadcast(string.Format("{0} has started a vote to kick {1}",
                                    args.Player.Name, user.Name),
                                    Convert.ToByte(TShock.Config.BroadcastRGB[0]),
                                    Convert.ToByte(TShock.Config.BroadcastRGB[1]),
                                    Convert.ToByte(TShock.Config.BroadcastRGB[2]));

                                _timers.Start();
                            }
                            else
                                args.Player.SendErrorMessage("Error! Please use /voting kick <playername>");
                        }
                        else
                            args.Player.SendErrorMessage(
                                "There are not enough players online to start a vote! {0} more players required.",
                                config.AmountofPlayersForVotesToTakeEffect - TShock.Utils.ActivePlayers());
                    }
                    else
                        args.Player.SendErrorMessage("Vote kicking has been disabled by the server owner.");

                    break;
                    
                    #endregion

                #region Mute

                case "mute":
                    if (config.CanPlayersVoteMute)
                    {
                        if (TShock.Utils.ActivePlayers() >= config.AmountofPlayersForVotesToTakeEffect)
                        {
                            if (args.Parameters.Count > 1)
                            {
                                var plStr = args.Parameters.Count > 3
                                    ? String.Join(" ",
                                        args.Parameters.GetRange(1, args.Parameters.Count - 2)).ToLower()
                                    : String.Join(" ",
                                        args.Parameters.GetRange(1, args.Parameters.Count - 1)).ToLower();

                                var players = TShock.Utils.FindPlayer(plStr);

                                if (players.Count == 0)
                                {
                                    args.Player.SendErrorMessage("No users matched your query '{0}'", plStr);
                                    return;
                                }

                                if (players.Count > 1)
                                {
                                    TShock.Utils.SendMultipleMatchError(args.Player, players.Select(p => p.Name));
                                    return;
                                }

                                var users = new List<User>();

                                foreach (
                                    var u in TShock.Users.GetUsers().FindAll(u => u.Name.ToLower().StartsWith(plStr)))
                                {
                                    if (u.Name.ToLower() == plStr)
                                    {
                                        users = new List<User> { u };
                                        break;
                                    }
                                    if (u.Name.ToLower().StartsWith(plStr))
                                        users.Add(u);
                                }

                                if (users.Count == 0)
                                    args.Player.SendErrorMessage("No users matched your query '{0}'", plStr);

                                else if (users.Count > 1)
                                    TShock.Utils.SendMultipleMatchError(args.Player, users.Select(p => p.Name));

                                var user = users[0];

                                if (Votes.ContainsKey(user.Name))
                                {
                                    args.Player.SendErrorMessage("Someone has already started a vote to {0} {1}",
                                        Votes[user.Name].voteType.ToString(), user.Name);
                                    return;
                                }


                                vote = new Vote
                                {
                                    active = true,
                                    votedplayer = user,
                                    voteType = VoteType.mute,
                                    timestarted = DateTime.Now,
                                    votestarter = args.Player
                                };
                                vote.votedyes.Add(args.Player);
                                vote.voters.Add(args.Player);

                                Votes.Add(user.Name, vote);

                                TShock.Utils.Broadcast(string.Format("{0} has started a vote to mute {1}",
                                    args.Player.Name, user.Name),
                                    Convert.ToByte(TShock.Config.BroadcastRGB[0]),
                                    Convert.ToByte(TShock.Config.BroadcastRGB[1]),
                                    Convert.ToByte(TShock.Config.BroadcastRGB[2]));

                                _timers.Start();
                            }
                            else
                                args.Player.SendErrorMessage("Error! Please use /voting mute <playername>");
                        }
                        else
                            args.Player.SendErrorMessage(
                                "There are not enough players online to start a vote! {0} more players required.",
                                config.AmountofPlayersForVotesToTakeEffect - TShock.Utils.ActivePlayers());
                    }
                    else
                        args.Player.SendErrorMessage("Vote muting has been disabled by the server owner.");

                    break;

                #endregion

                #region Ban

                case "ban":
                    if (config.CanPlayersVoteBan)
                    {
                        if (TShock.Utils.ActivePlayers() >= config.AmountofPlayersForVotesToTakeEffect)
                        {
                            if (args.Parameters.Count > 1)
                            {
                                var plStr = args.Parameters.Count > 3
                                    ? String.Join(" ",
                                        args.Parameters.GetRange(1, args.Parameters.Count - 2)).ToLower()
                                    : String.Join(" ",
                                        args.Parameters.GetRange(1, args.Parameters.Count - 1)).ToLower();

                                var players = TShock.Utils.FindPlayer(plStr);

                                if (players.Count == 0)
                                {
                                    args.Player.SendErrorMessage("No users matched your query '{0}'", plStr);
                                    return;
                                }

                                if (players.Count > 1)
                                {
                                    TShock.Utils.SendMultipleMatchError(args.Player, players.Select(p => p.Name));
                                    return;
                                }

                                var users = new List<User>();

                                foreach (
                                    var u in TShock.Users.GetUsers().FindAll(u => u.Name.ToLower().StartsWith(plStr)))
                                {
                                    if (u.Name.ToLower() == plStr)
                                    {
                                        users = new List<User> { u };
                                        break;
                                    }
                                    if (u.Name.ToLower().StartsWith(plStr))
                                        users.Add(u);
                                }

                                if (users.Count == 0)
                                    args.Player.SendErrorMessage("No users matched your query '{0}'", plStr);

                                else if (users.Count > 1)
                                    TShock.Utils.SendMultipleMatchError(args.Player, users.Select(p => p.Name));

                                var user = users[0];

                                if (Votes.ContainsKey(user.Name))
                                {
                                    args.Player.SendErrorMessage("Someone has already started a vote to {0} {1}",
                                        Votes[user.Name].voteType.ToString(), user.Name);
                                    return;
                                }


                                vote = new Vote
                                {
                                    active = true,
                                    votedplayer = user,
                                    voteType = VoteType.ban,
                                    timestarted = DateTime.Now,
                                    votestarter = args.Player
                                };
                                vote.votedyes.Add(args.Player);
                                vote.voters.Add(args.Player);

                                Votes.Add(user.Name, vote);

                                TShock.Utils.Broadcast(string.Format("{0} has started a vote to ban {1}",
                                    args.Player.Name, user.Name),
                                    Convert.ToByte(TShock.Config.BroadcastRGB[0]),
                                    Convert.ToByte(TShock.Config.BroadcastRGB[1]),
                                    Convert.ToByte(TShock.Config.BroadcastRGB[2]));

                                _timers.Start();
                            }
                            else
                                args.Player.SendErrorMessage("Error! Please use /voting ban <playername>");
                        }
                        else
                            args.Player.SendErrorMessage(
                                "There are not enough players online to start a vote! {0} more players required.",
                                config.AmountofPlayersForVotesToTakeEffect - TShock.Utils.ActivePlayers());
                    }
                    else
                        args.Player.SendErrorMessage("Vote banning has been disabled by the server owner.");

                    break;

                #endregion

                #region Info

                case "info":
                    vote = Votes.GetVote(voteIndex);
                    if (vote == null)
                    {
                        args.Player.SendErrorMessage("Vote not found!");
                        return;
                    }

                    args.Player.SendInfoMessage("Vote {0} targetting {1}",
                        vote.voteType.ToString(), vote.votedplayer.Name);
                    args.Player.SendInfoMessage("Players voted: {0}. Yes:No - {1}:{2}",
                        vote.voters.Count, vote.votedyes.Count, vote.votedno.Count);

                    break;

                    #endregion

                #region Cancel

                case "cancel":
                    if (args.Player.Group.HasPermission("caw.cancelvotekick"))
                    {
                        vote = Votes.GetVote(voteIndex);
                        if (vote == null)
                        {
                            args.Player.SendErrorMessage("Vote not found!");
                            return;
                        }
                        var reason = args.Parameters.Count > 3
                            ? string.Join(" ", args.Parameters.GetRange(2, args.Parameters.Count))
                            : "cancelled";
                        vote.Kill(string.Format("Vote to {0} {1} ended by {2} ({3})",
                            vote.voteType, vote.votedplayer.Name, args.Player.Name, reason));
                    }
                    else
                        args.Player.SendErrorMessage("You do not have permission to use that command!");
                    break;

                    #endregion
            }
        }


        private void Reload_Config(CommandArgs args)
        {
            if (config.ReadConfig())
                args.Player.SendSuccessMessage("Voting config reloaded sucessfully.");
            else
                args.Player.SendErrorMessage("Voting config failed to reload. Check logs for details.");
        }
    }

    public static class Extension
    {
        public static Vote GetVote(this Dictionary<string, Vote> dictionary, object index)
        {
            Vote ret = null;

            if (index is int)
                if ((int) index < dictionary.Count)
                    ret = dictionary[dictionary.Keys.ToList()[(int) index]];

            if (index is string)
                if (dictionary.ContainsKey(index.ToString()))
                    ret = dictionary[index.ToString()];

            return ret;
        }
    }
}
