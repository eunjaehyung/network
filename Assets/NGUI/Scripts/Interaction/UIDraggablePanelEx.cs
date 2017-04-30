using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UIDraggablePanelEx : UIScrollView
{
    public enum PanelExMode
    {
        AutoArrangement,
        ManualArrangement,
    }
    [SerializeField][HideInInspector] private PanelExMode m_PanelExMode = PanelExMode.AutoArrangement;

    public void SetPanelArrangeMode(PanelExMode mode)
    {
        m_PanelExMode = mode;
    }
    //=====================================================================
    //
    // Fields & Properties - UI
    //
    //=====================================================================

    /// <summary>
    /// UIGrid - 아이템 부모
    /// </summary>
    //    public UIGrid Grid;
    public enum EArrangement
    {
        Vertical,
        Horizontal,
    }
    public EArrangement Arrangement;
    public float CellWidth;
    public float CellHeight;
    public int LineCount;

    /// <summary>
    /// 프리팹
    /// </summary>
    [SerializeField][HideInInspector] private GameObject ScrollEachItemPrefab;
    [SerializeField][HideInInspector] private int EachItemWidth;
    [SerializeField][HideInInspector] private int EachItemHeight;
    [SerializeField][HideInInspector] private Vector3 mScaleOfItem = Vector3.one;

    [SerializeField][HideInInspector] private int NumItemPerRow;
    [SerializeField][HideInInspector] private int EachItemPadding;
    [SerializeField][HideInInspector] private int EachItemPaddingForHeight;


    public void SetNumOfItemPerRow(int _num)
    {
        NumItemPerRow = _num;
    }

    public void SetItemHeightPadding(int _padding)
    {
        EachItemPaddingForHeight = _padding;
    }

    // 여러가지 종류 리스트를 가지기 위해 구현함.
    // hack 관련된 사항은 [종]<to>[훈] 문의.
    public bool HasMultiList{get;set;}
    public Vector3 MultiListOffSet { get; set; }

    // 여러 리스트를 등록 할때, 해당 리스트의 갯수들을 저장한다.
    private int[] mCountArray;
    // 해당 리스트가 호출할 콜백 함수들.
    private ChangeIndexDelegate[] mCallbackArray;

    private UIPanel mUIPanel = null;
    private BoxCollider mBoxCollider = null;
    // 
    public GameObject boundaryLinePrefab;

    //=====================================================================
    //
    // Fields - Variable
    //
    //=====================================================================

    /// <summary>
    /// 아이템 스크롤 위치 수치 [0, 1] 
    /// </summary>
    public Vector2 mDragAmount = Vector2.zero;

    /// <summary>
    /// 총 아이템의 개수
    /// </summary>
    private int ItemTotalCount;
    public int ItemCount;

    /// <summary>
    /// 첫 부분과 끝 부분의 아이템. grid 영역의 크기를 구하기 위함.
    /// </summary>
    public Transform mFirstTemplate = null;
    public Transform mLastTemplate = null;

    /// <summary>
    /// 첫 부분과 끝 부분의 위치
    /// </summary>
    public Vector3 mFirstPosition = Vector3.zero;
    public Vector3 mPrevPosition = Vector3.zero;

    /// <summary>
    /// 관리 리스트
    /// </summary>
    private List<UIListItem> mList = new List<UIListItem>();

    /// <summary>
    /// 화면에 보여질 최소한의 개수 
    /// </summary>
    private int mMinShowCount;

    //=====================================================================
    //
    // Fields & Properties - Events
    //
    //=====================================================================

    public delegate void ChangeIndexDelegate(GameObject gameObj, int index);
    private ChangeIndexDelegate mCallback;

    //=====================================================================
    //
    // Fields & Properties - Get & Set
    //
    //=====================================================================

    /// <summary>
    /// 머리를 가리킨다.
    /// </summary>
    private UIListItem Head { get { return mList.Count <= 0 ? null : mList[0]; } }

    /// <summary>
    /// 꼬리를 가리킨다.
    /// </summary>
    private UIListItem Tail { get { return mList.Count <= 0 ? null : mList[mList.Count - 1]; } }

    private void CallAllOfItemDelegates(Transform rootItem, int ListIndex)
    {
        bool bShowBoundaryLine = false;
        int numChild = rootItem.childCount;
        for (int i = 0; i < numChild && i < NumItemPerRow; ++i)
        {
            GameObject eachItem = rootItem.GetChild(i).gameObject;

            int itemIdx = ListIndex * NumItemPerRow + i;

            bool bActive = false;

            int numOfList = HasMultiList ? mCountArray.Length : 1;
            for (int j = 0; j < numOfList; ++j)
            {
                int begin = 0;
                int end = HasMultiList ? mCountArray[j] : ItemTotalCount;
                if (0 != j)
                {
                    int numAccumCount = 0;
                    for (int k = j; k >0; --k)
                        numAccumCount += mCountArray[j - k];
                    begin = Mathf.CeilToInt((float)numAccumCount / (float)NumItemPerRow) * NumItemPerRow;
                }

                if (null != boundaryLinePrefab)
                {
                    begin += NumItemPerRow * j;

                    if (begin - NumItemPerRow <= itemIdx && itemIdx < begin)
                        bShowBoundaryLine = true;
                }

                if (begin <= itemIdx && itemIdx < begin + end)
                {
                    bActive = true;
                    if (HasMultiList)
                        mCallbackArray[j](eachItem, itemIdx - begin);
                    else
                        mCallback(eachItem, itemIdx - begin);
                }
            }

            eachItem.SetActive(bActive);
        }

        if (null != boundaryLinePrefab)
            rootItem.GetChild(NumItemPerRow).gameObject.SetActive(bShowBoundaryLine);
    }

    /// <summary>
    /// 스크롤 목록 아이템 프리펩 구성.
    /// </summary>
    private GameObject ConstructRootPrefab()
    {
        GameObject rootObj = NGUITools.AddChild(gameObject);
        rootObj.transform.localPosition = Vector3.zero;

        rootObj.AddComponent<cUIScrollListBase>();

        UIWidget widget = rootObj.AddComponent<UIWidget>();
        widget.width = (int)CellWidth;
        widget.height = (int)CellHeight;
        
        if (null != ScrollEachItemPrefab)
        {
            int halfNumItem = (NumItemPerRow >> 1);
            int halfWidth = (int)((EachItemWidth * mScaleOfItem.x) * 0.5f);

            bool even = (NumItemPerRow % 2) == 0;
            for (int i = 0; i < NumItemPerRow; ++i)
            {
                GameObject Item = NGUITools.AddChild(rootObj, ScrollEachItemPrefab);

                int evenGap = even ? halfWidth + (EachItemPadding>>1) : 0;
                Item.transform.localPosition = new Vector3(evenGap + 
                    (i - halfNumItem) * (EachItemWidth * mScaleOfItem.x+ EachItemPadding), 0);
                Item.transform.localScale = mScaleOfItem;
            }

            if (null != boundaryLinePrefab)
            {
                GameObject Item = NGUITools.AddChild(rootObj, boundaryLinePrefab);
                Item.SetActive(false);
            }
        }

        return rootObj;
    }

    /// <summary>
    /// 화면에 보일 수 있는 가로 개수
    /// </summary>
    private int maxCol
    {
        get
        {
            if (Arrangement == EArrangement.Vertical)
            {
                return LineCount;
            }
            else
            {
                return Mathf.CeilToInt(panel.clipRange.z / CellWidth);
            }
        }
    }

    /// <summary>
    /// 화면에 보일 수 있는 세로 개수
    /// </summary>
    private int maxRow
    {
        get
        {
            if (Arrangement == EArrangement.Vertical)
            {
                return Mathf.CeilToInt(panel.clipRange.w / (CellHeight + EachItemPaddingForHeight));
            }
            else
            {
                return LineCount;
            }
        }
    }
    //	{ get { return  } }

    //=====================================================================
    //
    // Methods - UIDraggablePanel override
    //
    //=====================================================================

    /// <summary>
    /// Calculate the bounds used by the widgets.
    /// </summary>
    public override Bounds bounds
    {
        get
        {
            if (!mCalculatedBounds)
            {
                mCalculatedBounds = true;
                mBounds = CalculateRelativeWidgetBounds2(mTrans, mFirstTemplate, mLastTemplate);
            }
            return mBounds;
        }
    }

    public void Awake()
    {
        base.Awake();
    }

    public void Start()
    {
        base.Start();
        mFirstPosition = mTrans.localPosition;
        mPrevPosition = mTrans.localPosition;
    }

    public void ResetPosition()
    {
        Vector3 pos = mTrans.localPosition;
        Vector2 clipOffsetPos = panel.clipOffset;
        if (Arrangement == EArrangement.Vertical)
        {
            pos.y += clipOffsetPos.y;
            clipOffsetPos.y = 0;
            panel.clipOffset = clipOffsetPos;
            mTrans.localPosition = pos;
        }
        else if (Arrangement == EArrangement.Horizontal)
        {
            pos.x += clipOffsetPos.x;
            clipOffsetPos.x = 0;
        }

        panel.clipOffset = clipOffsetPos;
        mTrans.localPosition = pos;
    }

    public void OnDestroy()
    {
        RemoveAll();
    }

    public List<UIListItem> GetItemList()
    {
        return mList;
    }

    public GameObject GetItemByIndex(int _idx)
    {
        if (_idx < 0 || _idx >= ItemTotalCount || null == mList || 0 == mList.Count )
            return null;

        int idxOfList = _idx / NumItemPerRow;
        int idxOfChild = _idx % NumItemPerRow;

        for(int i=0;i<mList.Count; ++i)
            if(idxOfList == mList[i].Index)
                return mList[i].Target.transform.GetChild(idxOfChild).gameObject;
        return null;
    }

    public void Refresh(int _count)
    {
        ItemTotalCount = _count;

        if (HasMultiList)
        {
            _count = 0;
            for (int i = 0; i < mCountArray.Length; ++i)
                _count += (Mathf.CeilToInt((float)mCountArray[i] / (float)NumItemPerRow)) * NumItemPerRow;

            if (null != boundaryLinePrefab)
                _count += (mCountArray.Length - 1) * NumItemPerRow;

            ItemTotalCount = _count;
        }
        else
        {
            if (NumItemPerRow > 0)
                _count = Mathf.CeilToInt((float)_count / (float)NumItemPerRow);

        }

        UIListItem item = null;
        GameObject obj = null;
        UIListItem prevItem = null;

        if (Arrangement == EArrangement.Vertical)
            mMinShowCount = maxCol * (maxRow + 1);
        else
            mMinShowCount = (maxCol + 1) * maxRow;

        int makeCount = Mathf.Min(_count, mMinShowCount);

        if (ItemCount != _count)
        {
            ItemCount = _count;
            SetTemplate(_count);
            UpdateCurrentPosition();
        }

        if (mList.Count > _count)
        {
            int removeIndexCount = _count;
            while (removeIndexCount < mList.Count)
            {
                GameObject.DestroyImmediate(mList[removeIndexCount].Target);
                mList.RemoveAt(removeIndexCount);
            }
        }
        else if (mList.Count < _count && mList.Count < makeCount)
        {
            int needMakeCount = makeCount > _count ? makeCount : _count;

            for (int i = mList.Count; i < needMakeCount; ++i)
            {
                obj = ConstructRootPrefab();

                if (obj.GetComponent<UIDragScrollView>() == null)
                    obj.AddComponent<UIDragScrollView>().scrollView = this;

                item = new UIListItem();
                item.Target = obj;
                item.SetIndex(i);
                mList.Add(item);

                item.Prev = prevItem;
                item.Next = null;
                if (prevItem != null)
                    prevItem.Next = item;
                prevItem = item;

                CallAllOfItemDelegates(item.Target.transform, i);
            }
        }

        for (int i = 0; i < mList.Count; i++)
        {
            if (i < _count)
            {
                item = mList[i];

                CallAllOfItemDelegates(item.Target.transform, mList[i].Index);
            }
        }

        if (mMinShowCount - 1 > _count)
        {
            ResetPosition();
        }
    }

    private void SetAutoArrangement()
    {
        if (PanelExMode.AutoArrangement != m_PanelExMode)
            return;

        float eachItemWidth = EachItemWidth * mScaleOfItem.x;
        float cellwidth = CellWidth ;
        NumItemPerRow = (int)(cellwidth / eachItemWidth);
        float extrasize = cellwidth - NumItemPerRow * eachItemWidth;
        EachItemPadding = (int)(extrasize / NumItemPerRow);
    }

    public bool HasEqualListCount(int _count)
    {
        if (_count == 0)
            return false;

        return (ItemTotalCount == _count);
    }

    public bool HasEqualListCount(int[] _count)
    {
        int count = 0;
        for (int i = 0; i < _count.Length; ++i)
            count += (Mathf.CeilToInt((float)_count[i] / (float)NumItemPerRow)) * NumItemPerRow;

        if (null != boundaryLinePrefab)
            count += (_count.Length - 1) * NumItemPerRow;

        if (count == 0)
            return false;

        return (ItemTotalCount == count);
    }

    public void Init(int[] _counts, ChangeIndexDelegate[] callbacks, float _dragAmountX = 0.0f, float _dragAmountY = 0.0f)
    {
     //   mFirstPosition = (_InitX < -99990.9f) ? mTrans.localPosition : new Vector3(_InitX, _InitY);
        if (null == _counts)
            return;
                
        mCountArray = _counts;
        mCallbackArray = callbacks;

        HasMultiList = true;
        StartCoroutine(InitEnumerator(0, _dragAmountX, _dragAmountY));
    }

    public void Init(int _count, ChangeIndexDelegate callback, float _dragAmountX = 0.0f, float _dragAmountY = 0.0f)
    {
      //  mFirstPosition = (_InitX < -99990.9f) ? mTrans.localPosition : new Vector3(_InitX, _InitY);
        mCallback = callback;
        HasMultiList = false;
        StartCoroutine(InitEnumerator(_count, _dragAmountX, _dragAmountY));
    }

    bool m_bEnableScrolling = false;
    IEnumerator InitEnumerator(int _count, float _dragAmountX = 0.0f, float _dragAmountY = 0.0f)
    {
        MultiListOffSet = Vector3.zero;
        yield return null;
        
        if (null == mUIPanel)
            mUIPanel = GetComponent<UIPanel>();
        CellWidth = mUIPanel.baseClipRegion.z;
        if (EachItemHeight != 0)
            CellHeight = (float)EachItemHeight;

        if (null == gameObject.GetComponent<UIDragScrollView>())
            gameObject.AddComponent<UIDragScrollView>().scrollView = this;

        if (null == mBoxCollider)
        {
            mBoxCollider = gameObject.AddComponent<BoxCollider>();
            mBoxCollider.center = new Vector3(mUIPanel.baseClipRegion.x, mUIPanel.baseClipRegion.y - mUIPanel.clipOffset.y);
            mBoxCollider.size = new Vector3(mUIPanel.baseClipRegion.z, mUIPanel.baseClipRegion.w);
        }

        SetAutoArrangement();

        ResetPosition();

        if (HasMultiList)
        {
            _count = 0;
            for (int i = 0; i < mCountArray.Length; ++i)
                _count += (Mathf.CeilToInt((float)mCountArray[i] / (float)NumItemPerRow)) * NumItemPerRow;

            if (null != boundaryLinePrefab)
                _count += (mCountArray.Length-1) * NumItemPerRow;
        }

        ItemTotalCount = _count;
        if (NumItemPerRow > 0)
            ItemCount = Mathf.CeilToInt((float)_count / (float)NumItemPerRow);
        else
            ItemCount = _count;

        SetTemplate(ItemCount);

        RemoveAll();
        mList.Clear();

        //화면에 보여질 개수
        if (Arrangement == EArrangement.Vertical)
            mMinShowCount = maxCol * (maxRow + 1);
        else
            mMinShowCount = (maxCol + 1) * maxRow;

        int makeCount = Mathf.Min(ItemCount, mMinShowCount+1);

        GameObject obj = null;
        UIListItem prevItem = null;
        for (int i = 0; i < makeCount; i++)
        {
            obj = ConstructRootPrefab();

            if (obj.GetComponent<UIDragScrollView>() == null)
                obj.AddComponent<UIDragScrollView>().scrollView = this;

            UIListItem item = new UIListItem();
            item.Target = obj;
            item.SetIndex(i);
            mList.Add(item);

            item.Prev = prevItem;
            item.Next = null;
            if (prevItem != null)
                prevItem.Next = item;
            prevItem = item;

            CallAllOfItemDelegates(item.Target.transform, i);
        }
        UpdatePosition();

        ///*
        if (mMinShowCount - 1 > ItemCount)
            m_bEnableScrolling = false;//enabled = false;
        else
            m_bEnableScrolling = true;// enabled = true;
        // */

        SetDragAmount(_dragAmountX, _dragAmountY, false);
        yield return null;
    }

    /// <summary>
    /// Restrict the panel's contents to be within the panel's bounds.
    /// </summary>
    //    public override bool RestrictWithinBounds(bool instant)
    //    {
    //        Vector3 constraint = panel.CalculateConstrainOffset(bounds.min, bounds.max);
    //
    //        if (constraint.magnitude > 0.001f)
    //        {
    //            if (!instant && dragEffect == DragEffect.MomentumAndSpring)
    //            {
    //                // Spring back into place
    //                SpringPanel.Begin(panel.gameObject, mTrans.localPosition + constraint, 13f, UpdateCurrentPosition);
    //            }
    //            else
    //            {
    //                // Jump back into place
    //                MoveRelative(constraint);
    //                mMomentum = Vector3.zero;
    //                mScroll = 0f;
    //            }
    //            return true;
    //        }
    //        return false;
    //    }

    /// <summary>    /// Changes the drag amount of the panel to the specified 0-1 range values.
    /// (0, 0) is the top-left corner, (1, 1) is the bottom-right.
    /// </summary>
    public override void SetDragAmount(float x, float y, bool updateScrollbars)
    {
        mDragAmount.x = x;
        mDragAmount.y = y;
        base.SetDragAmount(x, y, updateScrollbars);

        UpdateCurrentPosition();
    }

    /// <summary>
    /// Move the panel by the specified amount.
    /// </summary>
    public override void MoveRelative(Vector3 relative)
    {
        if (!m_bEnableScrolling)
            return;
        base.MoveRelative(relative);
        UpdateCurrentPosition();

        ///*
        Bounds b = bounds;
        Vector2 bmin = b.min;
        Vector2 bmax = b.max;
        
        Vector4 clip = mPanel.finalClipRegion;
        int intViewSize = Mathf.RoundToInt(clip.w);
        if ((intViewSize & 1) != 0) intViewSize -= 1;
        float halfViewSize = intViewSize * 0.5f;
        halfViewSize = Mathf.Round(halfViewSize);

        if (mPanel.clipping == UIDrawCall.Clipping.SoftClip)
            halfViewSize -= mPanel.clipSoftness.y;

        float contentSize = bmax.y - bmin.y;
        float viewSize = halfViewSize * 2f;
        float contentMin = bmin.y;
        float contentMax = bmax.y;
        float viewMin = clip.y - halfViewSize;
        float viewMax = clip.y + halfViewSize;

        contentMin = viewMin - contentMin;
        contentMax = contentMax - viewMax;

        contentMin = Mathf.Clamp01(contentMin / contentSize);
        contentMax = Mathf.Clamp01(contentMax / contentSize);

        float contentPadding = contentMin + contentMax;
        
        if (Arrangement == EArrangement.Vertical)
            mDragAmount.y = ((contentPadding > 0.001f) ? 1f - contentMin / contentPadding : 0f);
        else if (Arrangement == EArrangement.Horizontal)
            mDragAmount.x = ((contentPadding > 0.001f) ? 1f - contentMin / contentPadding : 0f);
       //*/


    }

    //=====================================================================
    //
    // Methods - UIDraggablePanel2
    //
    //=====================================================================

    /// <summary>
    /// 꼬리부분을 머리부분으로 옮긴다. 
    /// </summary>
    public void TailToHead()
    {
        int cnt = Arrangement == EArrangement.Vertical ? maxCol : maxRow;
        for (int i = 0; i < cnt; i++)
        {
            UIListItem item = Tail;

            if (item == null)
                return;

            if (item == Head)
                return;

            if (item.Prev != null)
                item.Prev.Next = null;

            item.Next = Head;
            item.Prev = null;

            Head.Prev = item;

            mList.RemoveAt(mList.Count - 1);
            mList.Insert(0, item);
        }
    }

    /// <summary>
    /// 머리 부분을 꼬리 부분으로 옮긴다. 
    /// </summary>
    public void HeadToTail()
    {
        int cnt = Arrangement == EArrangement.Vertical ? maxCol : maxRow;
        for (int i = 0; i < cnt; i++)
        {
            UIListItem item = Head;

            if (item == null)
                return;

            if (item == Tail)
                return;

            item.Next.Prev = null;
            item.Next = null;
            item.Prev = Tail;

            Tail.Next = item;

            mList.RemoveAt(0);
            mList.Insert(mList.Count, item);
        }
    }

    /// <summary>
    /// 실제 아이템 양 끝쪽에 임시 아이템을 생성 후 cllipping 되는 영역의 bound를 구한다.
    /// </summary>
    /// <param name="count"></param>
    private void SetTemplate(int count)
    {
        if (mFirstTemplate == null)
        {
            GameObject firstTemplate = ConstructRootPrefab();
            firstTemplate.SetActive(false);
            mFirstTemplate = firstTemplate.transform;
            mFirstTemplate.name = "first rect";
        }

        if (mLastTemplate == null)
        {
            GameObject lastTemplate = ConstructRootPrefab();
            lastTemplate.SetActive(false);
            mLastTemplate = lastTemplate.transform;
            mLastTemplate.name = "last rect";
        }

        float firstX = panel.baseClipRegion.x - ((panel.baseClipRegion.z - CellWidth) * 0.5f);
        float firstY = panel.baseClipRegion.y + ((panel.baseClipRegion.w - (CellHeight + EachItemPaddingForHeight) + panel.clipSoftness.y) * 0.5f);
        if (Arrangement == EArrangement.Vertical)
        {
            mFirstTemplate.localPosition = new Vector3(firstX,
                                                       firstY,
                                                       0); //처음위치
            mLastTemplate.localPosition = new Vector3(firstX + (LineCount - 1) * CellWidth,
                                                      firstY - (CellHeight + EachItemPaddingForHeight) * ((count - 1) / LineCount), 0); //끝위치
        }
        else
        {
            mFirstTemplate.localPosition = new Vector3(firstX,
                                                       firstY,
                                                       0); //처음위치
            mLastTemplate.localPosition = new Vector3(firstX + CellWidth * ((count - 1) / LineCount),
                                                      firstY - (LineCount - 1) * (CellHeight + EachItemPaddingForHeight),
                                                      0); //끝위치
        }

        mCalculatedBounds = true;
        mLastTemplate.localPosition = mLastTemplate.localPosition + MultiListOffSet;
        mBounds = CalculateRelativeWidgetBounds2(mTrans, mFirstTemplate, mLastTemplate);

        Vector3 constraint = panel.CalculateConstrainOffset(bounds.min, bounds.max);
        SpringPanel.Begin(panel.gameObject, mTrans.localPosition + constraint, 13f);

    }

    /// <summary>
    /// 아이템들의 재사용을 위하여 위치를 조절한다.
    /// </summary>
    public void UpdateCurrentPosition()
    {
        if (Head == null)
            return;

        Vector3 currentPos = mFirstPosition - mTrans.localPosition;

        if (Arrangement == EArrangement.Vertical)
        {
            bool isScrollUp = currentPos.y > mPrevPosition.y;

            int headIndex = (int)(-currentPos.y / (CellHeight + EachItemPaddingForHeight)) * maxCol - 1;
            headIndex = Mathf.Clamp(headIndex, 0, ItemCount - 1);

            if (headIndex + mList.Count > ItemCount)
                headIndex = ItemCount - mList.Count;

            if (headIndex < 0)
                headIndex = 0;

            if (Head.Index != headIndex)
            {
                if (isScrollUp)
                    TailToHead();
                else
                    HeadToTail();

                SetIndexHeadtoTail(headIndex);
                UpdatePosition();
            }

            if (null != mBoxCollider)
            {
                mBoxCollider.center = new Vector3(mUIPanel.baseClipRegion.x, mUIPanel.baseClipRegion.y + mUIPanel.clipOffset.y);
            }
        }
        else
        {
            bool isScrollUp = currentPos.x > mPrevPosition.x;

            int headIndex = (int)(currentPos.x / CellWidth) * maxRow;  //세로줄의 맨 처음 
            headIndex = Mathf.Clamp(headIndex, 0, ItemCount - maxRow);

            if (headIndex + mList.Count > ItemCount)
                headIndex = ItemCount - mList.Count;

            if (headIndex < 0)
                headIndex = 0;

            if (Head.Index != headIndex)
            {
                if (isScrollUp)
                    TailToHead();
                else
                    HeadToTail();

                if (headIndex + mList.Count > ItemCount || headIndex < 0)
                    return;

                SetIndexHeadtoTail(headIndex);
                UpdatePosition();
            }
        }
        mPrevPosition = currentPos;
    }

    /// <summary>
    /// head부터 index를 재 정리한다.
    /// </summary>
    /// <param name="headIndex"></param>
    public void SetIndexHeadtoTail(int headIndex)
    {
        UIListItem item = null;
        int index = -1;
        for (int i = 0; i < mList.Count && i < ItemCount; i++)
        {
            index = i + headIndex;
            item = mList[i];
            if (item.SetIndex(index))
                CallAllOfItemDelegates(item.Target.transform, index);
        }
    }

    /// <summary>
    /// tail부터 index를 재 정리한다.
    /// </summary>
    /// <param name="tailIndex"></param>
    public void SetIndexTailtoHead(int tailIndex)
    {
        UIListItem item = null;
        int index = -1;
        int cnt = mList.Count;
        for (int i = 0; i < cnt; i++)
        {
            index = tailIndex - i;
            item = mList[cnt - i - 1];
            if (item.SetIndex(index))
                CallAllOfItemDelegates(item.Target.transform, index);
        }
    }

    /// <summary>
    /// 아이템들의 위치를 정한다.
    /// </summary>
    private void UpdatePosition()
    {
        float firstX, firstY;
        firstX = panel.baseClipRegion.x - ((panel.baseClipRegion.z - CellWidth) * 0.5f);
        firstY = panel.baseClipRegion.y + ((panel.baseClipRegion.w - (CellHeight + EachItemPaddingForHeight) + panel.clipSoftness.y) * 0.5f);

        if (Arrangement == EArrangement.Vertical)
        {
            int col = maxCol;
            for (int i = 0; i < mList.Count; i++)
            {
                Transform t = mList[i].Target.transform;

                Vector3 position = Vector3.zero;

                //index를 기준으로 위치를 다시 잡는다. ( % 연산은 쓰지 않고 계산.)
                int div = mList[i].Index / col;
                int remain = mList[i].Index - (col * div);

                position.x += firstX + (remain * CellWidth);
                
                position.y -= -firstY + (div * (CellHeight + EachItemPaddingForHeight ));

                t.localPosition = position;
                t.name = string.Format("item index:{0}", mList[i].Index);
            }
        }
        else
        {
            int row = maxRow;
            for (int i = 0; i < mList.Count; i++)
            {
                Transform t = mList[i].Target.transform;

                Vector3 position = Vector3.zero;
                //index를 기준으로 위치를 다시 잡는다. ( % 연산은 쓰지 않고 계산.)
                int div = mList[i].Index / row;
                int remain = mList[i].Index - (row * div);

                position.x += firstX + (div * CellWidth);
                position.y -= -firstY + (remain * (CellHeight + EachItemPaddingForHeight));

                t.localPosition = position;
                t.name = string.Format("item index:{0}", mList[i].Index);
            }
        }
    }

    /// <summary>
    /// 해당 아이템을 삭제한다.
    /// </summary>
    /// <param name="item"></param>
    public void RemoveItem(UIListItem item)
    {
        if (item.Prev != null)
        {
            item.Prev.Next = item.Next;
        }

        if (item.Next != null)
        {
            item.Next.Prev = item.Prev;
        }

        UIListItem tmp = item.Next as UIListItem;
        int idx = item.Index;
        int tempIdx;
        while (tmp != null)
        {
            tempIdx = tmp.Index;
            tmp.Index = idx;

            CallAllOfItemDelegates(tmp.Target.transform, tmp.Index);

            idx = tempIdx;
            tmp = tmp.Next as UIListItem;

        }

        UIListItem tail = Tail;
        mList.Remove(item);

        if (ItemCount < mMinShowCount)
        {
            GameObject.DestroyImmediate(item.Target);
        }
        else
        {
            if (item == tail || Tail.Index >= ItemCount - 1)
            {
                // add head
                Head.Prev = item;
                item.Next = Head;
                item.Prev = null;
                item.Index = Head.Index - 1;
                mList.Insert(0, item);

                CallAllOfItemDelegates(tmp.Target.transform, tmp.Index);

                Vector3 constraint = panel.CalculateConstrainOffset(bounds.min, bounds.max);
                SpringPanel.Begin(panel.gameObject, mTrans.localPosition + constraint, 13f);
            }
            else
            {
                // add tail
                Tail.Next = item;
                item.Prev = Tail;
                item.Next = null;
                item.Index = Tail.Index + 1;
                mList.Add(item);

                CallAllOfItemDelegates(item.Target.transform, item.Index);
            }
        }

        UpdatePosition();
    }

    /// <summary>
    /// 아이템 모두 삭제한다.
    /// </summary>
    public void RemoveAll()
    {
        m_bEnableScrolling = false;
        UIListItem item = null;
        for (int i = 0; i < mList.Count; i++)
        {
            item = mList[i];
            GameObject.DestroyImmediate(item.Target);
        }

        mList.Clear();
    }



    public void AddItemTotail(int numItem, ChangeIndexDelegate callback=null, float _dragAmountX = 0.0f, float _dragAmountY = 0.0f)
    {
        ItemTotalCount += numItem;

        if (NumItemPerRow > 0)
            ItemCount = Mathf.CeilToInt((float)ItemTotalCount / (float)NumItemPerRow);
        else
            ItemCount = ItemTotalCount;

        //화면에 보여질 개수
        if (Arrangement == EArrangement.Vertical)
            mMinShowCount = maxCol * (maxRow + 1);
        else
            mMinShowCount = (maxCol + 1) * maxRow;

        int makeCount = Mathf.Min(ItemCount, mMinShowCount);

        if (!m_bEnableScrolling)
        {
            this.Init(ItemTotalCount, callback, _dragAmountX, _dragAmountX);
        }
        else
        {
            SetTemplate(ItemTotalCount);
            UpdateCurrentPosition();
        }

    }

    /// <summary>
    /// scroll영역을 계산한다.
    /// </summary>
    /// <param name="root"></param>
    /// <param name="firstTemplate"></param>
    /// <param name="lastTemplate"></param>
    /// <returns></returns>
    static public Bounds CalculateRelativeWidgetBounds2(Transform root, Transform firstTemplate, Transform lastTemplate)
    {
        if (firstTemplate == null || lastTemplate == null)
            return new Bounds(Vector3.zero, Vector3.zero);

        UIWidget[] widgets1 = firstTemplate.GetComponentsInChildren<UIWidget>(true) as UIWidget[];
        UIWidget[] widgets2 = lastTemplate.GetComponentsInChildren<UIWidget>(true) as UIWidget[];
        if (widgets1.Length == 0 || widgets2.Length == 0)
            return new Bounds(Vector3.zero, Vector3.zero);

        Vector3 vMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 vMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        Matrix4x4 toLocal = root.worldToLocalMatrix;
        bool isSet = false;
        Vector3 v;

        int nMax1 = widgets1.Length;
        for (int i = 0; i < nMax1; ++i)
        {
            UIWidget w = widgets1[i];

            Vector3[] corners = w.worldCorners;

            for (int j = 0; j < 4; ++j)
            {
                v = toLocal.MultiplyPoint3x4(corners[j]);
                vMax = Vector3.Max(v, vMax);
                vMin = Vector3.Min(v, vMin);
            }
            isSet = true;
        }

        int nMax2 = widgets2.Length;
        for (int i = 0; i < nMax2; ++i)
        {
            UIWidget w = widgets2[i];

            Vector3[] corners = w.worldCorners;

            for (int j = 0; j < 4; ++j)
            {
                v = toLocal.MultiplyPoint3x4(corners[j]);
                vMax = Vector3.Max(v, vMax);
                vMin = Vector3.Min(v, vMin);
            }
            isSet = true;
        }

        if (isSet)
        {
            Bounds b = new Bounds(vMin, Vector3.zero);
            b.Encapsulate(vMax);
            return b;
        }

        return new Bounds(Vector3.zero, Vector3.zero);

    }
}
