using System;
using System.Collections.Generic;
using System.Linq;
using BombStack.Core;
using BombStack.Data;

internal static class PrototypeChecks
{
    private static readonly GameDefinition DEFINITION = GameDefinition.CreateDefault();
    private static int s_checks;

    private static void Check(bool condition, string message)
    {
        s_checks++;
        if (!condition) throw new Exception(message);
    }

    private static void Set(BoardData board, int x, int y, CELL_TYPE type, BOMB_TYPE bomb = BOMB_TYPE.NONE) => board.SetCell(new GridPosition(x, y), new CellData(type, bomb));

    private static RunData Example(int level = 1, params RELIC_TYPE[] relics)
    {
        var run = new RunData(DEFINITION, 1);
        run.Build.Bombs[0].Level = level;
        run.Build.Relics.AddRange(relics);
        Set(run.Board, 3, 1, CELL_TYPE.BOMB, BOMB_TYPE.B01_BASIC);
        Set(run.Board, 4, 4, CELL_TYPE.BOMB, BOMB_TYPE.B01_BASIC);
        Set(run.Board, 4, 3, CELL_TYPE.NORMAL);
        Set(run.Board, 4, 2, CELL_TYPE.NORMAL);
        Set(run.Board, 4, 1, CELL_TYPE.NORMAL);
        Set(run.Board, 5, 1, CELL_TYPE.TRIGGER);
        for (int x = 1; x <= 5; x++) Set(run.Board, x, 0, CELL_TYPE.NORMAL);
        Set(run.Board, 1, 1, CELL_TYPE.NORMAL);
        Set(run.Board, 1, 2, CELL_TYPE.NORMAL);
        Set(run.Board, 2, 2, CELL_TYPE.NORMAL);
        Set(run.Board, 2, 1, CELL_TYPE.TRIGGER);
        return run;
    }

    private static int Occupied(BoardData board)
    {
        int count = 0;
        for (int y = 0; y < board.Height; y++)
            for (int x = 0; x < board.Width; x++) if (board.GetCell(new GridPosition(x, y)).Type != CELL_TYPE.EMPTY) count++;
        return count;
    }

    private static string Fingerprint(BoardData board)
    {
        var cells = new List<string>();
        for (int y = 0; y < board.Height; y++)
            for (int x = 0; x < board.Width; x++)
            {
                CellData cell = board.GetCell(new GridPosition(x, y));
                cells.Add($"{(int)cell.Type}:{(int)cell.BombType}");
            }
        return string.Join(",", cells);
    }

