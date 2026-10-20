using System;
using System.Collections.Generic;

namespace BombStack.Data
{
    public sealed class QueuedPieceData
    {
        public long Id { get; }
        public PieceData Piece { get; set; }

        public QueuedPieceData(long id, PieceData piece)
        {
            Id = id;
            Piece = piece ?? throw new ArgumentNullException(nameof(piece));
        }
    }

    /// <summary>목록 순서가 획득 순서입니다. null은 미배정, ID는 예약된 대기열 조각입니다.</summary>
    public sealed class FirstAppearanceData
    {
        public BOMB_TYPE BombType { get; }
        public long? QueuedPieceId { get; set; }

        public FirstAppearanceData(BOMB_TYPE bombType)
        {
            if (bombType == BOMB_TYPE.NONE || !Enum.IsDefined(typeof(BOMB_TYPE), bombType))
                throw new ArgumentOutOfRangeException(nameof(bombType));
            BombType = bombType;
        }
    }

    /// <summary>난수 구현이 사용할 상태 공간. 초기화/전진 알고리즘은 공급 로직에서 구현합니다.</summary>
    public sealed class RandomStreamStateData
    {
        public bool IsInitialized { get; set; }
        public ulong State { get; set; }
    }

    public sealed class RandomStreamsData
    {
        public RandomStreamStateData Shape { get; } = new RandomStreamStateData();
        public RandomStreamStateData Attribute { get; } = new RandomStreamStateData();
        public RandomStreamStateData SpecialCell { get; } = new RandomStreamStateData();
        public RandomStreamStateData BombType { get; } = new RandomStreamStateData();
        public RandomStreamStateData GrowthChoice { get; } = new RandomStreamStateData();
        public RandomStreamStateData RelicChoice { get; } = new RandomStreamStateData();
    }

    public sealed class SupplyData
    {
        // 각 Bag은 섞인 전체 목록과 다음 소비 인덱스를 함께 보관합니다.
        public List<PIECE_SHAPE> ShapeBag { get; } = new List<PIECE_SHAPE>();
        public int ShapeBagIndex { get; set; }
        public List<CELL_TYPE> AttributeBag { get; } = new List<CELL_TYPE>();
        public int AttributeBagIndex { get; set; }
        public List<BOMB_TYPE> BombTypeBag { get; } = new List<BOMB_TYPE>();
        public int BombTypeBagIndex { get; set; }
        public List<QueuedPieceData> Preview { get; } = new List<QueuedPieceData>();
        public List<FirstAppearanceData> FirstAppearances { get; } = new List<FirstAppearanceData>();
        public long NextPieceId { get; set; } = 1;
        public RandomStreamsData RandomStreams { get; } = new RandomStreamsData();
    }
}
