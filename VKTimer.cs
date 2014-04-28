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
        private Timer VKTimer;
        private Timer VKTimerNotify;

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
                    TShock.Utils.Kick(Votekick.poll.playertobekicked, Votekick.config.KickMessage, true, false);
                    Votekick.VoteKickRunning = false;
                    Votekick.poll.voters.Clear();
                    Votekick.poll.votedno.Clear();
                    Votekick.poll.votedyes.Clear();
                    Votekick.poll.playertobekicked = null;
                }

                else
                {
                    TSPlayer.All.SendInfoMessage("The votekick on " + Votekick.poll.playertobekicked.Name + " has failed.");
                    Votekick.VoteKickRunning = false;
                    Votekick.poll.voters.Clear();
                    Votekick.poll.votedno.Clear();
                    Votekick.poll.votedyes.Clear();
                    Votekick.poll.playertobekicked = null;
                }
            }
        }
        private void NotifyTimer(object sender, ElapsedEventArgs args)
        {
            if (Votekick.VoteKickRunning)
            {
                TSPlayer.All.SendSuccessMessage("Votekick ending in {0} seconds to kick {1} ", (Votekick.config.VoteTime / 2), Votekick.poll.playertobekicked.Name);
            }
        }
    }
}
