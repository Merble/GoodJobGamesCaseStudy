using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class BoardManager : MonoBehaviour
{
    [SerializeField] private Transform _BlocksParent;
    [Space(20f)]
    [SerializeField, ReadOnly] private int _MinNumberToBlast = 2;
    [Space(10f)]
    [SerializeField] [Range(2, 10)] private int _RowCount;
    [SerializeField] [Range(2, 10)] private int _ColumnCount;
    [SerializeField] [Range(1, 6)] private int _ColorNumber;
    [SerializeField] private Vector2Int _TileSize;
    [SerializeField] private Vector2Int _StartPos;
    [Space(15f)]
    [SerializeField] private int _NumberForConditionA;
    [SerializeField] private int _NumberForConditionB;
    [SerializeField] private int _NumberForConditionC;
    [Space(15f)]
    [SerializeField] private float _BoardRecreateWaitDuration;
    [SerializeField] private float _BlockMinScaleFactor;
    [SerializeField] private float _BlockCreationDuration;
    [SerializeField] private float _BlockRemoveDuration;
    [SerializeField] private float _BlockDropSpeed;
    [SerializeField] private float _BlockDropWaitDuration;
    [SerializeField] private float _BoardTopRefillWaitDuration;
    [Space(10f)]
    [SerializeField] private List<Block> _BlockPrefabs = new List<Block>();
    
    private Block[,] _blocks;
    
    private bool _isInputAllowed;

    private void Awake()
    {
        CreateBoard(false);
        _isInputAllowed = true;
    }
    
    private void CreateBoard(bool isAnimated)
    {
        _blocks = new Block[_RowCount, _ColumnCount];

        for (var x = 0; x < _RowCount; x++)
        {
            for (var y = 0; y < _ColumnCount; y++)
            {
                CreateRandomBlockAtPos(x, y, isAnimated);
            }
        }
        
        EvaluateBoard();
    }

    private void CreateRandomBlockAtPos(int x, int y, bool isAnimated)
    {
        var newBlock = GetRandomBlock();
        _blocks[x, y] = newBlock;
        
        newBlock.GridPos = new Vector2Int(x, y);
        newBlock.transform.localPosition = GetLocalPosForGridPos(x, y);
        newBlock.DidGetClicked += OnBlockClick;
        
        if (!isAnimated) return;
        newBlock.transform.localScale = Vector3.one * _BlockMinScaleFactor;
        LeanTween.scale(newBlock.gameObject, Vector3.one, _BlockCreationDuration);
    }
    
    private Block GetRandomBlock()
    { 
        var randomBlockPrefab = _BlockPrefabs[Random.Range(0, _ColorNumber)];
        var newObject = Instantiate(randomBlockPrefab, _BlocksParent, false);
        return newObject;
    }
    
    private Vector2 GetLocalPosForGridPos(int x, int y)
    {
        return new Vector2((_TileSize.x * x) + _StartPos.x, (_TileSize.y * y) + _StartPos.y);
    }

    private void OnBlockClick(Vector2Int gridPos)
    {
        if (!_isInputAllowed) return;
        
        var matchingBlocks = FloodFill(gridPos.x, gridPos.y);

        if (matchingBlocks.Count < 2) return;

        _isInputAllowed = false;
        foreach (var block in matchingBlocks)
        {
            RemoveBlock(block.GridPos, true);
        }

        StartCoroutine(AfterBlockRemovalRoutine());
    }

    private IEnumerator AfterBlockRemovalRoutine()
    {
        yield return new WaitForSeconds(_BlockRemoveDuration);
        
        DropAllBlocks();
     
        yield return new WaitForSeconds(_BlockDropWaitDuration);
        
        RefillTheBoard();

        yield return new WaitForSeconds(_BoardTopRefillWaitDuration);
        
        EvaluateBoard();
    }
    
    private void RemoveBlock(Vector2Int gridPos, bool isAnimated)
    {
        void DestroyBlock()
        {
            _blocks[gridPos.x, gridPos.y].DidGetClicked -= OnBlockClick;
            Destroy(_blocks[gridPos.x, gridPos.y].gameObject);
            _blocks[gridPos.x, gridPos.y] = null;
        }
        
        if(isAnimated)
        {
            LeanTween.scale(_blocks[gridPos.x, gridPos.y].gameObject, Vector3.one * _BlockMinScaleFactor, _BlockRemoveDuration).setOnComplete(DestroyBlock);
        }
        else DestroyBlock();
    }
    
    private void DropAllBlocks()
    {
        for (var x = 0; x < _RowCount; x++)
        {
            for (var y = 0; y < _ColumnCount; y++)
            {
                if (_blocks[x, y] != null) continue;
                    
                DropToEmptySpace(x, y);
                break;
            }
        }
    }
    private void DropToEmptySpace(int posX, int posY)
    {
        var nullCount = 1;
            
        for (var y = posY + 1; y < _ColumnCount; y++)
        {
            var block = _blocks[posX, y];
                
            if (block == null) 
            {
                nullCount++;
            }
            else
            {
                var newYPos = y - nullCount;
                _blocks[posX, newYPos] = block;
                _blocks[posX, y] = null;
                    
                block.GridPos = new Vector2Int(posX, newYPos);

                var blockLocalPos = GetLocalPosForGridPos(posX, newYPos);

                var distance = Vector2.Distance(block.transform.localPosition, blockLocalPos);
                var duration = distance / _BlockDropSpeed;
                LeanTween.moveLocal(block.gameObject, blockLocalPos, duration);
            }
        }
    }
    
    private void RefillTheBoard()
    {
        for (var x = 0; x < _RowCount; x++)
        {
            for (var y = 0; y < _ColumnCount; y++)
            {
                var tile = _blocks[x, y];
                    
                if (!tile)
                {
                    CreateRandomBlockAtTopFromGridPos(x, y);
                }
            }
        }
    }
    private void CreateRandomBlockAtTopFromGridPos(int x, int y)
    {
        var newBlock = GetRandomBlock();
        _blocks[x, y] = newBlock;
        
        newBlock.GridPos = new Vector2Int(x, y);
        var finalLocalPos = GetLocalPosForGridPos(x, y);
        var firstLocalPos = finalLocalPos + new Vector2(0, _TileSize.y * ((_ColumnCount - y) + (_ColumnCount/2)));
        newBlock.transform.localPosition = firstLocalPos;
        newBlock.DidGetClicked += OnBlockClick;

        var distance = Vector2.Distance(firstLocalPos, finalLocalPos);
        var duration = distance / _BlockDropSpeed;
        LeanTween.moveLocal(newBlock.gameObject, finalLocalPos, duration);
    }

    private void EvaluateBoard()
    {
        StartCoroutine(CheckMatchingBlocks());
    }
    
    private IEnumerator CheckMatchingBlocks()
    {
        var isAnyMatchOnRightFirst = RightFirst();
        var isAnyMatchOnUpFirst = UpFirst();

        if (!(isAnyMatchOnRightFirst || isAnyMatchOnUpFirst))
        {
            Debug.Log("no moves");
            RecreateBoard();
        }
        
        yield return new WaitForSeconds(.5f);
            
        _isInputAllowed = true;
    }

    private bool RightFirst()
    {
        var sameBlocks = new List<Block>();
        var isAnyMatch = false;

        for (var y = 0; y < _ColumnCount; y++)
        {
            for (var x = 0; x < _RowCount; x++)
            {
                if (x > _RowCount - _MinNumberToBlast)
                    continue;

                var currentBlock = _blocks[x, y];
                currentBlock.SetMatchGroupType(BlockMatchGroupType.Default);

                var isAllMatch = true;
                for (var i = 1; i < _MinNumberToBlast; i++)
                {
                    var blockToCheck = _blocks[x + i, y];

                    var isMatch = currentBlock.Color == blockToCheck.Color;
                    isAllMatch = isMatch;
                    if (!isMatch) break;
                }

                if (isAllMatch)
                {
                    isAnyMatch = true;
                    sameBlocks.Add(currentBlock);

                    var newBlocks = FloodFill(x, y);

                    foreach (var block in newBlocks.Where(block => !sameBlocks.Contains(block)))
                    {
                        sameBlocks.Add(block);
                    }
                    
                    var number = sameBlocks.Count;
                    var matchType = BlockMatchGroupType.Default;

                    if (number > _NumberForConditionA)
                    {
                        matchType = BlockMatchGroupType.A;
                        
                        if (number > _NumberForConditionB)
                        {
                            matchType = BlockMatchGroupType.B;
                    
                            if (number > _NumberForConditionC)
                            {
                                matchType = BlockMatchGroupType.C;
                            }
                        }
                    }
                    
                    foreach (var block in sameBlocks)
                    {
                        block.SetMatchGroupType(matchType);
                    }

                }
                
                sameBlocks.Clear();
            }
        }

        return isAnyMatch;
    }

    private bool UpFirst()
    {
        var sameBlocks = new List<Block>();
        var isAnyMatch = false;
            
        for (var x = 0; x < _RowCount; x++)
        {
            for (var y = 0; y < _ColumnCount; y++)
            {
                if (y > _ColumnCount - _MinNumberToBlast)
                    continue;

                var currentBlock = _blocks[x, y];

                var isAllMatch = true;
                for (var i = 1; i < _MinNumberToBlast; i++)
                {
                    var blockToCheck = _blocks[x, y + i];

                    var isMatch = currentBlock.Color == blockToCheck.Color;
                    isAllMatch = isMatch;
                    if (!isMatch) break;
                }

                if (isAllMatch)
                {
                    isAnyMatch = true;
                    
                    sameBlocks.Add(currentBlock);

                    var newBlocks = FloodFill(x, y);

                    foreach (var block in newBlocks.Where(block => !sameBlocks.Contains(block)))
                    {
                        sameBlocks.Add(block);
                    }
                    
                    var number = sameBlocks.Count;
                    var matchType = BlockMatchGroupType.Default;

                    if (number > _NumberForConditionA)
                    {
                        matchType = BlockMatchGroupType.A;
                        
                        if (number > _NumberForConditionB)
                        {
                            matchType = BlockMatchGroupType.B;
                            
                            if (number > _NumberForConditionC)
                            {
                                matchType = BlockMatchGroupType.C;
                            }
                        }
                    }
                    
                    foreach (var block in sameBlocks)
                    {
                        block.SetMatchGroupType(matchType);
                    }

                }
                
                sameBlocks.Clear();
            }
        }
        
        return isAnyMatch;
    }

    private List<Block> FloodFill(int x, int y)
    {
        var blockList = new List<Block>();

        var initialBlock = GetBlockAtPos(new Vector2Int(x, y));
        var lookupList = new List<Block> {initialBlock};
            
        while (lookupList.Count > 0)
        {
            var lookupPos = lookupList[lookupList.Count - 1].GridPos;
            var lookupBlock = GetBlockAtPos(lookupPos);
                
            lookupList.Remove(lookupBlock);
            blockList.Add(lookupBlock);
                
            var neighbors = new List<Block>();
                
            var left = GetBlockAtPos(lookupPos + Vector2Int.left);
            if(left) neighbors.Add(left);
                
            var right = GetBlockAtPos(lookupPos + Vector2Int.right);
            if(right) neighbors.Add(right);
                
            var up = GetBlockAtPos(lookupPos + Vector2Int.up);
            if(up) neighbors.Add(up);
                
            var down = GetBlockAtPos(lookupPos + Vector2Int.down);
            if(down) neighbors.Add(down);

            foreach (var neighbor in neighbors)
            {
                if (lookupList.Contains(neighbor)) continue;
                if (blockList.Contains(neighbor)) continue;
                if (neighbor.Color != lookupBlock.Color) continue;
                    
                lookupList.Add(neighbor);
            }
        }
        return blockList;
    }

    private Block GetBlockAtPos(Vector2Int pos)
    {
        if (pos.x < 0) return null;
        if (pos.y < 0) return null;
        if (pos.x >= _RowCount) return null;
        if (pos.y >= _ColumnCount) return null;

        return _blocks[pos.x, pos.y];
    }
    
    private void RecreateBoard()
    {
        foreach (var block in _blocks)
        {
            RemoveBlock(block.GridPos, true);
        }

        StartCoroutine(DoAfter(_BoardRecreateWaitDuration, () => { CreateBoard(true); }));
    }
    
    private IEnumerator DoAfter(float waitTime, Action callback)
    {
        yield return new WaitForSeconds(waitTime);
            
        callback?.Invoke();
    }
}