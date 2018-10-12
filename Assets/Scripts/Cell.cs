using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum Status
{
    None,
    Breach,
    Captured,
}

public class Cell : MonoBehaviour {
    public Vector2Int pos;
    private Status status;
    public Status CStatus { 
        get { return status; }
        set {
            status = value;
            Image img = GetComponent<Image>();
            switch (status)
            {
                case Status.Breach:
                    img.color = Color.gray;
                    break;
                case Status.Captured:
                    img.color = Color.green;
                    break;
                default:
                    img.color = Color.white;
                    break;
            }
        }
    }
    public int CLinked
    {
        get { 
            int linked = neighbors.FindAll(o => { return o.CanCapture(); }).Count;
            //GetComponentInChildren<Text>().text = linked.ToString();
            return linked;
        }
    }
    public List<Cell> neighbors;
    public List<Cell> canCaptureNeightbors { get { return neighbors.FindAll(o => { return o.CanCapture(); }); } }
    private int pointer;

    public bool CanCapture()
    {
        return status == Status.None;
    }

    public bool CanPass()
    {
        return status != Status.Breach;
    }

    public void OnClick()
    {
        GameMain.Instance.OnClickCell(this);
    }
    
    public void Reset(Vector2Int pos)
    {
        this.pos = pos;
        CStatus = Status.None;
        neighbors = new List<Cell>();
        pointer = -1;
    }
}
