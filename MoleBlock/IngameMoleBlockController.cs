using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IngameMoleBlockController : InGameControllerBaseMono
{
    private List<MoleBlock> _moleBlocks;
    private List<MoleTunnelTile> _tunnels;
    private List<int> _moleColorTypes;

    private int _movedTurn = 0;

    public override void Init()
    {
        InitData();
    }

    public override void ReInit()
    {
        InitData();
    }

    private void InitData()
    {
        if (_moleBlocks == null)
            _moleBlocks = new List<MoleBlock>();

        _moleBlocks.Clear();

        _movedTurn = InGameController.Instance.TurnCounter.RemainTurn;
    }

    public override void PostInit()
    {
        SetMoleRandomColorList();
    }

    private void SetMoleRandomColorList()
    {
        if (_moleColorTypes == null)
            _moleColorTypes = new List<int>();

        var possibleTypes = StageDataManager.it.StageData.GetPossibleBlockTypes();

        foreach (var type in possibleTypes)
        {
            if (EBlockTypeUtil.IsAvailableMoleColorType(type)
                && !_moleColorTypes.Contains(type))
            {
                _moleColorTypes.Add(type);
            }

            // 풍선블록과 같은 색의 두더지도 노출되게 수정
            if (EBlockTypeUtil.IsBalloonBlock(type))
            {
                var index = type - (int)EBlockType.BALLOON_RANDOM;
                if (!_moleColorTypes.Contains(index))
                    _moleColorTypes.Add(index);
            }
        }
    }

    public int GetColorType(int inIndex)
    {
        var colorType = StageDataManager.it.StageData.MissionObjectData.GetMoleBlockTypeAt(inIndex);

        if (_moleColorTypes.Contains(colorType))
            return colorType;

        return GetRandomType();
    }

    public int GetRandomType()
    {
        if (_moleColorTypes == null)
            return 0;

        return RandomUtil.GetItemByRandom(_moleColorTypes);
    }

    public override void OnLayerInitialized()
    {
        SetTunnels();
        SetRandomMole();
    }

    private void SetTunnels()
    {
        if (_tunnels == null)
            _tunnels = new List<MoleTunnelTile>();

        for (int i = 0; i < StageData.BLOCK_TOTAL; ++i)
        {
            BoardTile boardTile = LayerManager.it.BoardLayer.GetBoardTileAt(i);
            if (boardTile == null)
                continue;

            if (boardTile.Type == (int)EBoardTileType.MOLE_TUNNEL
                && boardTile is MoleTunnelTile)
            {
                _tunnels.Add(boardTile as MoleTunnelTile);
            }
        }
    }

    private void SetRandomMole()
    {
        var randomCount = StageDataManager.it.StageData.GetMoleRandomCount();
        if (randomCount <= _moleBlocks.Count)
            return;

        int addCount = randomCount - _moleBlocks.Count;
        var targetIndice = GetAvailableTunnelIndice();
        if (targetIndice.Count == 0)
            return;

        while (addCount > 0 && targetIndice.Count > 0)
        {
            var targetIndex = RandomUtil.GetItemByRandom(targetIndice);

            LayerManager.it.BlockLayer.RemoveBlockAtWithoutAnimation(targetIndex, null);

            LayerManager.it.BlockLayer.SetBlockAt(targetIndex, AddOneMole(targetIndex));

            --addCount;
            targetIndice.Remove(targetIndex);
        }
    }

    public void AddMoleBlock(MoleBlock inBlock)
    {
        _moleBlocks.Add(inBlock);
    }

    public void RemoveMoleBlock(MoleBlock inBlock)
    {
        _moleBlocks.Remove(inBlock);
    }

    public bool IsMoleMoveNow()
    {
        return _movedTurn != InGameController.Instance.TurnCounter.RemainTurn;
    }


    public IEnumerator MoveMoles()
    {
        WaitForSignal signal = new WaitForSignal();

        BlockSlidingManager.it.StopAllBlock(BlockStopper.EStoppedBy.MOLE_BLOCK_MOVE, () => { signal.Signal(); });

        yield return signal.Wait();

        _movedTurn = InGameController.Instance.TurnCounter.RemainTurn;

        var isMoved = HideMoles();
        if (isMoved)
            yield return new WaitForSeconds(0.5f);

        CheckAddMoles();

        if (_moleBlocks.Count > 0)
        {
            Dictionary<MoleBlock, int> moleBlockNextIndexDic = GetMoleNextIndexSet();

            RemoveAtNextIndex(moleBlockNextIndexDic);

            yield return null;

            SetMoleToNextIndex(moleBlockNextIndexDic);

            yield return null;

            ShowMoles();

            yield return new WaitForSeconds(0.5f);
        }

        BlockSlidingManager.it.SlideAllStoppedBlock(BlockStopper.EStoppedBy.MOLE_BLOCK_MOVE);
    }

    private bool HideMoles()
    {
        bool isMoved = false;
        _moleBlocks.ForEach(m => 
        {
            if (LayerManager.it.ObstacleLayer.HasLockingBlockObstacleAt(m.Index))
                return;

            m.Hide();
            _tunnels.Find(t => t.Index == m.Index)?.Hide();
            LayerManager.it.BlockLayer.SetNullAt(m.Index, m);

            isMoved = true;
        });

        if (isMoved)
            SoundManager.Instance.PlaySFX("ingame_obj_mole_move_in");

        return isMoved;
    }

    private void CheckAddMoles()
    {
        if (false == MissionManager.it.GetMissionTypes().Contains((int)EBlockType.MOLE_BLOCK))
            return;

        if (MissionManager.it.CheckItemAllCollected((int)EBlockType.MOLE_BLOCK))
            return;

        int currentMolesCount = _moleBlocks.Count;

        int remainMissionCount = MissionManager.it.GetRemainItemCount((int)EBlockType.MOLE_BLOCK);
        if (remainMissionCount <= currentMolesCount)
            return;

        int maxCount = StageDataManager.it.StageData.MissionData.GetMaxBlockCount((int)EBlockType.MOLE_BLOCK);
        var availableIndice = GetAvailableTunnelIndice();
        maxCount = Mathf.Min(maxCount, availableIndice.Count);

        if (currentMolesCount >= maxCount)
            return;

        int addCount = Mathf.Min(remainMissionCount, maxCount) - currentMolesCount;

        for (int i = 0; i < addCount; i++)
        {
            var targetIndex = RandomUtil.GetItemByRandom(availableIndice);

            AddOneMole(targetIndex);

            availableIndice.Remove(targetIndex);
        } 
    }

    private Block AddOneMole(int targetIndex)
    {
        return InGameObjectPoolManager.it.GetBlockAt(targetIndex, (int)EBlockType.MOLE_BLOCK);
    }

    private List<int> GetAvailableTunnelIndice()
    {
        List<int> tunnelIndice = new List<int>();
        foreach (var tunnel in _tunnels)
            tunnelIndice.Add(tunnel.Index);

        tunnelIndice.RemoveAll(index =>
        {
            return IsAvailableIndex(index) == false;
        });

        return tunnelIndice;
    }

    private bool IsAvailableIndex(int inNextIndex)
    {
        if (LayerManager.it.ObstacleLayer.HasLockingBlockObstacleAt(inNextIndex))
            return false;

        var b = LayerManager.it.BlockLayer.GetBlockAt(inNextIndex);
        if (b != null)
            return EBlockTypeUtil.IsNormalBlock(b.Type);

        return true;
    }

    private void RemoveAtNextIndex(Dictionary<MoleBlock, int> moleBlockNextIndexDic)
    {
        foreach (var moleNextIndexSet in moleBlockNextIndexDic)
        {
            var nextIndex = moleNextIndexSet.Value;

            InGameController.Instance.Stable.SetAsBusy(nextIndex, true, EBlockBusyBy.MOLE_BLOCK_MOVED);

            Block block = LayerManager.it.BlockLayer.GetBlockAtOrMovingTo(nextIndex);
            if (block != null)
            {
                BlockRemovalManager.it.RemoveAt(nextIndex, (int)EBlockRemovalType.REMOVE_ONLY_AT);
                LayerManager.it.BlockLayer.SetNullAt(nextIndex, block);
            }
        }
    }

    private void SetMoleToNextIndex(Dictionary<MoleBlock, int> moleBlockNextIndexDic)
    {
        foreach (var moleNextIndexSet in moleBlockNextIndexDic)
        {
            var nextIndex = moleNextIndexSet.Value;
            var moleBlock = moleNextIndexSet.Key;
            moleBlock.Index = nextIndex;
            moleBlock.transform.localPosition = Coordinate.ToBlockPosition(nextIndex);
            LayerManager.it.BlockLayer.SetBlockAt(nextIndex, moleBlock);

            InGameController.Instance.Stable.SetAsBusy(nextIndex, false, EBlockBusyBy.MOLE_BLOCK_MOVED);
        }

    }

    private Dictionary<MoleBlock, int> GetMoleNextIndexSet()
    {
        Dictionary<MoleBlock, int> moleBlockNextIndexDic = new Dictionary<MoleBlock, int>();
        List<MoleBlock> waitingMoles = new List<MoleBlock>(_moleBlocks);

        List<int> tunnelIndice = GetAvailableTunnelIndice();

        SetPrimaryTarget(moleBlockNextIndexDic, waitingMoles, tunnelIndice);

        SetSecondaryTarget(moleBlockNextIndexDic, waitingMoles, tunnelIndice);

        SetToAnotherPlace(moleBlockNextIndexDic, waitingMoles, tunnelIndice);

        SetRemainMolesNextIndex(moleBlockNextIndexDic, waitingMoles, tunnelIndice);

        return moleBlockNextIndexDic;
    }

    /// <summary>
    /// 1순위 타겟설정 (주변을 타격하는 특수블록 근처로 이동)
    /// </summary>
    /// <param name="nextIndexDic"></param>
    /// <param name="waitingMoles"></param>
    /// <param name="tunnelIndice"></param>
    private void SetPrimaryTarget(Dictionary<MoleBlock, int> nextIndexDic, List<MoleBlock> waitingMoles, List<int> tunnelIndice)
    {
        List<int> primaryIndice = GetPrimaryTargetIndex(tunnelIndice);

        while (waitingMoles.Count > 0 && primaryIndice.Count > 0)
        {
            var mole = RandomUtil.GetItemByRandom(waitingMoles);
            var targetIndex = RandomUtil.GetItemByRandom(primaryIndice);

            nextIndexDic.Add(mole, targetIndex);

            tunnelIndice.Remove(targetIndex);
            primaryIndice.Remove(targetIndex);
            waitingMoles.Remove(mole);
        }
    }


    /// <summary>
    /// 특수블록 근처 인덱스 
    /// </summary>
    /// <param name="inAvailableIndice"></param>
    /// <returns></returns>
    private List<int> GetPrimaryTargetIndex(List<int> inAvailableIndice)
    {
        List<int> result = new List<int>();

        foreach (var tunnelIndex in inAvailableIndice)
        {
            var adjacentIndice = IndexUtil.GetAdjacentIndice(tunnelIndex, false);

            foreach (var targetIndex in adjacentIndice)
            {
                var b = LayerManager.it.BlockLayer.GetBlockAt(targetIndex);
                if (b == null)
                    continue;

                if (IsMoleLikeSpecialType(b.Type))
                {
                    result.Add(tunnelIndex);
                    break;
                }
                    
            }
        }

        return result;
    }

    /// <summary>
    /// 바로옆을 타격할 수 있는 특수블록 목록
    /// </summary>
    /// <param name="blockType"></param>
    /// <returns></returns>
    private bool IsMoleLikeSpecialType(int blockType)
    {
        switch ((EBlockType)blockType)
        {
            case EBlockType.SPECIAL_MATCH_4_V:
            case EBlockType.SPECIAL_MATCH_4_H:
            case EBlockType.SPECIAL_MATCH_5_V:
            case EBlockType.SPECIAL_MATCH_5_H:
            case EBlockType.SPECIAL_MATCH_LT:
            case EBlockType.SPECIAL_CROSS:
            case EBlockType.SPECIAL_CLEANER:
            case EBlockType.SPECIAL_CLEANER_BOMB:
            case EBlockType.SPECIAL_CRABOO:
            case EBlockType.SPECIAL_SKUNK:
            case EBlockType.SPECIAL_TACO_V:
            case EBlockType.SPECIAL_TACO_H:
            case EBlockType.SPECIAL_PIZZA:
            case EBlockType.SPECIAL_RANGER:
            case EBlockType.SPECIAL_BURRITO:
            case EBlockType.SAND_CASTLE_STARFISH:
                return true;
        }

        return false;
    }

    /// <summary>
    /// 주변 8방향에 두더지와 같은 색상의 기본블록이 기준치이상 있을경우 두번째 타겟으로 추가
    /// </summary>
    /// <param name="inAvailableIndice"></param>
    /// <param name="moleType"></param>
    /// <returns></returns>
    private int SAME_COLOR_MIN_COUNT = 5;

    private  void SetSecondaryTarget(Dictionary<MoleBlock, int> nextIndexDic, List<MoleBlock> waitingMoles, List<int> tunnelIndice)
    {
        List<MoleBlock> secondaryMoles = new List<MoleBlock>(waitingMoles);

        while (secondaryMoles.Count > 0)
        {
            var mole = RandomUtil.GetItemByRandom(secondaryMoles);
            List<int> targetIndice = GetSecondaryTargetIndex(tunnelIndice, mole.ColorType);

            if (targetIndice.Count > 0)
            {
                int targetIndex = RandomUtil.GetItemByRandom(targetIndice);

                waitingMoles.Remove(mole);
                tunnelIndice.Remove(targetIndex);
                nextIndexDic.Add(mole, targetIndex);
            }

            secondaryMoles.Remove(mole);
        }
    }

    private List<int> GetSecondaryTargetIndex(List<int> inAvailableIndice, int moleColorType)
    {
        List<int> result = new List<int>();

        foreach (var tunnelIndex in inAvailableIndice)
        {
            var adjacentIndice = IndexUtil.GetAdjacentIndice(tunnelIndex, false);

            List<int> sameColorIndice = new List<int>();
            foreach (var targetIndex in adjacentIndice)
            {
                var b = LayerManager.it.BlockLayer.GetBlockAt(targetIndex);
                if (b == null)
                    continue;

                if (moleColorType == b.Type)
                    sameColorIndice.Add(targetIndex);
            }

            
            if (sameColorIndice.Count >= SAME_COLOR_MIN_COUNT)
                result.Add(tunnelIndex);
        }

        return result;
    }

    /// <summary>
    /// 최대한 두더지가 자기 자리로 돌아오는걸 방지
    /// </summary>
    private void SetToAnotherPlace(Dictionary<MoleBlock, int> nextIndexDic, List<MoleBlock> waitingMoles, List<int> tunnelIndice)
    {
        List<MoleBlock> moles = new List<MoleBlock>(waitingMoles);

        while (moles.Count > 0)
        {
            var mole = RandomUtil.GetItemByRandom(moles);
            List<int> targetIndice = new List<int>(tunnelIndice);

            targetIndice.RemoveAll(t => t == mole.Index);

            if (targetIndice.Count > 0)
            {
                int targetIndex = RandomUtil.GetItemByRandom(targetIndice);

                waitingMoles.Remove(mole);
                tunnelIndice.Remove(targetIndex);
                nextIndexDic.Add(mole, targetIndex);
            }

            moles.Remove(mole);
        }
    }

    private void SetRemainMolesNextIndex(Dictionary<MoleBlock, int> nextIndexDic, List<MoleBlock> waitingMoles, List<int> tunnelIndice)
    {
        // 남은 블록 타겟설정
        while (waitingMoles.Count > 0 && tunnelIndice.Count > 0)
        {
            var mole = RandomUtil.GetItemByRandom(waitingMoles);
            int targetIndex = RandomUtil.GetItemByRandom(tunnelIndice);

            waitingMoles.Remove(mole);
            tunnelIndice.Remove(targetIndex);

            nextIndexDic.Add(mole, targetIndex);
        }
    }

    private void ShowMoles()
    {
        _moleBlocks.ForEach(m =>
        {
            m.Out();
            _tunnels.Find(t => t.Index == m.Index)?.Out();
        });

        SoundManager.Instance.PlaySFX("ingame_obj_mole_move_out");
    }

    public override void Clear()
    {
        base.Clear();

        ClearData();
    }

    public override void Dispose()
    {
        base.Dispose();

        ClearData();
    }

    private void ClearData()
    {
        _moleBlocks?.Clear();
        _moleBlocks = null;

        _tunnels?.Clear();
        _tunnels = null;

        _moleColorTypes?.Clear();
        _moleColorTypes = null;
    }
}
