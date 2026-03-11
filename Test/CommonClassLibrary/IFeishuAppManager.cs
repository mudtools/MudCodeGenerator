using Mud.HttpUtils;

namespace CommonClassLibrary;


public interface IFeishuAppManager
{
    IMudAppContext GetDefaultApp();

    ITokenManage GetTokenManager(TokenType tokenType);

    IEnhancedHttpClient GetHttpClient();

    IMudAppContext GetApp(string appKey);
}