    private static void ExplosionExamples()
    {
        var resolver = new ExplosionResolver(DEFINITION);
        RunData run = Example();
        string before = Fingerprint(run.Board);
        ExplosionWaveData preview = resolver.FindFirstWave(run.Board, run.Build);
        Check(before == Fingerprint(run.Board) && preview.ActivatedBombs.Count == 1, "preview never mutates");
        PlacementResultData result = resolver.Resolve(run.Board, run.Build);
        Check(result.Score.TotalScore == 150 && result.NormalCellExperience == 8 && result.WaveCount == 2 && result.ActivatedBombCount == 2, "GDD 14.2 150/8");
        Check(Occupied(run.Board) == 3 && Enumerable.Range(0, 3).All(y => run.Board.GetCell(new GridPosition(1, y)).Type == CELL_TYPE.NORMAL), "GDD final cells");
        Check(result.Waves[0].RemovedCells.Count(cell => cell.Cell.Type == CELL_TYPE.NORMAL) == 6 && result.Waves[1].RemovedCells.Count(cell => cell.Cell.Type == CELL_TYPE.NORMAL) == 2, "per-wave N counts");
        run = Example(2);
        result = resolver.Resolve(run.Board, run.Build);
        Check(result.Score.TotalScore == 70 && result.NormalCellExperience == 7 && result.WaveCount == 1 && result.ActivatedBombCount == 1, "GDD 14.3 70/7");
        run = Example(1, RELIC_TYPE.R01_PRECISION_BLAST);
        Check(resolver.Resolve(run.Board, run.Build).Score.TotalScore == 174, "precision 174");
        run = Example(1, RELIC_TYPE.R06_HEATPROOF_TRIGGER);
        result = resolver.Resolve(run.Board, run.Build);
        Check(result.Score.TotalScore == 150 && run.Board.GetCell(new GridPosition(2, 0)).Type == CELL_TYPE.TRIGGER && run.Board.GetCell(new GridPosition(5, 0)).Type == CELL_TYPE.TRIGGER, "heatproof preserved/gravity");
        run = Example(1, RELIC_TYPE.R02_CHAIN_AMPLIFIER, RELIC_TYPE.R04_GRAVITY_CHARGER, RELIC_TYPE.R05_DEMOLITION_BONUS);
        Check(resolver.Resolve(run.Board, run.Build).Score.TotalScore == 220, "stacked relic example 220");

        run = new RunData(DEFINITION, 2);
        run.Build.Bombs.Add(new BombProgressData(BOMB_TYPE.B04_RELAY, 1));
        run.Build.Bombs.Add(new BombProgressData(BOMB_TYPE.B03_VERTICAL, 1));
        Set(run.Board, 2, 1, CELL_TYPE.BOMB, BOMB_TYPE.B01_BASIC);
        Set(run.Board, 3, 1, CELL_TYPE.BOMB, BOMB_TYPE.B04_RELAY);
        Set(run.Board, 6, 1, CELL_TYPE.BOMB, BOMB_TYPE.B03_VERTICAL);
        Set(run.Board, 6, 4, CELL_TYPE.BOMB, BOMB_TYPE.B01_BASIC);
        Set(run.Board, 1, 1, CELL_TYPE.TRIGGER);
        Set(run.Board, 5, 1, CELL_TYPE.NORMAL);
        Set(run.Board, 6, 2, CELL_TYPE.NORMAL);
        result = resolver.Resolve(run.Board, run.Build);
        Check(result.Score.TotalScore == 25 && result.NormalCellExperience == 1 && result.ActivatedBombCount == 4 && result.WaveCount == 1, "GDD relay 25/1");
        Check(Occupied(run.Board) == 1 && run.Board.GetCell(new GridPosition(5, 0)).Type == CELL_TYPE.NORMAL, "relay leaves intermediate N");
        Check(result.Waves[0].RelayConnections.Count > 0, "relay links recorded separately");

        run = new RunData(DEFINITION, 3);
        Set(run.Board, 0, 5, CELL_TYPE.NORMAL);
        for (int x = 0; x < 10; x++) Set(run.Board, x, 0, CELL_TYPE.NORMAL);
        result = resolver.Resolve(run.Board, run.Build);
        Check(result.WaveCount == 0 && result.Score.TotalScore == 0 && Occupied(run.Board) == 11 && run.Board.GetCell(new GridPosition(0, 5)).Type == CELL_TYPE.NORMAL, "no blast no gravity, full row not cleared");
        Set(run.Board, 8, 1, CELL_TYPE.BOMB, BOMB_TYPE.B01_BASIC);
        Set(run.Board, 9, 1, CELL_TYPE.TRIGGER);
        result = resolver.Resolve(run.Board, run.Build);
        Check(run.Board.GetCell(new GridPosition(0, 1)).Type == CELL_TYPE.NORMAL, "global gravity includes distant columns");

        run = new RunData(DEFINITION, 4);
        Set(run.Board, 2, 2, CELL_TYPE.BOMB, BOMB_TYPE.B01_BASIC);
        Set(run.Board, 3, 3, CELL_TYPE.TRIGGER);
        Check(resolver.FindFirstWave(run.Board, run.Build) == null, "no base diagonal ignition");
        run.Build.Relics.Add(RELIC_TYPE.R03_IGNITION_AMPLIFIER);
        Check(resolver.FindFirstWave(run.Board, run.Build) != null, "diagonal relic ignition");
        run.Build.RecoveryMasteryCount = 1;
        var synthetic = new PlacementResultData { DestroyedNormalCellCount = 1, ActivatedBombCount = 2 };
        synthetic.Waves.Add(new ExplosionWaveData());
        resolver.CalculateScore(synthetic, run.Build);
        Check(synthetic.Score.TotalScore == 16, "single total floor 16");

        run = new RunData(DEFINITION, 5);
        run.Build.Relics.AddRange(new[] { RELIC_TYPE.R02_CHAIN_AMPLIFIER, RELIC_TYPE.R04_GRAVITY_CHARGER, RELIC_TYPE.R05_DEMOLITION_BONUS });
        synthetic = new PlacementResultData { DestroyedNormalCellCount = 10, ActivatedBombCount = 3 };
        synthetic.Waves.Add(new ExplosionWaveData());
        synthetic.Waves.Add(new ExplosionWaveData());
        resolver.CalculateScore(synthetic, run.Build);
        Check(synthetic.Score.TotalScore == 385 && synthetic.NormalCellExperience == 10, "GDD 385/10 score example");
        synthetic = new PlacementResultData { DestroyedNormalCellCount = 1, ActivatedBombCount = 8 };
        synthetic.Waves.Add(new ExplosionWaveData());
        resolver.CalculateScore(synthetic, run.Build);
        Check(synthetic.Score.MultiplierTwice == 7 && synthetic.Score.TotalScore == 35, "base multiplier cap then relic");

        run = new RunData(DEFINITION, 6);
        Set(run.Board, 3, 0, CELL_TYPE.BOMB, BOMB_TYPE.B01_BASIC);
        Set(run.Board, 4, 0, CELL_TYPE.TRIGGER);
        Set(run.Board, 5, 0, CELL_TYPE.BOMB, BOMB_TYPE.B01_BASIC);
        Set(run.Board, 4, 1, CELL_TYPE.NORMAL);
        result = resolver.Resolve(run.Board, run.Build);
        Check(result.ActivatedBombCount == 2 && result.WaveCount == 1 && result.DestroyedNormalCellCount == 1, "one T ignites both B, overlap counted once");
    }

