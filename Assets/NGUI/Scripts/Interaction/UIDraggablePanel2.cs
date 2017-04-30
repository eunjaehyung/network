using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UIDraggablePanel2 : UIScrollView
{
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
    public GameObject TemplatePrefab;

    //=====================================================================
    //
    // Fields - Variable
    //
    //=====================================================================

    /// <summary>
    /// 총 아이템의 개수
    /// </summary>
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
        
    public delegate void ChangeIndexDelegate(UIListItem item, int index);
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

    /// <summary>
    /// 화면에 보일 수 있는 가로 개수
    /// </summary>
    private int maxCol 
	{ 
		get 
		{ 
			if( Arrangement == EArrangement.Vertical )
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
			if( Arrangement == EArrangement.Vertical )
			{
				return Mathf.CeilToInt(panel.clipRange.w / CellHeight);
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
		else if(Arrangement == EArrangement.Horizontal)
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

	public void Refresh(int count)
	{
		UIListItem item = null;
		GameObject obj = null;
		UIListItem prevItem = null;

		if( Arrangement == EArrangement.Vertical )
			mMinShowCount = maxCol * (maxRow + 1);
		else
			mMinShowCount = (maxCol + 1) * maxRow;
		
		int makeCount = Mathf.Min(count, mMinShowCount);

		if(ItemCount != count)
		{
			ItemCount = count;
			SetTemplate(count);
			UpdateCurrentPosition();
		}

		if(mList.Count > count)
		{
			int removeIndexCount  = count;
			while(removeIndexCount < mList.Count)
			{
				GameObject.DestroyImmediate(mList[removeIndexCount].Target);
				mList.RemoveAt(removeIndexCount);
			}
		}
		else if(mList.Count < count && mList.Count < makeCount)
		{
			int needMakeCount = makeCount > count ? makeCount : count;

			for(int i = mList.Count; i < needMakeCount; ++i)
			{
				obj = NGUITools.AddChild(gameObject, TemplatePrefab);
				
				if( obj.GetComponent<UIDragScrollView>() == null )
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
				
				mCallback(item, i);
			}
		}

		for (int i = 0; i < mList.Count; i++)
		{
			if(i < count)
			{
				item = mList[i];
				mCallback(item, mList[i].Index);
			}
		}

		if (mMinShowCount - 1 > count)
		{
			//ResetPosition ();
			enabled = false;
            StartCoroutine(IEnueUpdatePosition());
		}
	}

    // Hack 스크롤 조정 하기위해 by 종훈
    private IEnumerator IEnueUpdatePosition()
    {
        int deley = 5;
        while (deley > 0)
        {
            UpdatePosition();
            --deley;
            yield return null;
        }
    }


    /// <summary>
    /// 아이템을 생성한다.
    /// </summary>
    /// <param name="count"></param>
    /// <param name="callback"></param>
    public void Init(int count, ChangeIndexDelegate callback)
    {
		ResetPosition ();
        mCallback = callback;

        ItemCount = count;
        SetTemplate(count);

        RemoveAll();
        mList.Clear();

        //화면에 보여질 개수
		if( Arrangement == EArrangement.Vertical )
        	mMinShowCount = maxCol * (maxRow + 1);
		else
        	mMinShowCount = (maxCol + 1) * maxRow;

        int makeCount = Mathf.Min(count, mMinShowCount);

        GameObject obj = null;
        UIListItem prevItem = null;
        for (int i = 0; i < makeCount; i++)
        {
            obj = NGUITools.AddChild(gameObject, TemplatePrefab);

            if( obj.GetComponent<UIDragScrollView>() == null )
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

            mCallback(item, i);
        }

        if (mMinShowCount - 1 > count)
        {
            StartCoroutine(IEnueUpdatePosition());
            enabled = false;
        }
        else
        {
            UpdatePosition();
            enabled = true;
        }

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
        base.SetDragAmount(x, y, updateScrollbars);

        UpdateCurrentPosition();
    }

    /// <summary>
    /// Move the panel by the specified amount.
    /// </summary>
    public override void MoveRelative(Vector3 relative)
    {
        base.MoveRelative(relative);
        UpdateCurrentPosition();
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
            GameObject firstTemplate = NGUITools.AddChild(gameObject, TemplatePrefab);
            firstTemplate.SetActive(false);
            mFirstTemplate = firstTemplate.transform;
            mFirstTemplate.name = "first rect";
        }

        if (mLastTemplate == null)
        {
            GameObject lastTemplate = NGUITools.AddChild(gameObject, TemplatePrefab);
            lastTemplate.SetActive(false);
            mLastTemplate = lastTemplate.transform;
            mLastTemplate.name = "last rect";
        }

		float firstX = panel.baseClipRegion.x - ((panel.baseClipRegion.z - CellWidth) * 0.5f);
		float firstY = panel.baseClipRegion.y + ((panel.baseClipRegion.w - CellHeight + panel.clipSoftness.y) * 0.5f);
		if( Arrangement == EArrangement.Vertical )
		{
			mFirstTemplate.localPosition = new Vector3( firstX,
			                                           firstY,
			                                           0 ); //처음위치
			mLastTemplate.localPosition = new Vector3( firstX + (LineCount-1) * CellWidth, 
			                                          firstY - CellHeight * (( count - 1)/LineCount), 0); //끝위치
		}
		else
		{
			mFirstTemplate.localPosition = new Vector3( firstX,
			                                           firstY,
			                                           0 ); //처음위치
			mLastTemplate.localPosition = new Vector3( firstX + CellWidth * (( count - 1 )/LineCount),
			                                          firstY - (LineCount-1) * CellHeight, 
			                                          0); //끝위치
		}

		mCalculatedBounds = true;
		mBounds = CalculateRelativeWidgetBounds2(mTrans, mFirstTemplate, mLastTemplate);
		
		Vector3 constraint = panel.CalculateConstrainOffset(bounds.min, bounds.max);
		SpringPanel.Begin(panel.gameObject, mTrans.localPosition + constraint, 13f);
		
	}

    /// <summary>
    /// 아이템들의 재사용을 위하여 위치를 조절한다.
    /// </summary>
    public void UpdateCurrentPosition()
    {
		if(Head == null)
			return;

        Vector3 currentPos = mFirstPosition - mTrans.localPosition;
		
		if (Arrangement == EArrangement.Vertical)
        {
			bool isScrollUp = currentPos.y > mPrevPosition.y;

			int headIndex = (int)(-currentPos.y / CellHeight) * maxCol;
			headIndex = Mathf.Clamp(headIndex, 0, ItemCount-1);

			if( headIndex + mList.Count > ItemCount)
				headIndex = ItemCount - mList.Count;

			if(headIndex < 0)
				headIndex = 0;
			
			if( Head.Index != headIndex)
			{
				if( isScrollUp )
					TailToHead();
				else
					HeadToTail();

				SetIndexHeadtoTail(headIndex);
				UpdatePosition();
			}
        }
		else
		{
			bool isScrollUp = currentPos.x > mPrevPosition.x;

			int headIndex = (int)(currentPos.x / CellWidth) * maxRow;  //세로줄의 맨 처음 
			headIndex = Mathf.Clamp(headIndex, 0, ItemCount - maxRow);

			if( headIndex + mList.Count > ItemCount)
				headIndex = ItemCount - mList.Count;

			if(headIndex < 0)
				headIndex = 0;

			if( Head.Index != headIndex)
			{
				if (isScrollUp)
					TailToHead();
				else 
					HeadToTail();

				if( headIndex + mList.Count > ItemCount || headIndex < 0)
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
			if( item.SetIndex(index))
	            mCallback(item, index);
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
            if(item.SetIndex(index))
            	mCallback(item, index);
        }
    }

    /// <summary>
    /// 아이템들의 위치를 정한다.
    /// </summary>
    private void UpdatePosition()
    {
		float firstX, firstY;
		firstX = panel.baseClipRegion.x - ((panel.baseClipRegion.z - CellWidth) * 0.5f);

        if (mMinShowCount - 1 > ItemCount)
            firstY = panel.baseClipRegion.y + ((panel.baseClipRegion.w - CellHeight + panel.clipSoftness.y) * 0.5f)
                                        + panel.clipOffset.y;
        else
            firstY = panel.baseClipRegion.y + ((panel.baseClipRegion.w - CellHeight + panel.clipSoftness.y) * 0.5f);
        //firstY = panel.baseClipRegion.y - panel.clipOffset.y + ((CellHeight+panel.baseClipRegion.w) * 0.5f);


		if( Arrangement == EArrangement.Vertical )
		{
			int col = maxCol;
	        for (int i = 0; i < mList.Count; i++)
	        {
	            Transform t = mList[i].Target.transform;

	            Vector3 position = Vector3.zero;
	           
	        	//index를 기준으로 위치를 다시 잡는다. ( % 연산은 쓰지 않고 계산.)
				int div = mList[i].Index / col;
				int remain = mList[i].Index - ( col * div );

				position.x += firstX + (remain * CellWidth);
				position.y -= -firstY + (div * CellHeight);

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
				int remain = mList[i].Index - ( row * div );

				position.x += firstX + (div * CellWidth);
				position.y -= -firstY + (remain * CellHeight);
				
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
            mCallback(tmp, tmp.Index);

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
                mCallback(item, item.Index);

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

                mCallback(item, item.Index);
            }
        }

        UpdatePosition();
    }

    /// <summary>
    /// 아이템 모두 삭제한다.
    /// </summary>
    public void RemoveAll()
    {
        UIListItem item = null;
        for (int i = 0; i < mList.Count; i++)
        {
            item = mList[i];
            GameObject.DestroyImmediate(item.Target);
        }

        mList.Clear();
    }

    /// <summary>
    /// 해당 인덱스에 아이템을 추가한다.
    /// </summary>
    /// <param name="index"></param>
    public void AddItem(int index)
    {
		ItemCount = index;
		SetTemplate(ItemCount);
        // 아직 필요없어서 추후 필요하면 구현 -_-)~....
    }

    /// <summary>
    /// scroll영역을 계산한다.
    /// </summary>
    /// <param name="root"></param>
    /// <param name="firstTemplate"></param>
    /// <param name="lastTemplate"></param>
    /// <returns></returns>
	static public Bounds CalculateRelativeWidgetBounds2( Transform root, Transform firstTemplate, Transform lastTemplate )
	{
		if( firstTemplate==null || lastTemplate==null )
			return new Bounds(Vector3.zero, Vector3.zero);
		
		UIWidget[] widgets1 = firstTemplate.GetComponentsInChildren<UIWidget>(true) as UIWidget[];
		UIWidget[] widgets2 = lastTemplate.GetComponentsInChildren<UIWidget>(true) as UIWidget[];
		if( widgets1.Length==0 || widgets2.Length==0 )
			return new Bounds(Vector3.zero, Vector3.zero);
		
		Vector3 vMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
		Vector3 vMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);
		
		Matrix4x4 toLocal = root.worldToLocalMatrix;
		bool isSet = false;
		Vector3 v;
		
		int nMax1 = widgets1.Length;
		for( int i=0; i<nMax1; ++i )
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
		for( int i=0; i<nMax2; ++i )
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