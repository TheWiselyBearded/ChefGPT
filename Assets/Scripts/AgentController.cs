using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

public class AgentController : MonoBehaviour
{
    public CookSessionController cookSessionController;
    public GameObject PFB_HelpResponse;

    public SpeechModule speechModule;
    public ListenerModule listenerModule;
    public ThinkerModule thinkerModule;

    public UnityEvent[] thinkerEvents;

    [System.Serializable]
    public enum AgentState
    {
        Listening,
        Thinking,
        Speaking
    }
    public AgentState agentState;
    public UnityEvent<AgentState> OnAgentStateChanged;

    private void Awake()
    {
        agentState = AgentState.Listening;
    }

    private void Start()
    {
        listenerModule.OnUserInputReceived += ListenerModule_OnUserInputReceived;
        listenerModule.OnUserHelpInputReceived += ListenerModule_OnUserHelpInputReceived;
        thinkerModule.OnChatGPTInputReceived += ThinkerModule_OnChatGPTInputReceived;
        thinkerModule.OnChatGPTHelpInputReceived += ThinkerModule_OnChatGPTHelpInputReceived;
    }

    /// <summary>
    /// instantiate help prefab window with text
    /// </summary>
    /// <param name="obj"></param>
    private void ThinkerModule_OnChatGPTHelpInputReceived(string obj) {
        GameObject instance = Instantiate(PFB_HelpResponse, null);
        ResponseWindow window = instance.GetComponent<ResponseWindow>();
        window.SetResponseText(obj);
    }

    private void ListenerModule_OnUserHelpInputReceived(string obj) {
        thinkerModule.SubmitChatHelpJSON(obj);
        //thinkerModule.SubmitChat(obj);
        SetMode(AgentState.Thinking);
    }

    /// <summary>
    /// invoked via unity button press events
    /// </summary>
    /// <param name="state"></param>
    public void SetAgentMode(int state)
    {
        switch (state)
        {
            case 0:
                SetMode(AgentState.Listening);
                break;
            case 1:
                SetMode(AgentState.Thinking);
                break;
            case 2:
                SetMode(AgentState.Speaking);
                break;
        }
    }


    private void ListenerModule_OnUserInputReceived(string obj)
    {
        thinkerModule.SubmitChatJSON(obj);
        //thinkerModule.SubmitChat(obj);
        SetMode(AgentState.Thinking);
    }

    private void OnDestroy()
    {
        listenerModule.OnUserInputReceived -= ListenerModule_OnUserInputReceived;
        listenerModule.OnUserHelpInputReceived -= ListenerModule_OnUserHelpInputReceived;
        thinkerModule.OnChatGPTInputReceived -= ThinkerModule_OnChatGPTInputReceived;
        thinkerModule.OnChatGPTHelpInputReceived -= ThinkerModule_OnChatGPTHelpInputReceived;
    }

    public void ThinkerModule_OnChatGPTInputReceived(string obj)
    {
        ThinkerModule_OnChatGPTInputReceivedTask(obj);
        //Task.Run(async () => await ThinkerModule_OnChatGPTInputReceivedTask(obj));
    }

    private async void ThinkerModule_OnChatGPTInputReceivedTask(string obj) {
        Debug.Log($"Thinker Mode response fed to chef");
        SetMode(AgentState.Speaking);
        cookSessionController.CreateRecipeBook(obj);
        if (cookSessionController.recipeBook.Recipes.Count > 0) {
            foreach (Recipe recipe in cookSessionController.recipeBook.Recipes) {
                // create prefab of recipe
                Texture generatedTexture = await thinkerModule.SubmitChatImageGenerator(recipe.RecipeName +"\n Description: " + recipe.Description);
                cookSessionController.CreateRecipeObjects(recipe, generatedTexture);
            }
        }
    }

    public void SetMode(AgentState newState)
    {
        agentState = newState;
        switch (agentState)
        {
            case AgentState.Listening:
                ApplyListeningModeSettings();
                break;
            case AgentState.Thinking:
                ApplyThinkingModeSettings();
                break;
            case AgentState.Speaking:
                ApplySpeakingModeSettings();
                break;
        }
        OnAgentStateChanged.Invoke(agentState);
    }

    void ApplyListeningModeSettings()
    {
        Debug.Log("Listen mode settings");
        listenerModule.ToggleDictation(true);
    }

    void ApplyThinkingModeSettings()
    {
        listenerModule.ToggleDictation(false);
        InvokeEvents(thinkerEvents);
        // generate ui objects and such for response queue?
    }


    /// <summary>
    /// invoked via unity button event
    /// </summary>
    public void SubmitChatImageRequest() => thinkerModule.SubmitScreenshotChatRequest();

    private void InvokeEvents(UnityEvent[] events)
    {
        foreach (UnityEvent uev in events)
        {
            uev?.Invoke();
        }
    }

    void ApplySpeakingModeSettings()
    {

    }

}
