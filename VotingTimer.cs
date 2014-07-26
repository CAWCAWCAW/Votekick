using System;
using System.Collections.Generic;
using TShockAPI;
using System.Timers;
using Newtonsoft.Json;

namespace Voting
{
    public class VotingTimer
    {
        private readonly Timer _vTimerNotify;
        private int _ticks;

        public VotingTimer()
        {
            _vTimerNotify = new Timer(1000);
        }

        public void Start()
        {

            _vTimerNotify.Enabled = true;
            _vTimerNotify.Elapsed += NotifyTimer;
        }

        public void Stop()
        {
            _vTimerNotify.Stop();
        }

        private void NotifyTimer(object sender, ElapsedEventArgs args)
        {
            lock (Voting.Votes)
            {
                foreach (var pair in Voting.Votes)
                {
                    var vote = pair.Value;
                    var timePassed = (DateTime.Now - vote.timestarted).Seconds;
                    var timeToGo = Voting.config.VoteTimeInSeconds - timePassed;

                    if (timePassed > Voting.config.VoteTimeInSeconds)
                        DoVote(vote);

                    if (_ticks == Voting.config.VoteBroadCastInterval)
                    {
                        TShock.Utils.Broadcast(string.Format("You have {0} seconds to vote to {1} {2}!",
                            timeToGo, vote.voteType, vote.votedplayer.Name),
                            Convert.ToByte(TShock.Config.BroadcastRGB[0]),
                            Convert.ToByte(TShock.Config.BroadcastRGB[1]),
                            Convert.ToByte(TShock.Config.BroadcastRGB[2]));
                        TShock.Utils.Broadcast(string.Format("Use /voting yes {0} or /voting no {0} to vote",
                            vote.votedplayer.Name),
                            Convert.ToByte(TShock.Config.BroadcastRGB[0]),
                            Convert.ToByte(TShock.Config.BroadcastRGB[1]),
                            Convert.ToByte(TShock.Config.BroadcastRGB[2]));

                        _ticks = 0;
                    }
                    _ticks++;
                }
            }
        }

