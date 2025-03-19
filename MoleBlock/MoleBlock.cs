using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;

public class MoleBlock : Block
{
    [SerializeField]
    private SkeletonAnimation _skeleton;

    public int ColorType => _colorType;
    private int _colorType = -1;

    private bool _isMovingNow = false;

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

        _skeleton.GetComponent<MeshRenderer>().sortingLayerName = SortingLayerName.DEFAULT;

        _isGettingAttack = false;
        _isMovingNow = false;

        InGameController.Instance.MissionBlock.Mole.AddMoleBlock(this);

        SetType();

        _skeleton.AnimationState.ClearTracks();
        _skeleton.AnimationState.SetAnimation(0, "idle1", true);
    }

    private void SetType()
    {
        _colorType = InGameController.Instance.MissionBlock?.Mole?.GetColorType(Index) ?? 1;

        SetSkin();
    }

    private void SetSkin()
    {
        _skeleton.Skeleton.SetSkin($"skin{_colorType.ToString()}");
        _skeleton.Skeleton.SetSlotsToSetupPose();
        _skeleton.LateUpdate();
    }

    public override bool IsRemovableNow(BlockRemovalInfo inSource = null)
    {
        if (_isGettingAttack || _isMovingNow)
            return false;

        if (inSource != null
            && inSource.AroundAttackRemovalInfo != null)
        {
            return IsAttackedByNormalBlock(inSource);
        }

        return base.IsRemovableNow(inSource);
    }

    private static float REMOVE_ANIM_DURATION = 1f;

    public override void Remove(BlockRemovalInfo inSource = null)
    {
        if (_isGettingAttack || _isMovingNow)
            return;

        CatchMole();

        SlideOnThisAndPushBack(GameConfig.DELAY_SLIDING_BLOCK, REMOVE_ANIM_DURATION);
    }

    public override void FlyToMissionUI()
    {
        if (_isGettingAttack || _isMovingNow)
            return;

        StartCoroutine(_FlyToMissionUI());
    }

    private void CatchMole()
    {
        _isGettingAttack = true;

        InGameController.Instance.MissionBlock.Mole.RemoveMoleBlock(this);

        Vector3 origin = this.transform.localPosition;
        this.transform.SetParent(LayerManager.it.EffectLayer.transform);
        this.transform.localPosition = origin;

        _skeleton.GetComponent<MeshRenderer>().sortingLayerName = SortingLayerName.EFFECT;

        _skeleton.Play("remove1", false);

        SoundManager.Instance.PlaySFX("ingame_obj_mole_hit");

    }

    private IEnumerator _FlyToMissionUI()
    {
        CatchMole();

        SlideOnThis(GameConfig.DELAY_SLIDING_BLOCK);

        yield return new WaitForSeconds(REMOVE_ANIM_DURATION);

        _skeleton.Play("move1", false);

        base.FlyToMissionUI();
    }

    private bool IsAttackedByNormalBlock(BlockRemovalInfo inSource)
    {
        var list = inSource.AroundAttackRemovalInfo.Indice;
        for (int i = 0; i < list.Count; i++)
        {
            var block = LayerManager.it.BlockLayer.GetBlockAtOrMovingTo(list[i]);
            if (block != null && _colorType == block.Type)
            {
                return true;
            }
        }

        return false;
    }

    public void Hide()
    {
        _isMovingNow = true;
        _skeleton.Play("in3", false, OnCompleteAnim);
    }

    public void Out()
    {
        _isMovingNow = true;
        _skeleton.Play("out3", false, OnCompleteAnim);
    }

    private void OnCompleteAnim()
    {
        _isMovingNow = false;
    }

    public override void Animate(int inAnimationType)
    {
        if (inAnimationType == (int)EBlockAnimationType.SETTLE)
            _skeleton.Play("settle1", false);

    }

}
