using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;
using DG.Tweening;

public class SquirrelBlock : Block
{
    public enum SquirrelMoveState
    {
        Ready = 0,    // 움직임 체크 전
        Stay = 1,    // 제자리 (다음 길이 없음)
        Blocked = 2,    // 다음 길이 있지만 막혀있음
        Move = 3,    // 다음 길이 있고 움직일 수 있는상태

    }

    [SerializeField]
    private SkeletonAnimation _skeleton;

    private SquirrelBlockData _blockData;
    public SquirrelMoveState MoveState { get; set; }

    private int _pathIndex = 0;
    private bool _isInverted = false;

    public bool IsMovingNow = false;

    private Coroutine _animCoroutine = null;

    public int NextTurnIndex = -1;

    private MeshRenderer _renderer;
    private MaterialPropertyBlock _propertyBlock;

    private void Awake()
    {
        _renderer = _skeleton.GetComponent<MeshRenderer>();
        _propertyBlock = new MaterialPropertyBlock();
    }

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

        if (_animCoroutine != null)
            StopCoroutine(_animCoroutine);

        _animCoroutine = null;

        _skeleton.GetComponent<MeshRenderer>().sortingLayerName = SortingLayerName.DEFAULT;

        _blockData = StageDataManager.it.StageData.MissionObjectData?.SquirrelBlockDatas?.Find(d => d.Index == Index) ?? null;

        if (_blockData == null)
            _blockData = new SquirrelBlockData(Index, 0, new List<int>(), new List<int>());

        InGameController.Instance.MissionBlock.Squirrel.AddBlock(this);

        MoveState = SquirrelMoveState.Ready;

        _isInverted = false;
        IsMovingNow = false;
        _isGettingAttack = false;
        _pathIndex = 0;
        NextTurnIndex = -1;

