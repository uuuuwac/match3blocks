using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class MuffinBlock : Block
{
    private static readonly float FLYING_TIME = 0.6f;

    public override void Init(int inIndex, int inType)
    {
        InitData(inIndex, inType);
    }

    public override void ReInit(int inIndex, int inType)
    {
        InitData(inIndex, inType);
    }

    protected override void InitData(int inIndex, int inType)
    {
        base.InitData(inIndex, inType);

        Animate((int)EBlockAnimationType.IDLE);
    }

    public void MoveMuffin(int inTargetIndex)
    {
        Vector3 pos = Coordinate.ToBlockPosition(inTargetIndex);
        int jumpHeight = RandomUtil.GetInt(20, 40) + (int)(_index / StageData.COL_COUNT - StageData.COL_COUNT * 0.5) * 5;

        transform.DOLocalJump(new Vector3(pos.x, pos.y, pos.z), jumpHeight, 1, FLYING_TIME).OnComplete(() =>
        {
            OnArriveAtDest(inTargetIndex);

        }).SetEase(Ease.Linear);
    }

    private void OnArriveAtDest(int inTargetIndex)
    {
        _index = inTargetIndex;

        LayerManager.it.BlockLayer.RemoveBlockAtWithoutAnimation(_index, () =>
        {
            InGameController.Instance.Stable.SetAsBusy(_index, false, EBlockBusyBy.MUFFIN_CREATING);

            this.transform.SetParent(LayerManager.it.BlockLayer.transform);
            this.transform.localPosition = Coordinate.ToBlockPosition(_index);

            LayerManager.it.BlockLayer.SetBlockAt(_index, this);

            BlockSlidingManager.it.SlideBlock(this);
            
            InGameController.Instance.MissionBlock.Muffin.OnMuffinArrive(this);
        });
    }

    public void StopMoving()
    {
        transform.DOKill();
    }

    public override void FlyToMissionUI()
    {
        LayerManager.it.BlockLayer.SetNullAt(Index, null, true);

        SoundManager.Instance.PlaySFX("ingame_obj_muffin_collect");

        base.FlyToMissionUI();
    }

    public override void Remove(BlockRemovalInfo inSource = null)
    {
        SoundManager.Instance.PlaySFX("ingame_obj_muffin_remove");

        Animate(BlockRemovalManager.it.GetRemovalAnimationType(inSource, _index));

        EffectPool.it.Play((int)EInGameEffect.MUFFIN_BLOCK_REMOVE, LayerManager.it.EffectLayer.transform, Coordinate.ToBlockPosition(_index));

        SlideOnThisAndPushBack(GameConfig.DELAY_SLIDING_BLOCK, 0f);
    }

    public override void Animate(int inAnimationType)
    {
        _animator.Animate(GetLabel(inAnimationType));
    }

    private string GetLabel(int inType)
    {
        return $"FX1_block1_{EBlockAnimationTypeUtil.GetLabel(inType)}";
    }
}
