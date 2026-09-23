using System;
using System.Collections.Generic;
using P = BattlePatternRules.Pattern;

// Standalone harness for the exact BattlePatternRules class extracted from Plate.cs.
// Tiny Unity value/random substitutes make the coordinate rules runnable without the editor.
namespace UnityEngine
{
    public struct Vector2Int
    {
        public int x, y;
        public Vector2Int(int x, int y) { this.x = x; this.y = y; }
    }
    public static class Random
    {
        public static int Group;
        public static int Range(int min, int max) => Group;
    }
}

public static class AfterlifePatternTests
{
    static int checks;
    static void Check(bool value)
    {
        checks++;
        if (!value) throw new Exception("Failed check " + checks);
    }

    static void Mask(P pattern, string rows)
    {
        var expected = rows.Split('/');
        var actual = BattlePatternRules.GetDangerCells(pattern);
        for (int y = 0; y < 9; y++)
            for (int x = 0; x < 9; x++)
                Check(actual.Contains(new UnityEngine.Vector2Int(x, y)) == (expected[y][x] == '#'));
        foreach (var cell in actual) Check(cell.x >= 0 && cell.x < 9 && cell.y >= 0 && cell.y < 9);
    }

    static void Sequence(BattlePatternRules.PatternSequence sequence, P[][] groups)
    {
        for (int g = 0; g < groups.Length; g++)
        {
            sequence.Reset();
            Check(!sequence.IsLastStep);
            UnityEngine.Random.Group = g;
            Check(sequence.Next() == groups[g][0]);
            Check(sequence.IsLastStep == (groups[g].Length == 1));
            // Changing the next lottery must not change an in-progress group.
            UnityEngine.Random.Group = (g + 1) % groups.Length;
            for (int i = 1; i < groups[g].Length; i++)
            {
                Check(sequence.Next() == groups[g][i]);
                Check(sequence.IsLastStep == (i == groups[g].Length - 1));
            }
            Check(sequence.Next() == groups[(g + 1) % groups.Length][0]);
            sequence.Reset();
            Check(sequence.Next() == groups[(g + 1) % groups.Length][0]);
        }
    }

