using System;
using System.Collections.Generic;
using System.Linq;

namespace TI15SwissSimulator
{
    class Team
    {
        public string Name { get; set; }
        public string Group { get; set; }          // "A" or "B"
        public int Rating { get; set; }             // Editable skill rating (higher = stronger)
        public int Wins { get; set; } = 0;
        public int Losses { get; set; } = 0;
        public HashSet<string> Opponents { get; set; } = new HashSet<string>();
        public string Status { get; set; } = "Active"; // Active, Qualified, Eliminated

        public string Record => $"{Wins}-{Losses}";
    }

    class Program
    {
        static readonly Random rng = new Random();

        // ===================== CONFIG =====================
        // Edit these to change the simulation rules.
        const int WINS_TO_QUALIFY = 4;
        const int LOSSES_TO_ELIMINATE = 4;
        const int GROUP_LOCK_ROUNDS = 3; // rounds 1-3 stay within own hidden group
        const int MAX_ROUNDS = 5;        // the Swiss stage is hard-capped at 5 rounds;
                                          // teams stuck at 3-2 / 2-3 move to a separate decider stage
        // ====================================================

        // Round 1 is not randomly paired -- it's the officially published
        // schedule. All 8 matchups are within the same hidden group, which
        // matches the group-lock rule for the first few rounds.
        static readonly List<Tuple<string, string>> ROUND1_FIXED_PAIRINGS = new List<Tuple<string, string>>
        {
            Tuple.Create("Team Falcons", "LGD Gaming"),
            Tuple.Create("Iron Wing", "Nigma Galaxy"),
            Tuple.Create("BetBoom", "OG"),
            Tuple.Create("Parivision", "Team Resilience"),
            Tuple.Create("Team Spirit", "Xtreme Gaming"),
            Tuple.Create("Team Liquid", "Vici Gaming"),
            Tuple.Create("Aurora Gaming", "Gamerlegion"),
            Tuple.Create("Team Yandex", "Huligani"),
        };

        static void Main(string[] args)
        {
            List<Team> teams = InitializeTeams();

            Console.WriteLine("=================================================");
            Console.WriteLine("   TI15 SWISS STAGE SIMULATION");
            Console.WriteLine($"   Qualify at {WINS_TO_QUALIFY} wins | Eliminate at {LOSSES_TO_ELIMINATE} losses");
            Console.WriteLine($"   Groups locked for rounds 1-{GROUP_LOCK_ROUNDS}, then open bracket");
            Console.WriteLine("=================================================\n");

            int round = 1;
            while (round <= MAX_ROUNDS && teams.Any(t => t.Status == "Active"))
            {
                var activeTeams = teams.Where(t => t.Status == "Active").ToList();
                if (activeTeams.Count == 0) break;

                Console.WriteLine($"--- ROUND {round} ---");

                bool groupLocked = round <= GROUP_LOCK_ROUNDS;
                List<Tuple<Team, Team>> pairings = round == 1
                    ? GetRound1Pairings(activeTeams)
                    : GeneratePairings(activeTeams, groupLocked);

                foreach (var pair in pairings)
                {
                    Team teamA = pair.Item1;
                    Team teamB = pair.Item2;

                    if (teamB == null)
                    {
                        Console.WriteLine($"  {teamA.Name} receives a bye this round.");
                        continue;
                    }

                    Team winner = SimulateMatch(teamA, teamB);
                    Team loser = winner == teamA ? teamB : teamA;

                    winner.Wins++;
                    loser.Losses++;
                    teamA.Opponents.Add(teamB.Name);
                    teamB.Opponents.Add(teamA.Name);

                    Console.WriteLine($"  {teamA.Name,-16} vs {teamB.Name,-16} -> Winner: {winner.Name} ({winner.Record})");

                    if (winner.Wins >= WINS_TO_QUALIFY) winner.Status = "Qualified";
                    if (loser.Losses >= LOSSES_TO_ELIMINATE) loser.Status = "Eliminated";
                }

                Console.WriteLine();
                PrintStandings(teams);

                round++;
            }

            RunEliminationRound(teams);

            PrintFinalResults(teams);
        }