    private static void SupplyAndRotation()
    {
        var run = new RunData(DEFINITION, 1234);
        var other = new RunData(DEFINITION, 1234);
        SupplyRules.Initialize(run);
        SupplyRules.Initialize(other);
        for (int i = 0; i < 100; i++) SupplyRules.NextIndex(other.Supply.RandomStreams.GrowthChoice, 3);
        var generated = new List<PieceData>();
        for (int i = 0; i < 280; i++)
        {
            PieceData a = SupplyRules.ConsumeNext(DEFINITION, run);
            PieceData b = SupplyRules.ConsumeNext(DEFINITION, other);
            generated.Add(a);
            Check(a.Shape == b.Shape && a.Attribute == b.Attribute && a.SpecialCellIndex == b.SpecialCellIndex && a.BombType == b.BombType, "independent RNG/reproducible supply");
            Check(run.Supply.Preview.Count == 3, "preview always three");
        }
        for (int i = 0; i < generated.Count; i += 7) Check(generated.Skip(i).Take(7).Select(p => p.Shape).Distinct().Count() == 7, "shape bag exact");
        for (int i = 0; i < generated.Count; i += 4)
        {
            var bag = generated.Skip(i).Take(4).ToArray();
            Check(bag.Count(p => p.Attribute == CELL_TYPE.BOMB) == 2 && bag.Count(p => p.Attribute == CELL_TYPE.TRIGGER) == 1 && bag.Count(p => p.Attribute == CELL_TYPE.NORMAL) == 1, "attribute bag exact");
        }

        run = new RunData(DEFINITION, 88);
        SupplyRules.Initialize(run);
        run.Supply.Preview.Add(new QueuedPieceData(1, new PieceData(PIECE_SHAPE.T, CELL_TYPE.BOMB, 3, BOMB_TYPE.B01_BASIC)));
        run.Supply.Preview.Add(new QueuedPieceData(2, new PieceData(PIECE_SHAPE.O, CELL_TYPE.TRIGGER, 2)));
        run.Supply.Preview.Add(new QueuedPieceData(3, new PieceData(PIECE_SHAPE.L, CELL_TYPE.BOMB, 1, BOMB_TYPE.B01_BASIC)));
        run.Supply.NextPieceId = 4;
        run.Build.Bombs.Add(new BombProgressData(BOMB_TYPE.B02_HORIZONTAL, 1));
        SupplyRules.OnBombAcquired(run, BOMB_TYPE.B02_HORIZONTAL);
        run.Build.Bombs.Add(new BombProgressData(BOMB_TYPE.B04_RELAY, 1));
        SupplyRules.OnBombAcquired(run, BOMB_TYPE.B04_RELAY);
        Check(run.Supply.FirstAppearances[0].QueuedPieceId == 1 && run.Supply.FirstAppearances[1].QueuedPieceId == 3, "FIFO reserves separate B");
        Check(run.Supply.Preview[0].Piece.BombType == BOMB_TYPE.B02_HORIZONTAL && run.Supply.Preview[0].Piece.SpecialCellIndex == 3 && run.Supply.Preview[1].Piece.Attribute == CELL_TYPE.TRIGGER, "reservation changes only subtype");
        SupplyRules.ConsumeNext(DEFINITION, run);
        Check(run.Supply.FirstAppearances.Count == 1 && run.Supply.FirstAppearances[0].QueuedPieceId == 3, "reservation completed on spawn only");

        foreach (PieceDefinition definition in DEFINITION.Pieces)
        {
            var session = new GameSession(DEFINITION, 7);
            session.Run.ActivePiece = new ActivePieceData(new PieceData(definition.Shape, CELL_TYPE.TRIGGER, 0), DEFINITION.Board.SpawnOrigin);
            string original = string.Join(";", BoardRules.GetPositions(DEFINITION, session.Run.ActivePiece.Piece, session.Run.ActivePiece.Origin, session.Run.ActivePiece.Rotation).Select(p => p.ToString()));
            for (int i = 0; i < 4; i++) Check(session.Rotate(true), "spawn rotation valid");
            Check(string.Join(";", BoardRules.GetPositions(DEFINITION, session.Run.ActivePiece.Piece, session.Run.ActivePiece.Origin, session.Run.ActivePiece.Rotation).Select(p => p.ToString())) == original, "four actual rotations restore identity");
        }
        var timer = new GameSession(DEFINITION, 1);
        timer.Run.ActivePiece = new ActivePieceData(new PieceData(PIECE_SHAPE.O, CELL_TYPE.NORMAL), new GridPosition(3, -1));
        timer.Tick(0.1f, false);
        Check(timer.Run.ActivePiece.HasTouchedGround, "ground contact recorded");
        timer.Tick(0.4f, false);
        timer.Move(1);
        timer.Rotate(true);
        timer.Tick(0.41f, false);
        Check(timer.Run.State == RUN_STATE.RESOLVING, "move/rotate do not reset lock");
        timer.Pause();
        double elapsed = timer.Run.PlaySeconds;
        timer.Tick(100, true);
        Check(timer.Run.State == RUN_STATE.PAUSED && timer.Run.PlaySeconds == elapsed, "pause freezes timers");
        timer.Resume();
        Check(timer.Run.State == RUN_STATE.RESOLVING, "pause restores previous state");

        timer = new GameSession(DEFINITION, 1);
        timer.Run.ActivePiece = new ActivePieceData(new PieceData(PIECE_SHAPE.O, CELL_TYPE.NORMAL), new GridPosition(3, 1));
        Set(timer.Run.Board, 4, 1, CELL_TYPE.NORMAL);
        timer.Tick(0.1f, false);
        timer.Tick(0.6f, false);
        timer.Move(1);
        timer.Tick(0.3f, false);
        Check(timer.Run.State == RUN_STATE.PIECE_CONTROL && timer.Run.ActivePiece.LockElapsedSeconds >= 0.8f, "expired lock stays airborne until landing");
        timer.Tick(1, false);
        Check(timer.Run.State == RUN_STATE.RESOLVING, "expired airborne lock fixes on landing");

        for (int seed = 0; seed < 32; seed++)
        {
            run = new RunData(DEFINITION, seed);
            SupplyRules.Initialize(run);
            SupplyRules.FillPreview(DEFINITION, run, 3);
            run.Build.Bombs.Add(new BombProgressData(BOMB_TYPE.B02_HORIZONTAL, 1));
            SupplyRules.OnBombAcquired(run, BOMB_TYPE.B02_HORIZONTAL);
            run.Build.Bombs.Add(new BombProgressData(BOMB_TYPE.B04_RELAY, 1));
            SupplyRules.OnBombAcquired(run, BOMB_TYPE.B04_RELAY);
            int horizontalAt = 0;
            int relayAt = 0;
            for (int i = 1; i <= 6; i++)
            {
                PieceData piece = SupplyRules.ConsumeNext(DEFINITION, run);
                if (piece.BombType == BOMB_TYPE.B02_HORIZONTAL && horizontalAt == 0) horizontalAt = i;
                if (piece.BombType == BOMB_TYPE.B04_RELAY && relayAt == 0) relayAt = i;
            }
            Check(horizontalAt >= 1 && horizontalAt <= 5 && relayAt > horizontalAt && relayAt <= 6, "first appearances within 5/6 placements");
        }
    }

