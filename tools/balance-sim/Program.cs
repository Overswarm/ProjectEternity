using System;
using Eternity;
using UnityEngine;

class SmokeTest
{
    static int Main()
    {
        int failures = 0;
        foreach (int seed in new[] { 42, 1337, 900913 })
        {
            Console.WriteLine($"===== SEED {seed} =====");
            try { failures += RunWorld(seed) ? 0 : 1; }
            catch (Exception e)
            {
                Console.WriteLine($"CRASH: {e.GetType().Name}: {e.Message}\n{e.StackTrace}");
                failures++;
            }
        }
        Console.WriteLine(failures == 0 ? "\nALL WORLDS SURVIVED THE EONS." : $"\n{failures} WORLD(S) FAILED.");
        return failures;
    }

    static int CountWars(Simulation sim)
    {
        int n = 0;
        for (int a = 0; a < sim.Civs.Count; a++)
            for (int b = a + 1; b < sim.Civs.Count; b++)
                if (sim.Diplo.AtWar(a, b)) n++;
        return n;
    }

    static bool RunWorld(int seed)
    {
        var sim = new Simulation(seed, Tuning.DefaultRivals);

        // player founds capital at the best available site
        Tile best = null; float bestScore = float.MinValue;
        for (int x = 0; x < sim.World.W; x++)
            for (int y = 0; y < sim.World.H; y++)
            {
                var t = sim.World.Get(x, y);
                float s = sim.Settlement.ScoreSite(t, sim.Player);
                if (s > bestScore) { bestScore = s; best = t; }
            }
        if (best == null) { Console.WriteLine("FAIL: no valid start site"); return false; }
        sim.FoundCity(sim.Player, best);
        Console.WriteLine($"Player founds at ({best.X},{best.Y}) biome={best.Biome} score={bestScore:0.0}");

        int landTiles = 0;
        for (int x = 0; x < sim.World.W; x++)
            for (int y = 0; y < sim.World.H; y++)
                if (sim.World.Get(x, y).Land) landTiles++;
        Console.WriteLine($"World: {landTiles} land tiles ({100f * landTiles / (sim.World.W * sim.World.H):0}%)");

        float nextReport = -3900f;
        var eraYears = new System.Collections.Generic.List<string>();
        Era lastEra = Era.Neolithic;
        while (sim.Year < Tuning.EndYear && !sim.Victory.GameOver)
        {
            sim.Tick(0.25f);

            if (sim.Player.Era != lastEra)
            {
                eraYears.Add($"{TechCatalog.EraNames[(int)sim.Player.Era]} @ {EventLog.FormatYear(sim.Year)}");
                lastEra = sim.Player.Era;
            }
            if (sim.Year >= nextReport)
            {
                nextReport += 700f;
                int cities = 0; float pop = 0;
                foreach (var c in sim.AllCities()) { cities++; pop += c.Population; }
                var p = sim.Player;
                float sciRate = 0f; foreach (var c in p.Cities) sciRate += c.SciYield;
                Console.WriteLine($"{EventLog.FormatYear(sim.Year),8} | cities {cities,3} | worldpop {pop,10:0} | " +
                    $"P: era={TechCatalog.EraNames[(int)p.Era]}, techs={p.Techs.Count}, cities={p.Cities.Count}, " +
                    $"pop={p.TotalPop:0}, happy={p.AvgHappiness:0}, gold={p.Gold:0}, sci={sciRate:0.0}/yr | " +
                    $"faiths={sim.Religions.Religions.Count} armies={sim.Armies.Count} civs={sim.Civs.Count} wars={CountWars(sim)}");
            }

            // sanity: NaN and negative checks
            foreach (var c in sim.AllCities())
            {
                if (float.IsNaN(c.Population) || float.IsNaN(c.Happiness) || c.Population < 0)
                { Console.WriteLine($"FAIL: bad numbers in {c.Name}: pop={c.Population} happy={c.Happiness}"); return false; }
            }
        }

        // ---- verdict ----
        Console.WriteLine("Player era timeline: " + string.Join(", ", eraYears));
        Console.WriteLine($"End: year={EventLog.FormatYear(sim.Year)} gameOver={sim.Victory.GameOver} " +
                          $"type={sim.Victory.VictoryType} winner={(sim.Victory.Winner?.Name ?? "none")}");
        int totalCities = 0; foreach (var c in sim.AllCities()) totalCities++;
        int events = sim.Log.Entries.Count;
        Console.WriteLine($"Chronicle entries: {events}. Sample of late history:");
        for (int i = Math.Max(0, sim.Log.Entries.Count - 8); i < sim.Log.Entries.Count; i++)
            Console.WriteLine($"   {EventLog.FormatYear(sim.Log.Entries[i].Year)} — {sim.Log.Entries[i].Text}");

        bool ok = true;
        void Check(bool cond, string what)
        { if (!cond) { Console.WriteLine("FAIL: " + what); ok = false; } else Console.WriteLine("OK: " + what); }

        Check(sim.Player.Techs.Count >= 8 || sim.Player.Eliminated, "player discovered technologies (or died trying)");
        Check(totalCities >= 4 || sim.Victory.GameOver, "the world grew cities");
        Check(sim.Religions.Religions.Count >= 1, "at least one faith was founded");
        Check(events > 40, "history happened (chronicle is rich)");
        Check(sim.Victory.GameOver, "the game reached a verdict (victory/legacy)");
        return ok;
    }
}