        // ------------------------------------------------------------------
        // After 5 rounds, exactly five teams sit at 3-2 and five at 2-3.
        // Each 3-2 team is cross-paired against a 2-3 team: winner qualifies
        // for playoffs, loser is eliminated. This is the real TI Swiss
        // decider step that produces the final 8-qualify / 8-eliminate split.
        // ------------------------------------------------------------------
        static void RunEliminationRound(List<Team> teams)
        {
            var upperMidTable = teams.Where(t => t.Status == "Active" && t.Wins == 3 && t.Losses == 2)
                .OrderBy(t => rng.Next()).ToList();
            var lowerMidTable = teams.Where(t => t.Status == "Active" && t.Wins == 2 && t.Losses == 3)
                .OrderBy(t => rng.Next()).ToList();

            if (upperMidTable.Count == 0 && lowerMidTable.Count == 0) return;

            Console.WriteLine("--- ELIMINATION ROUND (3-2 vs 2-3 deciders) ---");

            int matchCount = Math.Min(upperMidTable.Count, lowerMidTable.Count);
            for (int i = 0; i < matchCount; i++)
            {
                Team teamA = upperMidTable[i];
                Team teamB = lowerMidTable[i];

                Team winner = SimulateMatch(teamA, teamB);
                Team loser = winner == teamA ? teamB : teamA;

                winner.Wins++;
                loser.Losses++;
                teamA.Opponents.Add(teamB.Name);
                teamB.Opponents.Add(teamA.Name);

                winner.Status = "Qualified";
                loser.Status = "Eliminated";

                Console.WriteLine($"  {teamA.Name,-16} vs {teamB.Name,-16} -> Winner: {winner.Name} ({winner.Record}) -- {winner.Name} QUALIFIES, {loser.Name} ELIMINATED");
            }

            Console.WriteLine();
            PrintStandings(teams);
        }

        // ------------------------------------------------------------------
        // Team setup. Ratings below are placeholders only -- edit freely to
        // reflect your own power rankings before running the simulation.
        // ------------------------------------------------------------------
        static List<Team> InitializeTeams()
        {
            return new List<Team>
            {
                // ===== GROUP A =====
                new Team { Name = "Parivision",      Group = "A", Rating = 16 },
                new Team { Name = "Nigma Galaxy",    Group = "A", Rating = 10 },
                new Team { Name = "Team Falcons",    Group = "A", Rating = 14 },
                new Team { Name = "OG",              Group = "A", Rating = 10 },
                new Team { Name = "BetBoom",         Group = "A", Rating = 14 },
                new Team { Name = "LGD Gaming",      Group = "A", Rating = 10 },
                new Team { Name = "Iron Wing",       Group = "A", Rating = 13 },
                new Team { Name = "Team Resilience", Group = "A", Rating = 7 },

                // ===== GROUP B =====
                new Team { Name = "Team Yandex",     Group = "B", Rating = 15 },
                new Team { Name = "Xtreme Gaming",   Group = "B", Rating = 12 },
                new Team { Name = "Team Liquid",     Group = "B", Rating = 12 },
                new Team { Name = "Vici Gaming",     Group = "B", Rating = 12 },
                new Team { Name = "Aurora Gaming",   Group = "B", Rating = 13 },
                new Team { Name = "Gamerlegion",     Group = "B", Rating = 4 },
                new Team { Name = "Team Spirit",     Group = "B", Rating = 14 },
                new Team { Name = "Huligani",        Group = "B", Rating = 3 },
            };
        }

        // ------------------------------------------------------------------
        // Pairing logic: group-locked for the first few rounds, then an
        // open Swiss field. Teams are paired against the closest available
        // record, avoiding rematches whenever possible.
        // ------------------------------------------------------------------
        static List<Tuple<Team, Team>> GetRound1Pairings(List<Team> teams)
        {
            var result = new List<Tuple<Team, Team>>();
            foreach (var p in ROUND1_FIXED_PAIRINGS)
            {
                Team a = teams.First(t => t.Name == p.Item1);
                Team b = teams.First(t => t.Name == p.Item2);
                result.Add(Tuple.Create(a, b));
            }
            return result;
        }

