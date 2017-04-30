using UnityEngine;
using System.Collections;

public class TweenFloit1 : UITweener {
	public float from = 1f;
	public float to = 1f;

	UIProgressBar mProgressBar;
	/// <summary>
	/// Camera that's being tweened.
	/// </summary>
	
	public UIProgressBar cachedProgressBar
	{ 
		get
		{ 
			if (mProgressBar == null)
				mProgressBar = this.GetComponent<UIProgressBar>();

			return mProgressBar; } }
	
	[System.Obsolete("Use 'value' instead")]
	public float orthoSize { get { return this.value; } set { this.value = value; } }
	
	/// <summary>
	/// Tween's current value.
	/// </summary>
	
	public float value
	{
		get { return cachedProgressBar.value; }
		set { 

			cachedProgressBar.value = value; }
	}
	
	/// <summary>
	/// Tween the value.
	/// </summary>


	protected override void OnUpdate (float factor, bool isFinished) 
	{ 
		value = from * (1f - factor) + to * factor;
	}
	
	/// <summary>
	/// Start the tweening operation.
	/// </summary>
	
	static public TweenOrthoSize Begin (GameObject go, float duration, float to)
	{

		TweenOrthoSize comp = UITweener.Begin<TweenOrthoSize>(go, duration);
		comp.from = comp.value;
		comp.to = to;

		if (duration <= 0f)
		{
			comp.Sample(1f, true);
			comp.enabled = false;
		}

		return comp;
	}
	
	public override void SetStartToCurrentValue () { from = value; }
	public override void SetEndToCurrentValue () { to = value; }
}