        PlayIdleAnim();
    }

    public override void GetAttacked(BlockRemovalInfo inSource = null)
    {
        if (_isGettingAttack == true)
            return;

        StartCoroutine(ChangeColor());

        StartCoroutine(_GetAttacked(inSource));
    }

    private IEnumerator ChangeColor()
    {
        float duration = 0.1f;
        float deltaTime = 0f;

        int id = Shader.PropertyToID("_Color");

        Color color;

        float t = 0;
        while (deltaTime < duration)
        {
            t = duration - deltaTime / duration;

            color = new Color(1f, t, t);

            _propertyBlock.SetColor(id, color);
            _renderer.SetPropertyBlock(_propertyBlock);

            yield return null;

            deltaTime += Time.deltaTime;
        }

        duration = 0.3f;
        deltaTime = 0f;
        t = 0;

        while (deltaTime < duration)
        {
            t = deltaTime / duration;

            color = new Color(1f, t, t);

            _propertyBlock.SetColor(id, color);
            _renderer.SetPropertyBlock(_propertyBlock);

            yield return null;

            deltaTime += Time.deltaTime;
        }

        _propertyBlock.SetColor(id, Color.white);
        _renderer.SetPropertyBlock(_propertyBlock);

        //_skeleton.skeleton.SetColor(Color.white);

        //duration = 0.1f
        //deltaTime = 0f;

        //while (deltaTime < duration)
        //{
        //    color = new Color(deltaTime / duration, 0f, 0f);

        //    deltaTime += Time.deltaTime;

        //    _skeleton.skeleton.SetColor(color);

        //    yield return null;
        //}

    }

    private IEnumerator _GetAttacked(BlockRemovalInfo inSource)
    {
        base.GetAttacked(inSource);

        _isGettingAttack = true;
        IsMovingNow = false;

        int curType = _type;
        string animName = string.Empty;
        float delay = 0f;

        switch (curType)
        {
            case (int)EBlockType.SQUIRREL_BLOCK_LV_3:
                animName = "idle_3_hit";
                delay = 0.4333f;
                break;
            case (int)EBlockType.SQUIRREL_BLOCK_LV_2:
                animName = "idle_2_hit";
                delay = 0.5f;
                break;
        }

        _skeleton.AnimationState.ClearTracks();
        _skeleton.AnimationState.SetAnimation(0, animName, false);

        SoundManager.Instance.PlaySFX("ingame_obj_squirrel_1");

        yield return new WaitForSeconds(delay);

        _type = EBlockTypeUtil.GetNextTypeByAttack(_type);
        _isGettingAttack = false;

        PlayIdleAnim();
    }

    public override void Remove(BlockRemovalInfo inSource = null)
    {
        IsMovingNow = false;

        base.Remove(inSource);

        if (MissionManager.it.IsMissionItem(Type) 
            && LayerManager.it.InGameMissionUI.GetMissionUIItemCount(Type) > 0)
        {
            FlyToMissionUI();
            //SlideOnThis(BlockLayer.SLIDING_DELAY_TIME_TO_MISSION_BLOCK);

            SoundManager.Instance.PlaySFX("ingame_obj_squirrel_2");
            return;
        }

        if (_animCoroutine != null)
            StopCoroutine(_animCoroutine);

        StartCoroutine(ChangeColor());
        StartCoroutine(_Remove());
    }

    private IEnumerator _Remove()
    {
        _skeleton.AnimationState.ClearTracks();
        _skeleton.AnimationState.SetAnimation(0, "idle_1_hit", false);
        SoundManager.Instance.PlaySFX("ingame_obj_squirrel_1");

        yield return new WaitForSeconds(0.433f);

        Removed();
    }

    private void Removed()
    {
        this.transform.SetParent(LayerManager.it.EffectLayer.transform);

        LayerManager.it.BlockLayer.SetNullAt(Index, null, true);
        BlockSlidingManager.it.SlideBlockTo(Index);

        InGameController.Instance.MissionBlock.Squirrel.RemoveBlock(this);

        _skeleton.AnimationState.ClearTracks();
        _skeleton.AnimationState.SetAnimation(0, "right_run", true);

        SoundManager.Instance.PlaySFX("ingame_obj_squirrel_5");

        float duration = 2f;
        float dest = this.transform.localPosition.x + 1500f;

        this.transform.localPosition = this.transform.localPosition.SetZ(0f);

        this.transform.DOLocalMoveX(dest, duration)
            .SetEase(Ease.Linear)
            .onComplete += () => { InGameObjectPoolManager.it.PushBlock(this); };
    }

    public override void FlyToMissionUI()
    {
        LayerManager.it.BlockLayer.SetNullAt(Index, null, true);
        BlockSlidingManager.it.SlideBlockTo(Index);

        transform.SetParent(LayerManager.it.EffectLayer.transform);
        transform.localPosition = Coordinate.ToBlockPosition(_index);
        InGameController.Instance.MissionBlock.Squirrel.RemoveBlock(this);

        StartCoroutine(_Co_FlyToMissionUI());
    }


    private IEnumerator _Co_FlyToMissionUI()
    {
        _skeleton.AnimationState.ClearTracks();
        _skeleton.AnimationState.SetAnimation(0, "remove", false);

        Vector3 pos = LayerManager.it.InGameMissionUI.GetMissionUIPosition((int)EBlockType.SQUIRREL_BLOCK_LV_1);

        Vector3 blockPos = transform.localPosition;
        blockPos.z = LayerManager.it.InGameMissionUI.FlyingBlockCount * -1;
        transform.localPosition = blockPos;

        // magic number
        int jumpHeight = 40 + (int)(_index / StageData.COL_COUNT - StageData.COL_COUNT * 0.5) * 5;
        float time = Vector2.Distance(pos, transform.localPosition) / 700;

        //MissionManager.it.UpdateMission((int)EBlockType.SQUIRREL_BLOCK_LV_1, 1.0f + time);

        yield return new WaitForSeconds(1.0f);

        _skeleton.AnimationState.ClearTracks();
        _skeleton.AnimationState.SetAnimation(0, "move", false);

        //// 미션 패널 sortingLayer가 변경되는 경우가 있어 블록이 미션패널로 날아간 뒤 블록이 미션패널 뒤로 가리는 것을 방지
        //_changedRenderer = _animationHandler.ChangeSortingLayerInChildren(SortingLayerName.EFFECT);

        _skeleton.GetComponent<MeshRenderer>().sortingLayerName = SortingLayerName.EFFECT;

        LayerManager.it.InGameMissionUI.FlyingBlockCount++;

        transform.DOLocalJump(new Vector3(pos.x, pos.y, blockPos.z), jumpHeight, 1, time).SetEase(Ease.Linear);

        yield return new WaitForSeconds(time);

        SoundManager.Instance.PlaySFX("Mission_Block_Heart");

        transform.SetParent(LayerManager.it.InGameMissionUI.MissionItemUI.GetMissionItemTransform((int)EBlockType.SQUIRREL_BLOCK_LV_1));

        _skeleton.AnimationState.ClearTracks();
        _skeleton.AnimationState.SetAnimation(0, "settle", false);

        yield return new WaitForSeconds(0.7f);

        _skeleton.GetComponent<MeshRenderer>().sortingLayerName = SortingLayerName.DEFAULT;

        InGameObjectPoolManager.it.PushBlock(this);
    }

    private bool IsRandomMove()
    {
        return _blockData.PathIndice.Count <= 0;
    }

    public void SetNextTurnIndex()
    {
        MoveState = SquirrelMoveState.Ready;

        NextTurnIndex = GetNextIndex();
    }

    private int GetNextIndex()
    {
        int nextIndex = -1;

        if (IsRandomMove())
        {
            nextIndex = GetRandomIndex(Index);
            return nextIndex;
        }

        switch (_blockData.PathType)
        {
            case SquirrelBlockData.EPathType.Oneway:
                if (_blockData.PathIndice.Count - 1 > _pathIndex)
                {
                    nextIndex = _blockData.GetPathIndex(_pathIndex + 1);
                }
                break;
            case SquirrelBlockData.EPathType.Return:
                if (_isInverted == false)
                {
                    nextIndex = _blockData.GetPathIndex(_pathIndex + 1);
                }
                else
                {
                    nextIndex = _blockData.GetPathIndex(_pathIndex - 1);
                }

                break;
            case SquirrelBlockData.EPathType.Loop:
                if (_blockData.PathIndice.Count > _pathIndex)
                {
                    nextIndex = _blockData.GetPathIndex(_pathIndex + 1);
                }

                break;
        }

        return nextIndex;
    }

    private int GetRandomIndex(int inIndex)
    {
        List<int> nextIndice = new List<int>();

        nextIndice = IndexUtil.GetCross4Indice(inIndex);

        nextIndice.RemoveAll(n => !IsMoveableIndex(n) || _blockData.BlockedIndice.Contains(n));

        if (nextIndice.Count <= 0)
            return -1;

        return RandomUtil.GetItemByRandom(nextIndice);
    }

    public SquirrelMoveState InvokeSquirrelMove()
    {
        if (MoveState != SquirrelMoveState.Stay
            && NextTurnIndex != -1)
        {
            var dir = (EDirection)EDirectionUtil.GetDirectionAToB(Index, NextTurnIndex);

            switch (MoveState)
            {
                case SquirrelMoveState.Blocked:
                    PlayBlockedAnim(dir);
                    break;

                case SquirrelMoveState.Move:
                    ChangePathIndex();
                    StartCoroutine(_MoveSquirrel(dir, NextTurnIndex));
                    break;

                default:
                    break;
            }

            NextTurnIndex = -1;
        }

        return MoveState;
    }

    private void ChangePathIndex()
    {
        switch (_blockData.PathType)
        {
            case SquirrelBlockData.EPathType.Oneway:
                {
                    ++_pathIndex;
                }
                break;

            case SquirrelBlockData.EPathType.Return:
                if (_isInverted == false)
                {
                    ++_pathIndex;

                    if (_blockData.PathIndice.Count - 1 == _pathIndex)
                        _isInverted = true;
                }
                else
                {
                    --_pathIndex;

                    if (0 == _pathIndex)
                        _isInverted = false;
                }

                break;

            case SquirrelBlockData.EPathType.Loop:
                {
                    ++_pathIndex;

                    if (_blockData.PathIndice.Count - 1 <= _pathIndex)
                        _pathIndex = -1;
                }
                break;
        }
    }

    private IEnumerator _MoveSquirrel(EDirection dir, int nextIndex)
    {
        PrepareToMove(nextIndex);

        yield return null;

        // 꼭 한 프레임 쉬어줘야함 안그러면 다람쥐가 떨어지는 블록에 없어짐
        BlockSlidingManager.it.SlideBlockTo(Index);

        PlayMoveAnim(dir);

        PlayTweenBlockPosition(nextIndex);

        yield return StartCoroutine(RemoveNextBlock(nextIndex));

        OnFinishMove();

        // 다람쥐가 밟아서 제거한 블록의 블록제거 효과가 
        // 다람쥐 블록위를 가리지 않게 딜레이 후 블록 레이어로 변경
        yield return new WaitForSeconds(0.2f);

        SetToBlockLayer();
    }

    private void PrepareToMove(int nextIndex)
    {
        IsMovingNow = true;

        transform.SetParent(LayerManager.it.EffectLayer.transform);
        //transform.localPosition = Coordinate.ToBlockPosition(_index);

        _skeleton.GetComponent<MeshRenderer>().sortingLayerName = SortingLayerName.EFFECT;

        InGameController.Instance.Stable.SetAsBusy(nextIndex, true, EBlockBusyBy.SQUIRREL_MOVE);

        LayerManager.it.BlockLayer.SetNullAt(Index, null, true);
        
    }

    private const float DELAY_BEFORE_REMOVE_BLOCK = 0.1f;
    private const float DELAY_AFTER_REMOVE_BLOCK = 0.4f;

    private IEnumerator RemoveNextBlock(int nextIndex)
    {
        yield return new WaitForSeconds(DELAY_BEFORE_REMOVE_BLOCK);

        float removeDelay = 0f;

        Block nextBlock = LayerManager.it.BlockLayer.GetBlockAtOrMovingTo(nextIndex);
        if (nextBlock != null)
        {
            BlockRemovalManager.it.RemoveAt(nextIndex, (int)EBlockRemovalType.REMOVED_BY_SQUIRREL);

            float marked = Time.realtimeSinceStartup;

            if (nextBlock.Type == (int)EBlockType.SPECIAL_RAINBOW)
            {
                while (nextBlock != null && nextBlock.Index >= 0)
                    yield return null;
            }

            removeDelay = Time.realtimeSinceStartup - marked;

            LayerManager.it.BlockLayer.SetNullAt(nextIndex, null, true);
        }

        SetSquirrelAtNextIndex(nextIndex);
       
        if (nextBlock != null && nextBlock.IsSpecialBlock())
            SquirrelHitBySpecialBlock(nextBlock);

        float afterRemoveDelay = removeDelay >= DELAY_AFTER_REMOVE_BLOCK
                                ? 0 : DELAY_AFTER_REMOVE_BLOCK - removeDelay;

        yield return new WaitForSeconds(afterRemoveDelay);
    }

    private void SetSquirrelAtNextIndex(int nextIndex)
    {
        Index = nextIndex;
        LayerManager.it.BlockLayer.SetBlockAt(nextIndex, this);
        InGameController.Instance.Stable.SetAsBusy(nextIndex, false, EBlockBusyBy.SQUIRREL_MOVE);
    }

    private void SquirrelHitBySpecialBlock(Block inBlock)
    {
        switch ((EBlockType)Type)
        {
            case EBlockType.SQUIRREL_BLOCK_LV_2:
            case EBlockType.SQUIRREL_BLOCK_LV_3:
                {
                    GetAttacked();
                }
                break;

            case EBlockType.SQUIRREL_BLOCK_LV_1:
                BlockRemovalManager.it.RemoveAt(Index, (int)EBlockRemovalType.REMOVE_ONLY_AT);
                break;
        }
    }

    private void OnFinishMove()
    {
        IsMovingNow = false;
    }

    /// <summary>
    /// 블록 제거 효과가 남아있어서 조금 딜레이를 주고 블록레이어로 변경
    /// </summary>
    private void SetToBlockLayer()
    {
        transform.SetParent(LayerManager.it.BlockLayer.transform);
        transform.localPosition = Coordinate.ToBlockPosition(_index);

        _skeleton.GetComponent<MeshRenderer>().sortingLayerName = SortingLayerName.DEFAULT;
    }

    private void PlayBlockedAnim(EDirection dir)
    {
        if (_animCoroutine != null)
        {
            StopCoroutine(_animCoroutine);
            _animCoroutine = null;
        }

        string animName = string.Empty;
        switch (dir)
        {
            case EDirection.TOP: animName = "up_hit"; break;
            case EDirection.RIGHT: animName = "right_hit"; break;
            case EDirection.BOTTOM: animName = "down_hit"; break;
            case EDirection.LEFT: animName = "left_hit"; break;
        }

        if (animName.IsNullOrEmpty())
            return;

        //SoundManager.Instance.PlaySFX("ingame_obj_squirrel_4");

        _skeleton.AnimationState.ClearTracks();
        _skeleton.AnimationState.SetAnimation(0, animName, false);
        _skeleton.AnimationState.Complete += (e) =>
        {
            PlayIdleAnim();
        };
    }

    private void PlayIdleAnim()
    {
        if (_animCoroutine != null)
            StopCoroutine(_animCoroutine);

        _animCoroutine = null;

        string animName = string.Empty;

        switch (_type)
        {
            case (int)EBlockType.SQUIRREL_BLOCK_LV_3:
                animName = "idle_3";
                break;
            case (int)EBlockType.SQUIRREL_BLOCK_LV_2:
                animName = "idle_2";
                break;
            case (int)EBlockType.SQUIRREL_BLOCK_LV_1:
                animName = "idle_1";
                break;
        }

        if (animName.IsNullOrEmpty())
            return;

        _skeleton.AnimationState.ClearTracks();
        _skeleton.AnimationState.SetAnimation(0, animName, true);

        _animCoroutine = StartCoroutine(_PlayIdleAnim());
    }

    private IEnumerator _PlayIdleAnim()
    {
        float delay = UnityEngine.Random.Range(3f, 5f);

        yield return new WaitForSeconds(delay);

        string animName = string.Empty;

        switch (_type)
        {
            case (int)EBlockType.SQUIRREL_BLOCK_LV_3:
                animName = "idle_3-2";
                break;
            case (int)EBlockType.SQUIRREL_BLOCK_LV_2:
                animName = "idle_2-2";
                break;
            case (int)EBlockType.SQUIRREL_BLOCK_LV_1:
                animName = "idle_1-2";
                break;
        }

        if (animName.IsNullOrEmpty())
            yield break;

        _skeleton.AnimationState.ClearTracks();
        _skeleton.AnimationState.SetAnimation(0, animName, false);
        _skeleton.AnimationState.Complete += (e) =>
        {
            PlayIdleAnim();
        };
    }

    private void PlayMoveAnim(EDirection dir)
    {
        if (_animCoroutine != null)
        {
            StopCoroutine(_animCoroutine);
            _animCoroutine = null;
        }

        string animName = string.Empty;
        switch (dir)
        {
            case EDirection.TOP: animName = "up_run"; break;
            case EDirection.RIGHT: animName = "right_run"; break;
            case EDirection.BOTTOM: animName = "down_run"; break;
            case EDirection.LEFT: animName = "left_run"; break;
        }

        if (animName.IsNullOrEmpty())
            return;

        _skeleton.AnimationState.ClearTracks();
        _skeleton.AnimationState.SetAnimation(0, animName, true);
    }

    private void PlayTweenBlockPosition(int nextIndex)
    {
        // 실제 블록 오브젝트 움직임
        var destPos = Coordinate.ToBlockPosition(nextIndex);
        float duration = 0.5f;
        transform.DOLocalMove(destPos, duration).onComplete = PlayIdleAnim;
    }

    public override void Clear()
    {
        base.Clear();

        _skeleton?.AnimationState?.ClearTracks();

        if (_animCoroutine != null)
            StopCoroutine(_animCoroutine);

        _animCoroutine = null;
    }

    public bool IsMoveableIndex(int inIndex)
    {

        if (!LayerManager.it.BoardLayer.IsBlockPlaceablePosition(inIndex))
            return false;

        if (InGameController.Instance.Obstacle.Stick.HasStickDataBetween(Index, inIndex))
            return false;

        if (LayerManager.it.ObstacleLayer.HasLockingBlockObstacleAt(inIndex))
            return false;

        if (!InGameController.Instance.Stable.IsStableAt(inIndex))
            return false;

        int blockType = LayerManager.it.BlockLayer.GetBlockTypeAt(inIndex);
        if (blockType < 0)
            return true;

        Block block = LayerManager.it.BlockLayer.GetBlockAtOrMovingTo(inIndex);
        if (block != null && EBlockTypeUtil.IsRemovableBySquirrel(block.Type))
            return true;

        return false;
    }

    private bool IsSquirrelIndex(int inIndex)
    {
        Block block = LayerManager.it.BlockLayer.GetBlockAtOrMovingTo(inIndex);
        if (block != null && EBlockTypeUtil.IsSquirrelBlock(block.Type))
            return true;

        return false;
    }


}
