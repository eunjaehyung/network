using UnityEngine;
using System.Collections;

public class ListItemBase 
{
	public ListItemBase Prev;
	public ListItemBase Next;
	
	public ListItemBase()
	{
		Prev = Next = null;
	}
}

public class UIListItem : ListItemBase {
	
	public int Index;
	public GameObject Target;
	
	public UIListItem()
	{
		Index = -1;
		Target = null;
	}
	
    /// <summary>
    /// index를 설정한다.
    /// </summary>
    /// <param name="index"></param>
	public bool SetIndex( int index )
	{
		if( Index != index )
		{
			Index = index;
			if( Target != null )
			{
				cUIScrollListBase scr = Target.GetComponent< cUIScrollListBase >();
				scr.ListItem = this;
			}
			return true;
		}		
		return false;
	}	
}

public class cUIScrollListBase : MonoBehaviour
{
    public UIListItem ListItem;
}
