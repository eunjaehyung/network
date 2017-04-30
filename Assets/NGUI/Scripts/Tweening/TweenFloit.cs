using UnityEngine;
using System.Collections;

public class TweenFloit : UITweener {
	public float from = 1f;
	public float to = 1f;
	public float times = 0.0f;

	UITexture mSprite;
	public UILabel mLabel;
	/// <summary>
	/// Camera that's being tweened.
	/// </summary>
	
	public UITexture cachedSprite 
	{ 
		get
		{ 
			if (mSprite == null)
				mSprite = this.GetComponent<UITexture>();

			return mSprite; } }
	
	[System.Obsolete("Use 'value' instead")]
	public float orthoSize { get { return this.value; } set { this.value = value; } }
	
	/// <summary>
	/// Tween's current value.
	/// </summary>
	
	public float value
	{
		get { return cachedSprite.fillAmount; }
		set { 

			cachedSprite.fillAmount = value; }
	}
	
	/// <summary>
	/// Tween the value.
	/// </summary>


	protected override void OnUpdate (float factor, bool isFinished) 
	{ 
		value = from * (1f - factor) + to * factor;
		if (value <= 0.01f )
		{
			times = 0.0f;
			if(mLabel != null)
				mLabel.text = "";
		}
		else
		{
			if(mLabel != null)
			{
				times += Time.deltaTime;
				mLabel.text = Mathf.RoundToInt((duration - times)).ToString();
			}
		}
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