    public static void Main()
    {
        string checker = "###...###/###...###/###...###/...###.../...###.../...###.../###...###/###...###/###...###";
        string inverse = "...###.../...###.../...###.../###...###/###...###/###...###/...###.../...###.../...###...";
        string full = "#########/#########/#########/#########/#########/#########/#########/#########/#########";
        string top = "#########/#########/#########/........./........./........./........./........./.........";
        string sides = "###...###/###...###/###...###/###...###/###...###/###...###/###...###/###...###/###...###";
        string inner = "........./.#######./.#######./.#######./.#######./.#######./.#######./.#######./.........";
        Mask(P.AfterlifePhase1A1, checker); Mask(P.AfterlifePhase1A2, inverse); Mask(P.AfterlifePhase1A3, full);
        Mask(P.AfterlifePhase1B1, top); Mask(P.AfterlifePhase1B2, sides); Mask(P.AfterlifePhase1B3, inner);
        Mask(P.AfterlifePhase1C1, "#.......#/.#.....#./..#...#../...#.#.../....#..../...#.#.../..#...#../.#.....#./#.......#");
        Mask(P.AfterlifePhase1C2, "....#..../....#..../....#..../....#..../#########/....#..../....#..../....#..../....#....");
        Mask(P.AfterlifePhase1C3, inner);
        Mask(P.AfterlifePhase2A1, "###....../###....../###....../###....../###....../###....../###....../###....../###......");
        Mask(P.AfterlifePhase2A2, "......###/......###/......###/......###/......###/......###/......###/......###/......###");
        Mask(P.AfterlifePhase2A3, inner); Mask(P.AfterlifePhase2A4, full);
        Mask(P.AfterlifePhase2B1, sides); Mask(P.AfterlifePhase2B2, checker);
        Mask(P.AfterlifePhase2B3, sides); Mask(P.AfterlifePhase2B4, inverse);
        Mask(P.AfterlifePhase2C1, top); Mask(P.AfterlifePhase2C2, full);
        // Audience chamber reference: # is bright/attacked, . is dark/safe.
        // Explicit masks also catch incorrect stripe widths and the hollow safe ring in A2.
        Mask(P.KingA1, "...###.../...###.../...###.../...###.../...###.../...###.../...###.../...###.../...###...");
        Mask(P.KingA2, "#########/#.......#/#.#####.#/#.#####.#/#.#####.#/#.#####.#/#.#####.#/#.......#/#########");
        Mask(P.KingA3, "##.....##/##.....##/##.....##/##.....##/##.....##/##.....##/##.....##/##.....##/##.....##");
        Mask(P.KingB1, "#.......#/.#.....#./..#...#../...#.#.../....#..../...#.#.../..#...#../.#.....#./#.......#");
        Mask(P.KingB2, "....#..../....#..../....#..../....#..../#########/....#..../....#..../....#..../....#....");
        Mask(P.KingB3, "#########/#########/#########/###...###/###...###/###...###/#########/#########/#########");
        Mask(P.KingC1, ".###.###./.###.###./.###.###./.###.###./.###.###./.###.###./.###.###./.###.###./.###.###.");
        Mask(P.KingC2, "........./#########/#########/#########/........./#########/#########/#########/.........");
        Mask(P.KingC3, "........./........./..#####../..#####../..#####../..#####../..#####../........./.........");
        Mask(P.KingC4, full);
        Sequence(new BattlePatternRules.KingSequence(), new[] {
            new[] { P.KingA1, P.KingA2, P.KingA3 },
            new[] { P.KingB1, P.KingB2, P.KingB3 },
            new[] { P.KingC1, P.KingC2, P.KingC3, P.KingC4 }});
        Sequence(new BattlePatternRules.AfterlifePhase1Sequence(), new[] {
            new[] { P.AfterlifePhase1A1, P.AfterlifePhase1A2, P.AfterlifePhase1A3 },
            new[] { P.AfterlifePhase1B1, P.AfterlifePhase1B2, P.AfterlifePhase1B3 },
            new[] { P.AfterlifePhase1C1, P.AfterlifePhase1C2, P.AfterlifePhase1C3 }});
        Sequence(new BattlePatternRules.AfterlifePhase2Sequence(), new[] {
            new[] { P.AfterlifePhase2A1, P.AfterlifePhase2A2, P.AfterlifePhase2A3, P.AfterlifePhase2A4 },
            new[] { P.AfterlifePhase2B1, P.AfterlifePhase2B2, P.AfterlifePhase2B3, P.AfterlifePhase2B4 },
            new[] { P.AfterlifePhase2C1, P.AfterlifePhase2C2 }});
        Check(BattlePatternRules.GetDangerCells(P.None).Count == 0);
        var red = new HashSet<P> { P.AfterlifePhase1A3, P.AfterlifePhase2A4, P.AfterlifePhase2C2 };
        var green = new HashSet<P> { P.AfterlifePhase1B2, P.AfterlifePhase2B1, P.AfterlifePhase2B3 };
        var yellow = new HashSet<P> { P.AfterlifePhase1B3, P.AfterlifePhase1C3, P.AfterlifePhase2A3 };
        foreach (P pattern in Enum.GetValues(typeof(P)))
        {
            var expectedEffect = red.Contains(pattern) ? BattlePatternRules.ColorEffect.Relocate
                : green.Contains(pattern) ? BattlePatternRules.ColorEffect.ReduceNextActionPoints
                : yellow.Contains(pattern) ? BattlePatternRules.ColorEffect.DamageBossOnDodge
                : pattern == P.KingB3 ? BattlePatternRules.ColorEffect.IncreaseKingWillPower
                : pattern == P.KingA2 ? BattlePatternRules.ColorEffect.HealKing
                : pattern == P.KingC4 ? BattlePatternRules.ColorEffect.RelocateKingTargets
                : BattlePatternRules.ColorEffect.None;
            Check(BattlePatternRules.GetColorEffect(pattern) == expectedEffect);
        }
        UnityEngine.Random.Group = 0;
        var occupied = new HashSet<UnityEngine.Vector2Int> { new UnityEngine.Vector2Int(0, 0), new UnityEngine.Vector2Int(1, 0) };
        var relocated = BattlePatternRules.PickRelocationCells(3, occupied);
        Check(relocated.Count == 3);
        Check(new HashSet<UnityEngine.Vector2Int>(relocated).Count == 3);
        foreach (var cell in relocated) Check(!occupied.Contains(cell));
        Check(occupied.Count == 2);
        Check(BattlePatternRules.PickRelocationCells(0, occupied).Count == 0);
        var allCells = BattlePatternRules.PickRelocationCells(81, new HashSet<UnityEngine.Vector2Int>());
        Check(allCells.Count == 81 && new HashSet<UnityEngine.Vector2Int>(allCells).Count == 81);
        Check(BattlePatternRules.PickRelocationCells(3, new HashSet<UnityEngine.Vector2Int>(allCells)).Count == 0);
        Console.WriteLine("PASS: " + checks + " coordinate and sequence assertions (29 patterns)");
    }
}
