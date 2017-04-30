using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetWorkManager : MonoBehaviour 
{
	public static NetWorkManager Inst = null;
	public string serverId = "http://jjsprite.dothome.co.kr/userInfo.php";

	void Start()
	{
		Inst = this;
	}

	public void ChangeUserInfo(int id, string name, string pass)
	{
		WWWForm www = new WWWForm ();
		www.AddField ("select", "ChangeUserInfo");
		www.AddField ("id", id.ToString());
		www.AddField ("name", name);
		www.AddField ("pass", pass);

		StartCoroutine (Send(www));
	}

	public void Summit(int id, string name, string pass)
	{
		WWWForm www = new WWWForm ();
		www.AddField ("select", "submit");
		www.AddField ("id", id.ToString());
		www.AddField ("name", name);
		www.AddField ("pass", pass);

		StartCoroutine (Send(www));
	}

	IEnumerator Send(WWWForm data, System.Action cb = null)
	{
		WWW www = new WWW (serverId, data);

		yield return www;
		Debug.Log ("Succece");

		if (cb != null) cb ();
	}
}