    private static GameSession Pending(long score, int xp)
    {
        var session = new GameSession(DEFINITION, 55);
        session.HardDrop();
        session.Run.LastPlacement.Score.DestructionScore = score;
        session.Run.LastPlacement.NormalCellExperience = xp;
        return session;
    }

    private static void Progression()
    {
        GameSession session = Pending(200, 20);
        session.Run.RemainingPieces = 0;
        Check(session.FinishResolution() && session.Run.State == RUN_STATE.LEVEL_UP_CHOICE && session.Run.Build.Experience == 30, "last piece target succeeds, completion XP before growth");
        long total = session.Run.TotalScore;
        Check(!session.FinishResolution() && session.Run.TotalScore == total, "settles exactly once");
        BoardData board = session.Run.Board;
        SupplyData supply = session.Run.Supply;
        int count = 0;
        while (session.Run.State == RUN_STATE.LEVEL_UP_CHOICE)
        {
            Check(session.Run.GrowthChoices.Count == 3 && session.Run.GrowthChoices.Count(c => c.Type == GROWTH_CHOICE_TYPE.COMMON_UPGRADE) >= 1, "three offers contain common");
            session.ChooseGrowth(session.Run.GrowthChoices.FindIndex(c => c.Type == GROWTH_CHOICE_TYPE.COMMON_UPGRADE));
            count++;
        }
        Check(count == 2 && session.Run.Build.PlayerLevel == 3 && session.Run.Build.Experience == 10 && session.Run.State == RUN_STATE.RELIC_CHOICE, "multiple levels then relic");
        session.ChooseRelic(0);
        Check(session.Run.StageNumber == 2 && session.Run.StageScore == 0 && session.Run.RemainingPieces == 20 && session.Run.TotalScore == 200, "stage reset/total retained");
        Check(ReferenceEquals(board, session.Run.Board) && ReferenceEquals(supply, session.Run.Supply) && session.Run.Build.Relics.Count == 1, "stage retains board/supply/build");
        Check(!session.ChooseRelic(0), "cannot claim relic twice");

        session = Pending(0, 30);
        session.Run.RemainingPieces = 0;
        session.FinishResolution();
        Check(session.Run.EndReason == RUN_END_REASON.PIECE_LIMIT && session.Run.GrowthChoices.Count == 0, "failure before growth");
        session = Pending(200, 20);
        PieceData next = session.Run.Supply.Preview[0].Piece;
        GridPosition blocked = BoardRules.GetPositions(DEFINITION, next, DEFINITION.Board.SpawnOrigin, PIECE_ROTATION.SPAWN)[0];
        Set(session.Run.Board, blocked.X, blocked.Y, CELL_TYPE.NORMAL);
        session.FinishResolution();
        Check(session.Run.EndReason == RUN_END_REASON.SPAWN_BLOCKED && session.Run.LastPlacement.CompletionExperience == 0 && session.Run.Build.Experience == 20, "blocked intermediate no completion XP/growth");
        session = Pending(800, 50);
        session.Run.StageNumber = 5;
        next = session.Run.Supply.Preview[0].Piece;
        blocked = BoardRules.GetPositions(DEFINITION, next, DEFINITION.Board.SpawnOrigin, PIECE_ROTATION.SPAWN)[0];
        Set(session.Run.Board, blocked.X, blocked.Y, CELL_TYPE.NORMAL);
        session.FinishResolution();
        Check(session.Run.EndReason == RUN_END_REASON.VICTORY && session.Run.LastPlacement.CompletionExperience == 0 && session.Run.GrowthChoices.Count == 0, "final victory before spawn/growth");
        Check(RecordRules.IsBetter(RunRecordData.FromRun(session.Run), null), "victory record accepted");

        session = Pending(0, 8);
        session.Run.Build.Bombs[0].Level = 3;
        session.FinishResolution();
        Check(session.Run.GrowthChoices.Count(c => c.Type == GROWTH_CHOICE_TYPE.ACQUIRE_BOMB) == 2 && session.Run.GrowthChoices.Count(c => c.Type == GROWTH_CHOICE_TYPE.COMMON_UPGRADE) == 1, "max basic does not force acquisition");
        session = Pending(0, 8);
        session.Run.Build.Bombs[0].Level = 3;
        session.Run.Build.Bombs.Add(new BombProgressData(BOMB_TYPE.B02_HORIZONTAL, 3));
        session.Run.Build.Bombs.Add(new BombProgressData(BOMB_TYPE.B04_RELAY, 3));
        session.FinishResolution();
        Check(session.Run.GrowthChoices.Count == 3 && session.Run.GrowthChoices.All(c => c.Type == GROWTH_CHOICE_TYPE.COMMON_UPGRADE) && session.Run.GrowthChoices.Select(c => c.CommonUpgradeType).Distinct().Count() == 3, "fully upgraded build offers three different common upgrades");

        // 실제 공급/고정/정산을 반복해 종료와 결정성을 검사합니다. 자동 배치의 성적은 재미 검증이 아닙니다.
        for (int seed = 0; seed < 24; seed++)
        {
            var a = new GameSession(DEFINITION, seed);
            var b = new GameSession(DEFINITION, seed);
            int steps = 0;
            while (a.Run.State != RUN_STATE.RESULT && steps++ < 300)
            {
                Check(a.Run.State == b.Run.State, "deterministic state");
                switch (a.Run.State)
                {
                    case RUN_STATE.PIECE_CONTROL:
                        int direction = (steps % 3) - 1;
                        if (direction != 0) { a.Move(direction); b.Move(direction); }
                        a.HardDrop(); b.HardDrop(); break;
                    case RUN_STATE.RESOLVING: a.FinishResolution(); b.FinishResolution(); break;
                    case RUN_STATE.LEVEL_UP_CHOICE: a.ChooseGrowth(0); b.ChooseGrowth(0); break;
                    case RUN_STATE.RELIC_CHOICE: a.ChooseRelic(0); b.ChooseRelic(0); break;
                }
                Check(a.Run.TotalScore == b.Run.TotalScore && Fingerprint(a.Run.Board) == Fingerprint(b.Run.Board), "deterministic scores/board");
            }
            Check(a.Run.State == RUN_STATE.RESULT && steps < 300, "finite full run ends");
        }
    }

    private static void Main()
    {
        ExplosionExamples();
        SupplyAndRotation();
        Progression();
        Console.WriteLine($"PASS: {s_checks} prototype checks (C# 9 / .NET Standard 2.1 core). GDD examples, supply, rotation, timers, progression, 24 paired deterministic runs.");
    }
}