        static List<Tuple<Team, Team>> GeneratePairings(List<Team> activeTeams, bool groupLocked)
        {
            var pairings = new List<Tuple<Team, Team>>();

            if (groupLocked)
            {
                var groupA = activeTeams.Where(t => t.Group == "A").ToList();
                var groupB = activeTeams.Where(t => t.Group == "B").ToList();
                pairings.AddRange(PairPool(groupA));
                pairings.AddRange(PairPool(groupB));
            }
            else
            {
                pairings.AddRange(PairPool(activeTeams));
            }

            return pairings;
        }

        static List<Tuple<Team, Team>> PairPool(List<Team> pool)
        {
            var result = new List<Tuple<Team, Team>>();

            // Group strictly by identical current record (wins-losses differential).
            // Pairing NEVER crosses into a different score group -- if two teams in
            // the same group have already played, we force a rematch inside that
            // group rather than reaching into a neighboring group, which would
            // break the clean bracket shape (and can cause byes/odd counts).
            var scoreGroups = pool
                .GroupBy(t => t.Wins - t.Losses)
                .OrderByDescending(g => g.Key)
                .ToList();

            foreach (var group in scoreGroups)
            {
                var unpaired = group.OrderBy(t => rng.Next()).ToList();

                while (unpaired.Count > 0)
                {
                    Team teamA = unpaired[0];
                    unpaired.RemoveAt(0);

                    if (unpaired.Count == 0)
                    {
                        // Only happens if a score group has an odd number of teams,
                        // which shouldn't occur under the normal bracket shape.
                        result.Add(Tuple.Create<Team, Team>(teamA, null));
                        break;
                    }

                    int idx = unpaired.FindIndex(t => !teamA.Opponents.Contains(t.Name));
                    if (idx == -1) idx = 0; // forced rematch, still within this score group

                    Team teamB = unpaired[idx];
                    unpaired.RemoveAt(idx);
                    result.Add(Tuple.Create(teamA, teamB));
                }
            }

            return result;
        }

        // ------------------------------------------------------------------
        // Match outcome: the higher-rated team always wins. Only when both
        // teams share the exact same rating is the result a coin flip.
        // ------------------------------------------------------------------
        static Team SimulateMatch(Team a, Team b)
        {
            if (a.Rating == b.Rating)
                return rng.NextDouble() < 0.5 ? a : b;

            return a.Rating > b.Rating ? a : b;
        }

        static void PrintStandings(List<Team> teams)
        {
            Console.WriteLine("  Standings:");
            foreach (var t in teams.OrderByDescending(x => x.Wins - x.Losses).ThenBy(x => x.Name))
            {
                Console.WriteLine($"    {t.Name,-18} {t.Record,-6} [{t.Status}]");
            }
            Console.WriteLine();
        }

        static void PrintFinalResults(List<Team> teams)
        {
            Console.WriteLine("=================================================");
            Console.WriteLine("   FINAL RESULTS");
            Console.WriteLine("=================================================\n");

            var qualified = teams.Where(t => t.Status == "Qualified")
                .OrderByDescending(t => t.Wins).ThenBy(t => t.Losses).ToList();
            var eliminated = teams.Where(t => t.Status == "Eliminated")
                .OrderByDescending(t => t.Wins).ThenBy(t => t.Losses).ToList();
            var stillActive = teams.Where(t => t.Status == "Active").ToList();

            Console.WriteLine($"QUALIFIED FOR PLAYOFFS ({qualified.Count}):");
            foreach (var t in qualified)
                Console.WriteLine($"  {t.Name,-18} {t.Record,-6} (Group {t.Group})");

            Console.WriteLine($"\nELIMINATED ({eliminated.Count}):");
            foreach (var t in eliminated)
                Console.WriteLine($"  {t.Name,-18} {t.Record,-6} (Group {t.Group})");

            if (stillActive.Count > 0)
            {
                Console.WriteLine($"\nMID-TABLE / DECIDER ROUND NEEDED ({stillActive.Count}) -- stuck at 3-2 or 2-3 after 5 rounds:");
                foreach (var t in stillActive)
                    Console.WriteLine($"  {t.Name,-18} {t.Record,-6} (Group {t.Group})");
            }
        }
    }
}