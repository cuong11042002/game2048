using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;
using Random = UnityEngine.Random;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using GG.Infrastructure.Utils.Swipe;

public class GameManager : MonoBehaviour
{
    [SerializeField] private float _width = 3.6f;
    [SerializeField] private float _height = 3.6f;
    [SerializeField] private Node _nodePrefaps;
    [SerializeField] private Block _blockPrefaps;
    [SerializeField] private SpriteRenderer _boardPrefabs;
    [SerializeField] private List<BlockType> _types;
    [SerializeField] private float _travelTime = 0.2f;
    [SerializeField] private int _winCondition = 2048;
    [SerializeField] private Button intructionButton;

    [SerializeField] private SwipeListener SwipeListener;
    [SerializeField] private Transform blockTransform;
    [SerializeField] private float blockSpeed;

    private Vector2 blockDirection = Vector2.zero;

    private void OnEnable()
    {
        SwipeListener.OnSwipe.AddListener(OnSwipe);
    }
    private void OnSwipe(string swipe)
    {
        ChangeState(GameState.Moving);
        Debug.Log(swipe);
        switch (swipe)
        {
            case "Left":
                blockDirection = Vector2.left;
                Shilf(Vector2.left);
                break;
            case "Right":
                blockDirection = Vector2.right;
                Shilf(Vector2.right);
                break;
            case "Up":
                blockDirection = Vector2.up;
                Shilf(Vector2.up);
                break;
            case "Down":
                blockDirection = Vector2.down;
                Shilf(Vector2.down);
                break;
        }

    }

    private void OnDisable()
    {
        SwipeListener.OnSwipe.RemoveListener(OnSwipe);
    }

    [SerializeField] private GameObject _winScreen, _loseScreen;

    private List<Node> _nodes;
    private List<Block> _blocks;
    private GameState _state;
    private int _round;

    public int Value { get; private set; }

    private BlockType GetBlockTypeByValue(int value) => _types.First(t => t.Value == value);


