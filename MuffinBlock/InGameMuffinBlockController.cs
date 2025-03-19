using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Spine.Unity;

public class InGameMuffinBlockController : InGameControllerBase
{
    private List<int> _reservedIndice;
    
    private List<ClocheBlock> _clocheBlocks;

    private List<MuffinBlock> _flyingMuffins;

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
        _clocheBlocks = new List<ClocheBlock>();

        _reservedIndice = new List<int>();
        _flyingMuffins = new List<MuffinBlock>();
    }

    public override void Clear()
    {
        ClearData();

        base.Clear();
    }

    public override void Dispose()
    {
        ClearData();

        base.Dispose();
    }

    private void ClearData()
    {
        _clocheBlocks?.Clear();
        _clocheBlocks = null;

        _reservedIndice?.Clear();
        _reservedIndice = null;

        if (_flyingMuffins != null)
        {
            for (int i = 0; i < _flyingMuffins.Count; ++i)
            {
                _flyingMuffins[i].StopMoving();
                InGameObjectPoolManager.it.PushBlock(_flyingMuffins[i]);
            }

            _flyingMuffins.Clear();
            _flyingMuffins = null;
        }
    }

    public void AddClocheBlock(ClocheBlock inBlock)
    {
        if (_clocheBlocks == null)
            _clocheBlocks = new List<ClocheBlock>();

        if (_clocheBlocks.Contains(inBlock))
            return;

        _clocheBlocks?.Add(inBlock);
    }



    public void CreateMuffinBlock(ClocheBlock inBlock, int inCount, List<int> targetIndice)
    {
        List<int> targets = new List<int>();

        targetIndice?.ShuffleList();

        foreach (var i in targetIndice)
        {
            if (inCount <= targets.Count)
                break;

            bool targetAvailable = InGameController.Instance.PoppingBlockController.CheckInvalidTarget((int)EBlockType.MUFFIN_BLOCK, i, _reservedIndice);

            if (targetAvailable)
                targets.Add(i);
        }

        if (targets.Count <= 0)
            targets = InGameController.Instance.PoppingBlockController.GetTargetIndice((int)EBlockType.MUFFIN_BLOCK, inCount, _reservedIndice);

        for (int i = 0; i < targets.Count; ++i)
        {
            var muffin = GetMuffinBlock(inBlock.Index);
            if (muffin == null)
                return;

            InGameController.Instance.Stable.SetAsBusy(targets[i], true, EBlockBusyBy.MUFFIN_CREATING);

            muffin.MoveMuffin(targets[i]);

            _reservedIndice.Add(targets[i]);

            _flyingMuffins.Add(muffin);
        }

        SoundManager.Instance.PlaySFX("ingame_obj_muffin_spread");

        if (inBlock.Type == (int)EBlockType.MUFFIN_CLOCHE_LV_1)
            CheckClocheRemove(inBlock);
    }

    private MuffinBlock GetMuffinBlock(int inIndex)
    {
        var muffin = InGameObjectPoolManager.it.GetBlockAt(inIndex, (int)EBlockType.MUFFIN_BLOCK);

        muffin.transform.SetParent(LayerManager.it.EffectLayer.transform);
        muffin.transform.localPosition = Coordinate.ToBlockPosition(inIndex);

        return muffin as MuffinBlock;
    }


    public void CheckClocheRemove(ClocheBlock inBlock)
    {
        var sameGroupBlock = GetSameGroupCloche(inBlock.OriginIndex);

        if (CheckAllMuffinPopped(sameGroupBlock))
            RemoveCloche(sameGroupBlock);
    }

    private List<ClocheBlock> GetSameGroupCloche(int inIndex)
    {
        var sameGroupBlock = new List<ClocheBlock>();

        foreach (var c in _clocheBlocks)
        {
            if (c.IsContainInGroup(inIndex))
                sameGroupBlock.Add(c);
        }
        return sameGroupBlock;
    }

    private bool CheckAllMuffinPopped(List<ClocheBlock> inBlocks)
    {
        foreach (var c in inBlocks)
        {
            if (c.Type != (int)EBlockType.MUFFIN_CLOCHE_LV_1)
                return false;
        }

        return true;
    }

    private void RemoveCloche(List<ClocheBlock> inBlocks)
    {
        SoundManager.Instance.PlaySFX("ingame_obj_muffin_plate");

        foreach (var b in inBlocks)
        {
            BlockRemovalManager.it.RemoveAt(b.Index, (int)EBlockRemovalType.MUFFIN_CLOCHE_REMOVING);
        }
    }

    public void OnMuffinArrive(MuffinBlock muffin)
    {
        _reservedIndice?.Remove(muffin.Index);
        _flyingMuffins?.Remove(muffin);

        BlockSlidingManager.it.SlideAllStoppedBlock(BlockStopper.EStoppedBy.MUFFIN_CLOCHE);
    }

}
