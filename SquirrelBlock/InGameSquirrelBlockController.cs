using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SquirrelMoveState = SquirrelBlock.SquirrelMoveState;
using Spine.Unity;

public class InGameSquirrelBlockController : InGameControllerBaseMono
{
    private List<SquirrelBlock> _squirrels;

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
        _squirrels = new List<SquirrelBlock>();

        _movedTurn = InGameController.Instance.TurnCounter.RemainTurn;
    }

    public override void Clear()
    {
        base.Clear();

        if (_squirrels != null)
            _squirrels.ForEach(s => {
                InGameObjectPoolManager.it.BlockFactory.PushBlock(s);
            });

        _squirrels?.Clear();
        _squirrels = null;
    }

    public override void Dispose()
    {
        base.Dispose();

        if (_squirrels != null)
            _squirrels.ForEach(s => s.Clear());

        _squirrels?.Clear();
        _squirrels = null;
    }

    public void AddBlock(SquirrelBlock inBlock)
    {
        if (_squirrels.Contains(inBlock))
            return;

        _squirrels?.Add(inBlock);
    }

    public void RemoveBlock(SquirrelBlock inBlock)
    {
        var control = _squirrels.Find( b => b == inBlock);
        if (control != null)
        {
            control.Clear();
            _squirrels?.Remove(control);
        }
    }

    public bool IsSquirrelsMove()
    {
        return _movedTurn != InGameController.Instance.TurnCounter.RemainTurn;
    }


    private SquirrelBlock GetSquirrelAt(int inIndex)
    {
        return _squirrels.Find(b => b.Index == inIndex);
    }

    public IEnumerator MoveSquirrels()
    {
        yield return StartCoroutine(WaitUntilFinishMove());

        _movedTurn = InGameController.Instance.TurnCounter.RemainTurn;

        SetSquirrelDest();

        InvokeMove();

        yield return StartCoroutine(WaitUntilFinishMove());
    }

    private void SetSquirrelDest()
    {
        _squirrels.ForEach(s => s.SetNextTurnIndex());

        for (int i = 0; i < _squirrels.Count; i++)
        {
            if (_squirrels[i].MoveState != SquirrelMoveState.Ready)
                continue;

            CheckBlockMove(_squirrels[i], new List<SquirrelBlock> { });
        }

        CorrectSameDest();
    }



    private SquirrelMoveState CheckBlockMove(SquirrelBlock squirrel, List<SquirrelBlock> stackedList)
    {
        stackedList.Add(squirrel);

        int nextIndex = squirrel.NextTurnIndex;
        if (nextIndex == -1)
        {
            squirrel.MoveState = SquirrelMoveState.Stay;
        }
        else
        {
            var nextSquirrel = GetSquirrelAt(nextIndex);
            bool canMoveToNext = false;

            if (nextSquirrel != null)
            {
                // 다람쥐가 마주보고 오는경우
                if (nextSquirrel.NextTurnIndex == squirrel.Index)
                {
                    nextSquirrel.MoveState = SquirrelMoveState.Blocked;
                    canMoveToNext = false;
                }
                // 다람쥐 경로가 꼬리를 물고 있는경우
                else if (stackedList.Contains(nextSquirrel))
                {
                    nextSquirrel.MoveState = SquirrelMoveState.Move;
                    canMoveToNext = true;
                }
                else
                {
                    var nextSquirrelState = CheckBlockMove(nextSquirrel, stackedList);
                    canMoveToNext = nextSquirrelState == SquirrelMoveState.Move;
                }
            }
            else
            {
                canMoveToNext = squirrel.IsMoveableIndex(nextIndex);
            }

            squirrel.MoveState = canMoveToNext ? SquirrelMoveState.Move : SquirrelMoveState.Blocked;
        }

        return squirrel.MoveState;
    }


    /// <summary>
    /// 같은 인덱스로 움직이는 다람쥐들 움직임 보정
    /// </summary>
    private void CorrectSameDest()
    {
        for (int i = 0; i < _squirrels.Count; i++)
        {
            var curSquirrel = _squirrels[i];
            if (curSquirrel.MoveState != SquirrelMoveState.Move)
                continue;

            int index = curSquirrel.NextTurnIndex;
            var sameSquirrels = _squirrels.FindAll(s => s.MoveState == SquirrelMoveState.Move && s.NextTurnIndex == index);

            var selectedSquirrel = RandomUtil.GetItemByRandom(sameSquirrels);

            foreach (var s in sameSquirrels)
            {
                if (s != selectedSquirrel)
                {
                    s.MoveState = SquirrelMoveState.Blocked;
                    CheckBlockedIndex(s.Index);
                }
            }
        }
    }

    private void CheckBlockedIndex(int blockedIndex)
    {
        var list = _squirrels.FindAll(s => s.MoveState == SquirrelMoveState.Move && s.NextTurnIndex == blockedIndex);

        if (list != null && list.Count > 0)
        {
            foreach (var s in list)
            {
                s.MoveState = SquirrelMoveState.Blocked;
                CheckBlockedIndex(s.Index);
            }
        }

    }

    private void InvokeMove()
    {
        bool isMoved = false;
        bool isBlocked = false;

        foreach (var s in _squirrels)
        {
            var moveState = s.InvokeSquirrelMove();

            switch (moveState)
            {
                case SquirrelMoveState.Blocked:
                    isBlocked = true;
                    break;
                case SquirrelMoveState.Move:
                    isMoved = true;
                    break;
            }
        }


        // 동시에 나오는 사운드라 한번만 플레이해줌
        if (isBlocked)
            SoundManager.Instance.PlaySFX("ingame_obj_squirrel_4");

        if (isMoved)
            SoundManager.Instance.PlaySFX("ingame_obj_squirrel_3");
    }

    private IEnumerator WaitUntilFinishMove()
    {
        bool isMovingNow = false;
        while (true)
        {
            isMovingNow = false;

            foreach (var s in _squirrels)
            {
                if (s.IsMovingNow || s.IsGettingAttack)
                    isMovingNow = true;
            }

            if (isMovingNow == false)
                break;

            yield return null;
        }
    }
}