    void Start()
    {
        ChangeState(GameState.GernerateLevel);
    }
    void Update()
    {
        if (_state != GameState.WaittingInput) return;

        if (Input.GetKeyDown(KeyCode.LeftArrow)) Shilf(Vector2.left);
        if (Input.GetKeyDown(KeyCode.RightArrow)) Shilf(Vector2.right);
        if (Input.GetKeyDown(KeyCode.DownArrow)) Shilf(Vector2.down);
        if (Input.GetKeyDown(KeyCode.UpArrow)) Shilf(Vector2.up);
    }
    private void ChangeState(GameState newState)
    {
        _state = newState;

        switch (newState)
        {
            case GameState.GernerateLevel:
                GenerateGrid();
                break;
            case GameState.SpawningBlocks:
                SpawnBlocks(_round++ == 0 ? 2 : 1);
                break;
            case GameState.WaittingInput:
                break;
            case GameState.Moving:
                break;
            case GameState.Win:
                _winScreen.SetActive(true);
                break;
            case GameState.Lose:
                _loseScreen.SetActive(true);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
        }
    }
    void GenerateGrid()
    {
        _round = 0;
        _nodes = new List<Node>();
        _blocks = new List<Block>();
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                var node = Instantiate(_nodePrefaps, new Vector2(x, y), Quaternion.identity);
                _nodes.Add(node);
            }
        }
        var center = new Vector2((float)_width / 2 - 0.5f, (float)_height / 2 - 0.5f);

        var board = Instantiate(_boardPrefabs, center, Quaternion.identity);
        board.size = new Vector2(_width, _height);
        Camera.main.transform.position = new Vector3(1.5f, 1, -999);

        ChangeState(GameState.SpawningBlocks);
    }
    void SpawnBlocks(int amout)
    {
        var freeNodes = _nodes.Where(n => n.OccupiedBlock == null).OrderBy(b => Random.value).ToList();

        foreach(var node in freeNodes.Take(amout))
        {

                SpawnBlock(node, Random.value > 0.8f ? 4 : 2);
        }
        if (freeNodes.Count() == 1 && CheckBoardHavePairedNodesCanMerge() != true)
        {
            if(_blocks.Any(b => b.Value >= _winCondition)){
                ChangeState(GameState.Win);
            }
            else
            {
                ChangeState(GameState.Lose);
                return;
            }
        }
        //ChangeState(_blocks.Any(b=>b.Value == _winCondition ) ? GameState.Win : GameState.WaittingInput);
    }
    void SpawnBlock(Node node, int value)
    {
        var block = Instantiate(_blockPrefaps, node.Pos, Quaternion.identity);
        block.Init(GetBlockTypeByValue(value));
        block.SetBlock(node);
        _blocks.Add(block);
    }

    void Shilf(Vector2 dir)
    {
        ChangeState(GameState.Moving);
        var orderedBlocks = _blocks.OrderBy(b => b.Pos.x).ThenBy(b => b.Pos.y).ToList();
        if (dir == Vector2.right || dir == Vector2.up) orderedBlocks.Reverse();
        foreach (var block in orderedBlocks)
        {
            var next = block.Node;
            do
            {
                block.SetBlock(next);

                var possibleNode = GetNodeAtPosition(next.Pos + dir);
                if(possibleNode != null)
                {
                    if(possibleNode.OccupiedBlock != null && possibleNode.OccupiedBlock.CanMerge(block.Value))
                    {
                        block.MergeBlock(possibleNode.OccupiedBlock);
                    }
                    else if (possibleNode.OccupiedBlock == null) next = possibleNode;
                }
            } while ( next != block.Node);

        }
        var sequence = DOTween.Sequence();
        var isMove = false;
        Debug.Log(orderedBlocks.Count);
        foreach(var block in orderedBlocks)
        {
            
            var movePoint = block.MergingBlock != null ? block.MergingBlock.Node.Pos : block.Node.Pos;
            if(!isCheckPos(movePoint, block.transform.position))
            {
                isMove = true;
                sequence.Insert(0, block.transform.DOMove(movePoint, _travelTime));
            }
        }
        
        sequence.OnComplete(() =>
        {
            foreach (var block in orderedBlocks.Where(b=>b.MergingBlock != null))
            {
                MergeBlocks(block.MergingBlock, block);
            }
            
            if(isMove )
            {
                ChangeState(GameState.SpawningBlocks);
            }
        });
    }

    bool isCheckPos(Vector3 vec1, Vector3 vec2)
    {
        return vec1.Equals(vec2);
    }
    void MergeBlocks(Block baseBlock, Block mergingBlock)
    {
        SpawnBlock(baseBlock.Node, baseBlock.Value*2) ;
        RemoveBlock(baseBlock);
        RemoveBlock(mergingBlock);
    }
    void RemoveBlock(Block block)
    {
        _blocks.Remove(block);
        Destroy(block.gameObject);
    }

    Node GetNodeAtPosition(Vector2 pos)
    {
        return _nodes.FirstOrDefault(n => n.Pos == pos);
    }
    public void _RestartButton()
    {
        SceneManager.LoadScene(sceneBuildIndex: Application.loadedLevel);
    }

    ////
    public Node GetNodeByPos(Vector2 pos)
    {
        foreach (Node node in _nodes)
        {
            if (node.Pos == pos) return node;
        }
        return null;
    }

    public bool IsNodeCanMergeWithAroundedNode(Node currentNode, Node nodeAround)
    {
        if(currentNode.OccupiedBlock && nodeAround.OccupiedBlock && nodeAround.OccupiedBlock.Value == currentNode.OccupiedBlock.Value)
        {
            return true;
        }
        return false;
    }

    public bool CheckNodeCanMergeWithAllAroundedNodes(Node currentNode)
    {
        Node leftNode = GetNodeByPos(currentNode.GetLeftNodePos());
        if(leftNode)
        {
            bool isLeftNodeCanMerge = IsNodeCanMergeWithAroundedNode(currentNode, leftNode);
            if (isLeftNodeCanMerge) return true;
        }

        ///top
        Node upNode = GetNodeByPos(currentNode.GetUpNodePos());
        if (upNode)
        {
            bool isUpNodeCanMerge = IsNodeCanMergeWithAroundedNode(currentNode, upNode);
            if (isUpNodeCanMerge) return true;
        }
        ///right
        Node rightNode = GetNodeByPos(currentNode.GetRightNodePos());
        if (rightNode)
        {
            bool isRightNodeCanMerge = IsNodeCanMergeWithAroundedNode(currentNode, rightNode);
            if (isRightNodeCanMerge) return true;
        }
        ///bottom
        Node downNode = GetNodeByPos(currentNode.GetDownNodePos());
        if (downNode)
        {
            bool isDownNodeCanMerge = IsNodeCanMergeWithAroundedNode(currentNode, downNode);
            if (isDownNodeCanMerge) return true;
        }

        return false;
    }

    public bool CheckBoardHavePairedNodesCanMerge()
    {
        foreach(Node node in _nodes)
        {
            if (CheckNodeCanMergeWithAllAroundedNodes(node)) return true;
        }
        return false;
    }
}
[Serializable]
public struct BlockType
{
    public int Value;
    public Color Color;
}
public enum GameState
{
    GernerateLevel,
    SpawningBlocks,
    WaittingInput,
    Moving,
    Win,
    Lose
} 