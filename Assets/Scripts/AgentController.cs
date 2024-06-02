using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

public class AgentController : MonoBehaviour
{
    public CookSessionController cookSessionController;
    public GameObject PFB_HelpResponse;
    public GameObject avatar;

    public SpeechModule speechModule;
    public ListenerModule listenerModule;
    public ThinkerModule thinkerModule;


    private bool requestRecipes = false;
    private string requestString = "";

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

    [System.Serializable]
    public enum ThinkingMode
    {
        Recipes,
        Help
    }
    public ThinkingMode thinkingMode = ThinkingMode.Recipes;

    private void Awake()
    {
        agentState = AgentState.Listening;
        avatar.SetActive(false);
    }

    private void Start()
    {
        listenerModule.OnUserInputReceived += ListenerModule_OnUserInputReceived;
        listenerModule.OnUserHelpInputReceived += ListenerModule_OnUserHelpInputReceived;
        thinkerModule.OnChatGPTInputReceived += ThinkerModule_OnChatGPTInputReceived;
        ThinkerModule.OnChatGPTHelpInputReceived += ThinkerModule_OnChatGPTHelpInputReceived;
    }

    /// <summary>
    /// instantiate help prefab window with text
    /// </summary>
    /// <param name="obj"></param>
    private void ThinkerModule_OnChatGPTHelpInputReceived(string obj)
    {
        //GameObject instance = Instantiate(PFB_HelpResponse, null);
        //ResponseWindow window = instance.GetComponent<ResponseWindow>();
        //window.SetResponseText(obj);
    }

    private void ListenerModule_OnUserHelpInputReceived(string obj)
    {
        Debug.Log($"Would submit help text {obj}");
        thinkerModule.SubmitChatHelpJSON(obj);
        
        thinkingMode = ThinkingMode.Help;
        SetMode(AgentState.Thinking);
    }

    private void Update()
    {
        if (requestRecipes)
        {
            requestRecipes = false;
            RecipeChatRequest();
        }
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

    private string listenerModuleInput;
    private bool listenerModuleInputReceived = false;
    public UnityEvent<string> OnListenerModuleInputReceived;

    private void ListenerModule_OnUserInputReceived(string obj)
    {
        // with new UX, we will need to not immediately switch to thinking mode
        listenerModuleInput = obj;
        listenerModuleInputReceived = true;
        OnListenerModuleInputReceived.Invoke(obj);

        Debug.Log("Listener input received: " + obj);
        SubmitChatRequest();
        // thinkerModule.SubmitChatJSON(obj);
        //thinkerModule.SubmitChat(obj);
        // SetMode(AgentState.Thinking);
    }

    public void SubmitChatRequest()
    {
        if (!listenerModuleInputReceived)
        {
            Debug.LogError("No listener input received");
            return;
        }
        thinkerModule.SubmitChatJSON(listenerModuleInput);
        thinkingMode = ThinkingMode.Recipes;
        SetMode(AgentState.Thinking);

        listenerModuleInputReceived = false;
    }

    private void OnDestroy()
    {
        listenerModule.OnUserInputReceived -= ListenerModule_OnUserInputReceived;
        listenerModule.OnUserHelpInputReceived -= ListenerModule_OnUserHelpInputReceived;
        thinkerModule.OnChatGPTInputReceived -= ThinkerModule_OnChatGPTInputReceived;
        ThinkerModule.OnChatGPTHelpInputReceived -= ThinkerModule_OnChatGPTHelpInputReceived;
    }

    /// <summary>
    /// called from update for now, set from event of chatgpt input received, saves string as global variable
    /// </summary>
    public void RecipeChatRequest()
    {
        //Task.Run(async () => await ThinkerModule_OnChatGPTInputReceivedTask(obj));
        ThinkerModule_OnChatGPTInputReceivedTask(requestString);
    }

    public void ThinkerModule_OnChatGPTInputReceived(string obj)
    {
        requestString = obj;
        requestRecipes = true;
    }

    private async void ThinkerModule_OnChatGPTInputReceivedTask(string obj)
    {
        Debug.Log($"Thinker Mode response fed to chef");
        cookSessionController.CreateRecipeBook(obj);
        if (cookSessionController.recipeBook.Recipes.Count > 0) {
            foreach (Recipe recipe in cookSessionController.recipeBook.Recipes) { // create prefab of recipe                
                Texture generatedTexture = await thinkerModule.SubmitChatImageGenerator(recipe.RecipeName + "\n Description: " + recipe.Description);
                cookSessionController.CreateRecipeObjects(recipe, generatedTexture);
            }
            // Assuming you have at least three recipes in your recipeBook
            /*string[] recipes = new string[3]
            {
                cookSessionController.recipeBook.Recipes[0].RecipeName + "\n Description: " + cookSessionController.recipeBook.Recipes[0].Description,
                cookSessionController.recipeBook.Recipes[1].RecipeName + "\n Description: " + cookSessionController.recipeBook.Recipes[1].Description,
                cookSessionController.recipeBook.Recipes[2].RecipeName + "\n Description: " + cookSessionController.recipeBook.Recipes[2].Description
            };
            List<Texture2D> genTextures = new List<Texture2D>();
            genTextures = await thinkerModule.ExecuteParallelImageGeneratorRequest(recipes);
            for (int i = 0; i < genTextures.Count; i++) {
                cookSessionController.CreateRecipeObjects(cookSessionController.recipeBook.Recipes[i], genTextures[i]);
            }*/
        }
        avatar.SetActive(true);
        SetMode(AgentState.Speaking);
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