        private void DoVote(Vote vote)
        {
            var active = TShock.Utils.ActivePlayers();
            var percent = active/(double) Voting.config.AmountofPlayersForVotesToTakeEffect;

            if (vote.votedyes.Count > vote.votedno.Count && vote.votedyes.Count >= percent)
            {
                switch (vote.voteType)
                {
                    case VoteType.ban:
                        var ips = JsonConvert.DeserializeObject<List<string>>(vote.votedplayer.KnownIps);
                        var ip = ips[ips.Count - 1];
                        TShock.Bans.AddBan(ip, vote.votedplayer.Name, vote.votedplayer.UUID,
                            "vote banned", false, vote.votestarter.Name);
                        TShock.Utils.Kick(TShock.Utils.FindPlayer(vote.votedplayer.Name)[0],
                            "vote banned", true, false, vote.votestarter.Name);

                        TShock.Utils.Broadcast(string.Format("{0} was banned (Yes:No - {1}:{2})",
                            vote.votedplayer.Name, vote.votedyes.Count, vote.votedno.Count),
                            Convert.ToByte(TShock.Config.BroadcastRGB[0]),
                            Convert.ToByte(TShock.Config.BroadcastRGB[1]),
                            Convert.ToByte(TShock.Config.BroadcastRGB[2]));

                        break;
                    case VoteType.kick:
                        if (TShock.Utils.FindPlayer(vote.votedplayer.Name).Count == 1)
                            TShock.Utils.Kick(TShock.Utils.FindPlayer(vote.votedplayer.Name)[0],
                                "vote kicked", true, false, vote.votestarter.Name);

                        TShock.Utils.Broadcast(string.Format("{0} was kicked (Yes:No - {1}:{2})",
                            vote.votedplayer.Name, vote.votedyes.Count, vote.votedno.Count),
                            Convert.ToByte(TShock.Config.BroadcastRGB[0]),
                            Convert.ToByte(TShock.Config.BroadcastRGB[1]),
                            Convert.ToByte(TShock.Config.BroadcastRGB[2]));

                        break;
                    case VoteType.mute:
                    {
                        if (TShock.Utils.FindPlayer(vote.votedplayer.Name).Count == 1)
                            TShock.Utils.FindPlayer(vote.votedplayer.Name)[0].mute = true;

                        TShock.Utils.Broadcast(string.Format("{0} was muted (Yes:No - {1}:{2})",
                            vote.votedplayer.Name, vote.votedyes.Count, vote.votedno.Count),
                            Convert.ToByte(TShock.Config.BroadcastRGB[0]),
                            Convert.ToByte(TShock.Config.BroadcastRGB[1]),
                            Convert.ToByte(TShock.Config.BroadcastRGB[2]));

                        break;
                    }
                }
            }

            if (vote.votedno.Count > vote.votedyes.Count && vote.votedno.Count > percent)
            {
                switch (vote.voteType)
                {
                    case VoteType.ban:

                        TShock.Utils.Broadcast(string.Format("{0} was not banned (Yes:No - {1}:{2})",
                            vote.votedplayer.Name, vote.votedyes.Count, vote.votedno.Count),
                            Convert.ToByte(TShock.Config.BroadcastRGB[0]),
                            Convert.ToByte(TShock.Config.BroadcastRGB[1]),
                            Convert.ToByte(TShock.Config.BroadcastRGB[2]));

                        break;
                    case VoteType.kick:

                        TShock.Utils.Broadcast(string.Format("{0} was not kicked (Yes:No - {1}:{2})",
                            vote.votedplayer.Name, vote.votedyes.Count, vote.votedno.Count),
                            Convert.ToByte(TShock.Config.BroadcastRGB[0]),
                            Convert.ToByte(TShock.Config.BroadcastRGB[1]),
                            Convert.ToByte(TShock.Config.BroadcastRGB[2]));

                        break;
                    case VoteType.mute:
                    {
                        TShock.Utils.Broadcast(string.Format("{0} was not muted (Yes:No - {1}:{2})",
                            vote.votedplayer.Name, vote.votedyes.Count, vote.votedno.Count),
                            Convert.ToByte(TShock.Config.BroadcastRGB[0]),
                            Convert.ToByte(TShock.Config.BroadcastRGB[1]),
                            Convert.ToByte(TShock.Config.BroadcastRGB[2]));

                        break;
                    }
                }
            }

            if (vote.votedno.Count < percent && vote.votedyes.Count < percent)
            {
                switch (vote.voteType)
                {
                    case VoteType.ban:

                        TShock.Utils.Broadcast(string.Format("{0} was not banned (Not enough votes)",
                            vote.votedplayer.Name),
                            Convert.ToByte(TShock.Config.BroadcastRGB[0]),
                            Convert.ToByte(TShock.Config.BroadcastRGB[1]),
                            Convert.ToByte(TShock.Config.BroadcastRGB[2]));

                        break;
                    case VoteType.kick:

                        TShock.Utils.Broadcast(string.Format("{0} was not kicked (Not enough votes)",
                            vote.votedplayer.Name),
                            Convert.ToByte(TShock.Config.BroadcastRGB[0]),
                            Convert.ToByte(TShock.Config.BroadcastRGB[1]),
                            Convert.ToByte(TShock.Config.BroadcastRGB[2]));

                        break;
                    case VoteType.mute:
                        {
                            TShock.Utils.Broadcast(string.Format("{0} was not muted (Not enough votes)",
                                vote.votedplayer.Name),
                                Convert.ToByte(TShock.Config.BroadcastRGB[0]),
                                Convert.ToByte(TShock.Config.BroadcastRGB[1]),
                                Convert.ToByte(TShock.Config.BroadcastRGB[2]));

                            break;
                        }
                }
            }
            vote.Kill();
        }
    }
}
