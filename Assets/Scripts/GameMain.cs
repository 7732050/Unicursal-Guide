using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum Stage
{
    None,
    Breach,
    Origin,
}

public class GameMain : MonoBehaviour {
    public static GameMain Instance = null;

    [SerializeField]
    private Dropdown xSizeDrop;
    [SerializeField]
    private Dropdown ySizeDrop;
    [SerializeField]
    private GridLayoutGroup gridRoot;
    [SerializeField]
    private Cell cellTemplet;
    [SerializeField]
    private LineRenderer line;
    [SerializeField]
    private Text log;
    private Dictionary<Vector2Int, Cell> cellDic;
    private Stage stage;
    private Cell origin;

    #region 流程控制
    void Init()
    {
        Instance = this;
        xSizeDrop.options.Clear();
        Dropdown.OptionData[] t = new Dropdown.OptionData[]{
            new Dropdown.OptionData("3"),
            new Dropdown.OptionData("4"),
            new Dropdown.OptionData("5"),
            new Dropdown.OptionData("6"),
            new Dropdown.OptionData("7"),
            new Dropdown.OptionData("8")
        };
        xSizeDrop.options.Clear();
        xSizeDrop.options.AddRange(t);
        ySizeDrop.options.Clear();
        ySizeDrop.options.AddRange(t);
        cellDic = new Dictionary<Vector2Int, Cell>();
        log.text = "";
    }

    public void Regrid()
    {
        foreach (var kv in cellDic)
        {
            Destroy(kv.Value.gameObject);
        }
        cellDic.Clear();
        origin = null;
        int x = int.Parse(xSizeDrop.options[xSizeDrop.value].text);
        int y = int.Parse(ySizeDrop.options[ySizeDrop.value].text);
        gridRoot.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridRoot.constraintCount = x;
        for (int j = 0; j < y; j++)
        for (int i = 0; i < x; i++)
        {
            GameObject go = Instantiate<GameObject>(cellTemplet.gameObject);
            go.SetActive(true);
            go.transform.parent = gridRoot.transform;
            go.transform.localScale = Vector3.one;
            go.name = i.ToString() + "-" + j.ToString();
            Cell c = go.GetComponent<Cell>();
            c.Reset(new Vector2Int(i, j));
            cellDic.Add(c.pos, c);
        }
        line.positionCount = 0;
        log.text = "";
    }
    #endregion

    #region OnClick
    public void OnClickStart()
    {
        stage = Stage.None;
        if (origin == null || origin.CStatus != Status.Captured)
        {
            return;
        }
        var path = GetPath(cellDic, origin);
        if (path == null)
        {
            log.text = "无法一笔画完";
        }
        else
        {
            log.text = "成功";
            int x = int.Parse(xSizeDrop.options[xSizeDrop.value].text);
            int y = int.Parse(ySizeDrop.options[ySizeDrop.value].text);
            List<Vector3> poss = new List<Vector3>();
            path.ForEach(o => { poss.Add(new Vector3(o.pos.x - x * 0.5f + 0.5f, (o.pos.y - y * 0.5f + 0.5f) * -1) * 0.649519f); });
            line.positionCount = poss.Count;
            line.SetPositions(poss.ToArray());
        }
        foreach (var kv in cellDic)
        {
            if (kv.Value.CStatus == Status.Captured && kv.Value != origin)
                kv.Value.CStatus = Status.None;
        }
    }

    public void OnClickReset()
    {
        Regrid();
        stage = Stage.None;
    }

    public void OnClickOrigin()
    {
        stage = Stage.Origin;
    }

    public void OnClickBreach()
    {
        stage = Stage.Breach;
    }

    public void OnClickCell(Cell c)
    {
        if (stage == Stage.Breach)
        {
            c.CStatus = c.CStatus == Status.Breach ? Status.None : Status.Breach;
        }
        
        if (stage == Stage.Origin)
        {
            c.CStatus = Status.Captured;
            if (origin != null && !origin.Equals(c))
            {
                origin.CStatus = Status.None;
            }
            origin = c;
        }
    }
    #endregion

    #region Mono
    void Awake()
    {
        Init();
    }
    #endregion

    #region 算法
    List<Cell> GetPath(Dictionary<Vector2Int, Cell> area, Cell origin)
    {
        CaculateLink(area);
        int a;
        if (!isLegal(area, origin, out a))
            return null;
        List<Cell> path = new List<Cell>();
        path.Add(origin);
        if (DFS(area, a, ref path))
            return path;
        else
            return null;
    }

    bool DFS(Dictionary<Vector2Int, Cell> area, int availableCount, ref List<Cell> path)
    {
        if (availableCount == path.Count)
            return true;
        var current = path[path.Count - 1];
        if (!CanConnect(area, current))
        {
            current.CStatus = Status.None;
            path.RemoveAt(path.Count - 1);
            return false;
        }
        foreach (var neighbor in current.canCaptureNeightbors)
        {
            path.Add(neighbor);
            neighbor.CStatus = Status.Captured;
            if (DFS(area, availableCount, ref path))
                return true;
        }
        current.CStatus = Status.None;
        path.RemoveAt(path.Count - 1);
        return false;
    }

    bool CanConnect(Dictionary<Vector2Int, Cell> area, Cell origin)
    {
        List<Cell> originNeighbor = origin.canCaptureNeightbors;
        int t = 0;
        foreach (var kv in area)
        {
            if (!kv.Value.CanCapture())
                continue;
            if (kv.Value.CLinked == 1 && !originNeighbor.Contains(kv.Value))
                t++;
        }
        return t <= 1;
    }

    void CaculateLink(Dictionary<Vector2Int, Cell> area)
    {
        foreach (var kv in area)
        {
            kv.Value.neighbors = GetNeighbor(area, kv.Value.pos);
        }
    }
    
    //检测联通性
    bool isLegal(Dictionary<Vector2Int, Cell> area, Cell origin, out int avaliables)
    {
        List<Vector2Int> combined = new List<Vector2Int>();
        combined.Add(origin.pos);
        bool hasNew = true;
        while (hasNew)
        {
            hasNew = false;
            for(int i = 0; i < combined.Count; i++)
            {
                var pos = combined[i];
                var neightbors = GetCell(area, pos).neighbors;
                foreach (var n in neightbors)
                {
                    if (!combined.Contains(n.pos) && n.CanPass())
                    {
                        hasNew = true;
                        combined.Add(n.pos);
                    }
                }
            }
        }
        avaliables = 0;
        foreach(var kv in area)
        {
            if (kv.Value.CanPass())
            {
                avaliables++;
            }
        }
        if (avaliables != combined.Count)
        {
            Debug.LogErrorFormat("连通性有问题 {0}/{1}", avaliables, combined.Count);
        }
        return avaliables == combined.Count;
    }

    Cell GetCell(Dictionary<Vector2Int, Cell> area, Vector2Int pos)
    {
        if (!area.ContainsKey(pos))
            return null;
        return area[pos];
    }

    List<Cell> GetNeighbor(Dictionary<Vector2Int, Cell> area, Vector2Int pos)
    {
        List<Cell> res = new List<Cell>();
        Cell t = GetCell(area, pos + Vector2Int.up);
        if (t != null) res.Add(t);
        t = GetCell(area, pos + Vector2Int.right);
        if (t != null) res.Add(t);
        t = GetCell(area, pos + Vector2Int.down);
        if (t != null) res.Add(t);
        t = GetCell(area, pos + Vector2Int.left);
        if (t != null) res.Add(t);
        return res;
    }
    #endregion
}
