using UnityEngine;
using UnityEngine.EventSystems;

public class Puzzle : MonoBehaviour, IDragHandler, /*IEndDragHandler,*/ IBeginDragHandler, IPointerDownHandler
{
    private Vector3             originVec;
    private Vector3             changeVec;
    private Transform           originalTrans;
    private GridManager         gridM;
    private RectTransform       canvasRect;
    private Camera              worldCamera;
    private int                 originalX, originalY;
    private int                 changeX, changeY;
    private int                 x, y;
    private float               positionx;
    private float               positiony;
    private float               startX;
    private float               startY;
    private bool                isMoved;

    public bool IsMoved
    {
        get => isMoved; 
        set => isMoved = value;
    }

    private void Start()
    {
        gridM =             GridManager.Instance;
        canvasRect =        GameManager.Instance.anipangCanvas;
        worldCamera =       GameManager.Instance.mainCamera;
        originalTrans =     this.transform;
        isMoved =           true;    
        x =                 0;
        y =                 1;
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        // 좌표 알아내기 로그
        Debug.Log(GridManager.Instance.GetCoordinates(this.gameObject)); // 내 좌표 알아내기 로그
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        originalX = gridM.GetCoordinates(this.gameObject)[x];
        originalY = gridM.GetCoordinates(this.gameObject)[y];
        Vector3 worldPosition;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(canvasRect, eventData.position, worldCamera, out worldPosition))
        {
            startX = worldPosition.x;
            startY = worldPosition.y;
        }

    }
    public void OnDrag(PointerEventData eventData)
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        Vector3 worldPosition;

        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(canvasRect, eventData.position, worldCamera, out worldPosition))
        {
            positionx = worldPosition.x - startX;
            positiony = worldPosition.y - startY;
        }

        if (positionx > 5 && Mathf.Abs(positionx) > Mathf.Abs(positiony) && isMoved)
        {
            // 오른쪽 이동
            isMoved = false;
            StartCoroutine(gridM.ChangePositionCo(originalX, originalY, originalX, originalY + 1,this));
        }
        else if (positionx < -5 && Mathf.Abs(positionx) > Mathf.Abs(positiony) && isMoved)
        {
            // 왼쪽 이동
            isMoved = false;
            StartCoroutine(gridM.ChangePositionCo(originalX, originalY, originalX, originalY - 1, this));
        }
        else if (positiony > 5 && Mathf.Abs(positionx) < Mathf.Abs(positiony) && isMoved)
        {
            // 위쪽 이동
            isMoved = false;
            StartCoroutine(gridM.ChangePositionCo(originalX, originalY, originalX -1, originalY, this));
        }
        else if (positiony < -5 && Mathf.Abs(positionx) < Mathf.Abs(positiony) && isMoved)
        {
            // 아래쪽 이동
            isMoved = false;
            StartCoroutine(gridM.ChangePositionCo(originalX, originalY, originalX +1, originalY, this));
        }   
    }
}
