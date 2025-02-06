using System.Collections;
using System.Collections.Generic;
using Jing;
using UnityEngine;
using UnityEngine.UI;

public class Main : MonoBehaviour
{
    private SignalTrigger st;

    public GameObject text;
    
    void Start()
    {
        st = new SignalTrigger(this.GetType().Assembly);
        st.Watch("None", OnSignalSwitched);
        st.Unwatch("None", OnSignalSwitched);
        st.Watch("A", OnSignalSwitched);
        st.SyncSignals();
    }

    private void OnSignalSwitched(string signalName, bool state)
    {
        Debug.Log($"信号触发: [{signalName}]({state})");
        if ("A" == signalName)
        {
            text.SetActive(state);
        }
    }

    // Update is called once per frame
    void Update()
    {
        st.CheckSignals();
    }

    [SignalTrigger.Condition("A")]
    static bool Condition1()
    {
        return GameObject.Find("Toggle1").GetComponent<Toggle>().isOn;
    }
    
    [SignalTrigger.Condition("A")]
    static bool Condition2()
    {
        return GameObject.Find("Toggle2").GetComponent<Toggle>().isOn;
    }
    
    [SignalTrigger.Condition("C")]
    static bool Condition3()
    {
        return GameObject.Find("Toggle3").GetComponent<Toggle>().isOn;
    }
}
