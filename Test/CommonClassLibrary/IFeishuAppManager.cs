using Mud.HttpUtils;

namespace CommonClassLibrary;


public interface IFeishuAppManager
{
    IMudAppContext GetDefaultApp();

    ITokenManager GetTokenManager(string tokenType);

    IEnhancedHttpClient GetHttpClient();

    IMudAppContext GetApp(string appKey);
}