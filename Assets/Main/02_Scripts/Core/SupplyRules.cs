using System;
using System.Collections.Generic;
using BombStack.Data;

namespace BombStack.Core
{
    public static class SupplyRules
    {
        public static void Initialize(RunData run)
        {
            var seed = new RandomStreamStateData { State = unchecked((ulong)(long)run.Seed), IsInitialized = true };
            RandomStreamsData streams = run.Supply.RandomStreams;
            foreach (RandomStreamStateData stream in new[] { streams.Shape, streams.Attribute, streams.SpecialCell, streams.BombType, streams.GrowthChoice, streams.RelicChoice })
            {
                stream.State = Next(seed);
                stream.IsInitialized = true;
            }
        }

        // SplitMix64: 버전 내 재현을 위해 System.Random 및 UnityEngine.Random을 사용하지 않습니다.
        private static ulong Next(RandomStreamStateData stream)
        {
            if (!stream.IsInitialized) throw new InvalidOperationException("난수 스트림을 먼저 초기화해야 합니다.");
            unchecked
            {
                ulong value = stream.State += 0x9E3779B97F4A7C15UL;
                value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
                value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
                return value ^ (value >> 31);
            }
        }

        public static int NextIndex(RandomStreamStateData stream, int count)
        {
            if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));
            uint threshold = unchecked(0u - (uint)count) % (uint)count;
            uint sample;
            do { sample = (uint)Next(stream); } while (sample < threshold);
            return (int)(sample % (uint)count);
        }

        public static void Shuffle<T>(List<T> items, RandomStreamStateData stream)
        {
            for (int i = items.Count - 1; i > 0; i--)
            {
                int index = NextIndex(stream, i + 1);
                T temporary = items[i];
                items[i] = items[index];
                items[index] = temporary;
            }
        }

        public static void FillPreview(GameDefinition definition, RunData run, int count)
        {
            SupplyData supply = run.Supply;
            while (supply.Preview.Count < count)
            {
                if (supply.ShapeBagIndex >= supply.ShapeBag.Count)
                {
                    supply.ShapeBag.Clear();
                    supply.ShapeBag.AddRange(definition.Supply.ShapeBag);
                    Shuffle(supply.ShapeBag, supply.RandomStreams.Shape);
                    supply.ShapeBagIndex = 0;
                }
                if (supply.AttributeBagIndex >= supply.AttributeBag.Count)
                {
                    supply.AttributeBag.Clear();
                    supply.AttributeBag.AddRange(definition.Supply.AttributeBag);
                    Shuffle(supply.AttributeBag, supply.RandomStreams.Attribute);
                    supply.AttributeBagIndex = 0;
                }
                PIECE_SHAPE shape = supply.ShapeBag[supply.ShapeBagIndex++];
                CELL_TYPE attribute = supply.AttributeBag[supply.AttributeBagIndex++];
                int special = attribute == CELL_TYPE.NORMAL ? -1 : NextIndex(supply.RandomStreams.SpecialCell, 4);
                long id = supply.NextPieceId++;
                BOMB_TYPE type = BOMB_TYPE.NONE;
                if (attribute == CELL_TYPE.BOMB)
                {
                    FirstAppearanceData pending = supply.FirstAppearances.Find(item => !item.QueuedPieceId.HasValue);
                    if (pending != null) { type = pending.BombType; pending.QueuedPieceId = id; }
                    else
                    {
                        if (supply.BombTypeBagIndex >= supply.BombTypeBag.Count) RebuildBombBag(run);
                        type = supply.BombTypeBag[supply.BombTypeBagIndex++];
                    }
                }
                supply.Preview.Add(new QueuedPieceData(id, new PieceData(shape, attribute, special, type)));
            }
        }

        public static PieceData ConsumeNext(GameDefinition definition, RunData run)
        {
            FillPreview(definition, run, definition.Supply.PreviewCount + 1);
            QueuedPieceData next = run.Supply.Preview[0];
            run.Supply.Preview.RemoveAt(0);
            run.Supply.FirstAppearances.RemoveAll(item => item.QueuedPieceId == next.Id);
            return next.Piece;
        }

        public static void OnBombAcquired(RunData run, BOMB_TYPE type)
        {
            RebuildBombBag(run);
            run.Supply.FirstAppearances.Add(new FirstAppearanceData(type));
            var reserved = new HashSet<long>();
            foreach (FirstAppearanceData item in run.Supply.FirstAppearances)
                if (item.QueuedPieceId.HasValue) reserved.Add(item.QueuedPieceId.Value);
            foreach (FirstAppearanceData pending in run.Supply.FirstAppearances)
            {
                if (pending.QueuedPieceId.HasValue) continue;
                foreach (QueuedPieceData queued in run.Supply.Preview)
                {
                    if (queued.Piece.Attribute != CELL_TYPE.BOMB || reserved.Contains(queued.Id)) continue;
                    PieceData previous = queued.Piece;
                    queued.Piece = new PieceData(previous.Shape, previous.Attribute, previous.SpecialCellIndex, pending.BombType);
                    pending.QueuedPieceId = queued.Id;
                    reserved.Add(queued.Id);
                    break;
                }
            }
        }

        private static void RebuildBombBag(RunData run)
        {
            run.Supply.BombTypeBag.Clear();
            foreach (BombProgressData bomb in run.Build.Bombs) run.Supply.BombTypeBag.Add(bomb.Type);
            Shuffle(run.Supply.BombTypeBag, run.Supply.RandomStreams.BombType);
            run.Supply.BombTypeBagIndex = 0;
        }
    }
}
