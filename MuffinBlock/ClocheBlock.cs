using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class ClocheBlock : Block
{
    [SerializeField]
    Animator TableAnimator;

    [SerializeField]
    SpriteRenderer TableClothRenderer;

    [SerializeField]
    List<Animator> MuffinList;

    private MuffinBlockData _muffinBlockData;

    public bool IsContainInGroup(int inIndex) => _muffinBlockData.GroupIndice.Contains(inIndex);

    private int _curMuffinCount = 0;

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

        _isGettingAttack = false;

        SetMuffinData();

        AddBlockToController();

        InitObject();

        SetTableCloth();
    }

    private void SetMuffinData()
    {
        _muffinBlockData = StageDataManager.it.StageData.MissionObjectData.GetMuffinBlockDataAt(OriginIndex);

        if (_muffinBlockData == null)
            _muffinBlockData = new MuffinBlockData(_index);

        _curMuffinCount = _muffinBlockData.MuffinCount;
    }

    private void AddBlockToController()
    {
        InGameController.Instance.MissionBlock.Muffin.AddClocheBlock(this);
    }

    private void InitObject()
    {
        _animator.Animate("idle1");
        TableAnimator.Play("idle1");

        SetMuffinObject();
    }

    private void SetMuffinObject()
    {
        for (int i = 0; i < MuffinList.Count; i++)
        {
            var renderer = MuffinList[i].GetComponentInChildren<SpriteRenderer>();;

            if (renderer != null)
                renderer.enabled = i < _curMuffinCount;

            if (i < _curMuffinCount)
                MuffinList[i].Play("idle1");
        }
    }

    private void SetTableCloth()
    {
        if (_muffinBlockData == null)
            return;

        TableClothRenderer.transform.parent.localRotation = Quaternion.identity;

        string spriteName = string.Empty;
        if (_muffinBlockData.GroupIndice.Count <= 1)
        {
            spriteName = "muffin_tile_main";
        }
        else
        {
            var currentBlockOrder = _muffinBlockData.GroupIndice.IndexOf(_index);
            var lastIndex = _muffinBlockData.GroupIndice.Count - 1;

            if (currentBlockOrder == 0)
            {
                var dir = EDirectionUtil.GetDirectionAToB(_index, _muffinBlockData.GroupIndice[1]);

                switch ((EDirection)dir)
                {
                    case EDirection.RIGHT:
                        spriteName = "muffin_tile_LD";
                        break;
                    case EDirection.BOTTOM:
                        spriteName = "muffin_tile_RU";
                        SetTableClothRotation(90f);
                        break;

                }
            }
            else if (currentBlockOrder == lastIndex)
            {
                var dir = EDirectionUtil.GetDirectionAToB(_index, _muffinBlockData.GroupIndice[currentBlockOrder - 1]);

                switch ((EDirection)dir)
                {
                    case EDirection.LEFT:
                        spriteName = "muffin_tile_RU";
                        break;
                    case EDirection.TOP:
                        spriteName = "muffin_tile_LD";
                        SetTableClothRotation(90f);
                        break;
                }
            }
            else
            {
                var dir = EDirectionUtil.GetDirectionAToB(_index, _muffinBlockData.GroupIndice[currentBlockOrder + 1]);

                spriteName = "muffin_tile_C";

                switch ((EDirection)dir)
                {
                    case EDirection.BOTTOM:
                        SetTableClothRotation(90f);
                        break;
                }
            }
        }

        TableClothRenderer.sprite = InGameResourceManager.it.GetSprite(spriteName);
    }

    private void SetTableClothRotation(float zValue)
    {
        TableClothRenderer.transform.parent.localRotation = Quaternion.Euler(0, 0, zValue);
    }

    public override void GetAttacked(BlockRemovalInfo inSource = null)
    {
        if (_isGettingAttack)
            return;

        switch (_type)
        {
            case (int)EBlockType.MUFFIN_CLOCHE_LV_3:
                StartCoroutine(Co_OpenCloche());
                break;

            case (int)EBlockType.MUFFIN_CLOCHE_LV_2:
                ThrowMuffins();
                break;
        }
    }

    private const float LID_OPEN_ANIM_DURATION = 0.5F;
    
    private IEnumerator Co_OpenCloche()
    {
        _isGettingAttack = true;

        SoundManager.Instance.PlaySFX("ingame_obj_muffin_cloche");

        EffectPool.it.Play((int)EInGameEffect.MUFFIN_CLOCHE_REMOVE, LayerManager.it.EffectLayer.transform, Coordinate.ToBlockPosition(_index),"hit1");

        _animator.Animate("hit1");

        yield return new WaitForSeconds(LID_OPEN_ANIM_DURATION);

        _isGettingAttack = false;

        if (_curMuffinCount > 0)
            _type = (int)EBlockType.MUFFIN_CLOCHE_LV_2;
        else
        {
            _type = (int)EBlockType.MUFFIN_CLOCHE_LV_1;
            InGameController.Instance.MissionBlock.Muffin.CheckClocheRemove(this);
        }
            
    }

    private void ThrowMuffins()
    {
        _isGettingAttack = true;

        EffectPool.it.Play((int)EInGameEffect.MUFFIN_CLOCHE_REMOVE, LayerManager.it.EffectLayer.transform, Coordinate.ToBlockPosition(_index), "hit2");

        _animator.Animate("hit2");

        --_curMuffinCount;

        RemoveMuffinAt(_curMuffinCount);

        BlockSlidingManager.it.StopAllBlock(BlockStopper.EStoppedBy.MUFFIN_CLOCHE, OnStopAllBlock);
    }

    private void RemoveMuffinAt(int inIndex)
    {
        if (MuffinList.Count > inIndex)
            MuffinList[inIndex]?.Play("hit1");
    }

    private void OnStopAllBlock()
    {
        if (_curMuffinCount <= 0)
            _type = (int)EBlockType.MUFFIN_CLOCHE_LV_1;

        _isGettingAttack = false;

        InGameController.Instance.MissionBlock.Muffin.CreateMuffinBlock(this, 1, _muffinBlockData.MuffinTargetIndice);
    }

    public override void Remove(BlockRemovalInfo inSource = null)
    {
        TableAnimator.Play("remove1");
        _animator.Animate("remove1");

        SlideOnThisAndPushBack(GameConfig.DELAY_SLIDING_BLOCK, 0.83f);
    }

    public override bool IsRemovableNow(BlockRemovalInfo inSource = null)
    {
        if (_type == (int)EBlockType.MUFFIN_CLOCHE_LV_1)
        {
            return inSource != null && inSource.Type == (int)EBlockRemovalType.MUFFIN_CLOCHE_REMOVING;
        }
        else
            return true;

        
    }
}
