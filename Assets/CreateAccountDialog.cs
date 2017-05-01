using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateAccountDialog : MonoBehaviour 
{
	public UIInput m_inputIdx;
	public UIInput m_inputId;
	public UIInput m_inputPw;
	public int id = 3;

	public void OnClickButton(GameObject btn)
	{
		if (btn.name == "btnCreate")
		{
			NetWorkManager.Inst.Summit (id++, m_inputId.value, m_inputPw.value);
		}

		if (btn.name == "btnModify") 
		{
			NetWorkManager.Inst.ChangeUserInfo (System.Convert.ToInt32(m_inputIdx.value), m_inputId.value, m_inputPw.value);
		}
	}
}
