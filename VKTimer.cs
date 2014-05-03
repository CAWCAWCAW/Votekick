using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using System.Timers;

namespace VoteKick
{
    public class VoteKickTimer
    {
        public Timer VKTimer;
        public Timer VKTimerNotify;

        public VoteKickTimer()
        {
            VKTimer = new Timer(Votekick.config.VoteTime * 1000);
            VKTimerNotify = new Timer((Votekick.config.VoteTime / 2) * 1000);
        }

        public void Start()
        {
            VKTimer.Enabled = true;
            VKTimer.Elapsed += UpdateTimer;

            VKTimerNotify.Enabled = true;
            VKTimerNotify.Elapsed += NotifyTimer;
        }

        private void UpdateTimer(object sender, ElapsedEventArgs args)
        {
            if (Votekick.VoteKickRunning)
            {
                double active = TShock.Utils.ActivePlayers();
                double percentageofactive = ((active) * (Votekick.config.PercentofPlayersVoteYesToKick / 100));
                double totalvoters = Votekick.poll.voters.Count;

                    if (Votekick.poll.votedyes.Count > Votekick.poll.votedno.Count && Votekick.poll.votedyes.Count >= percentageofactive)
                    {
                        TShock.Utils.Kick(Votekick.poll.votedplayer, Votekick.config.KickMessage, true, false);
                        Votekick.VoteKickRunning = false;
                        Votekick.poll.voters.Clear();
                        Votekick.poll.votedno.Clear();
                        Votekick.poll.votedyes.Clear();
                        Votekick.poll.votedplayer = null;
                        VKTimerNotify.Stop();
                    }

                    else
                    {
                        TSPlayer.All.SendInfoMessage("[VoteKick] The votekick on " + Votekick.poll.votedplayer.Name + " has failed.");
                        Votekick.VoteKickRunning = false;
                        Votekick.poll.voters.Clear();
                        Votekick.poll.votedno.Clear();
                        Votekick.poll.votedyes.Clear();
                        Votekick.poll.votedplayer = null;
                        VKTimerNotify.Stop();
                    }
            }

            if (Votekick.VoteMuteRunning)
            {
                double active = TShock.Utils.ActivePlayers();
                double percentageofactive = ((active) * (Votekick.config.PercentofPlayersVoteYesToMute / 100));
                double totalvoters = Votekick.poll.voters.Count;

                    if (Votekick.poll.votedyes.Count > Votekick.poll.votedno.Count && Votekick.poll.votedyes.Count >= percentageofactive)
                    {
                        TSPlayer.All.SendInfoMessage("[VoteMute] The vote to mute {0} has succeeded.", Votekick.poll.votedplayer.Name);
                        Votekick.poll.votedplayer.mute = true;
                        Votekick.VoteMuteRunning = false;
                        Votekick.poll.voters.Clear();
                        Votekick.poll.votedno.Clear();
                        Votekick.poll.votedyes.Clear();
                        Votekick.poll.votedplayer = null;
                        VKTimerNotify.Stop();
                    }

                    else
                    {
                        TSPlayer.All.SendInfoMessage("[VoteMute] The vote to mute " + Votekick.poll.votedplayer.Name + " has failed.");
                        Votekick.VoteMuteRunning = false;
                        Votekick.poll.voters.Clear();
                        Votekick.poll.votedno.Clear();
                        Votekick.poll.votedyes.Clear();
                        Votekick.poll.votedplayer = null;
                        VKTimerNotify.Stop();
                    }
            }

            if (Votekick.VoteBanRunning)
            {
                double active = TShock.Utils.ActivePlayers();
                double percentageofactive = ((active) * (Votekick.config.PercentofPlayersVoteYesToBan / 100));
                double totalvoters = Votekick.poll.voters.Count;

                    if (Votekick.poll.votedyes.Count > Votekick.poll.votedno.Count && Votekick.poll.votedyes.Count >= percentageofactive)
                    {
                        TShock.Utils.Kick(Votekick.poll.votedplayer, Votekick.config.BanMessage, true, false);
                        TShock.Bans.AddBan(Votekick.poll.votedplayer.IP, Votekick.poll.votedplayer.Name, Votekick.poll.votedplayer.UUID, Votekick.config.BanMessage, false, "Server Vote", DateTime.UtcNow.AddDays(Votekick.config.BanTimeInDays).ToString("s"));
                        Votekick.VoteBanRunning = false;
                        Votekick.poll.voters.Clear();
                        Votekick.poll.votedno.Clear();
                        Votekick.poll.votedyes.Clear();
                        Votekick.poll.votedplayer = null;
                        VKTimerNotify.Stop();
                    }

                    else
                    {
                        TSPlayer.All.SendInfoMessage("[VoteBan] The vote to ban " + Votekick.poll.votedplayer.Name + " has failed.");
                        Votekick.VoteBanRunning = false;
                        Votekick.poll.voters.Clear();
                        Votekick.poll.votedno.Clear();
                        Votekick.poll.votedyes.Clear();
                        Votekick.poll.votedplayer = null;
                        VKTimerNotify.Stop();
                    }
            }
        }
        private void NotifyTimer(object sender, ElapsedEventArgs args)
        {
            if (Votekick.VoteKickRunning)
            {
                TSPlayer.All.SendSuccessMessage("The vote is ending in {0} seconds to kick {1}.", (Votekick.config.VoteTime / 2), Votekick.poll.votedplayer.Name);
                VKTimerNotify.Stop();
            }

            if (Votekick.VoteMuteRunning)
            {
                TSPlayer.All.SendSuccessMessage("The vote is ending in {0} seconds to mute {1}.", (Votekick.config.VoteTime / 2), Votekick.poll.votedplayer.Name);
                VKTimerNotify.Stop();
            }

            if (Votekick.VoteBanRunning)
            {
                TSPlayer.All.SendSuccessMessage("The vote is ending in {0} seconds to ban {1} for a time length of {2} days.", (Votekick.config.VoteTime / 2), Votekick.poll.votedplayer.Name, Votekick.config.BanTimeInDays);
                VKTimerNotify.Stop();
            }
        }
    }
}
