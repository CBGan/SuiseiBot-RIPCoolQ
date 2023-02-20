using System;
using System.IO;
using System.Threading.Tasks;
using Skadi.Database;
using Skadi.Entities.ConfigModule;
using Skadi.Interface;
using Skadi.TimerEvent;
using Sora.EventArgs.SoraEvent;
using YukariToolBox.LightLog;
// ReSharper disable RedundantDefaultMemberInitializer

namespace Skadi.ServerInterface;

/// <summary>
/// 初始化事件
/// </summary>
internal static class InitializationEvent
{
    private static bool IsInit = false;

    /// <summary>
    /// 初始化处理
    /// </summary>
    internal static ValueTask Initialization(string _, ConnectEventArgs connectEvent)
    {
        if (IsInit)
        {
            Log.Error("Skadi初始化", "Skadi仅为单一账户用户设计，不支持重复初始化");
            return ValueTask.CompletedTask;
        }

        Log.Info("Skadi初始化", "与onebot客户端连接成功，初始化资源...");
        //初始化配置文件
        Log.Info("Skadi初始化", $"初始化用户[{connectEvent.LoginUid}]的配置");

        IGenericStorage genericStorage = SkadiApp.GetService<IGenericStorage>();
        UserConfig      userConfig     = genericStorage.GetUserConfig(connectEvent.LoginUid);
        if (userConfig is null)
        {
            Log.Fatal(new IOException("无法获取用户配置文件(Initialization)"), "Skadi初始化", "用户配置文件初始化失败");
            Environment.Exit(-1);
            return ValueTask.CompletedTask;
        }

        //在控制台显示启用模块
        Log.Info("已启用的模块",
                 $"\n{userConfig.ModuleSwitch}");
        //显示代理信息
        if (userConfig.ModuleSwitch.Hso && !string.IsNullOrEmpty(userConfig.HsoConfig.PximgProxy))
            Log.Debug("Hso Proxy", userConfig.HsoConfig.PximgProxy);

        //初始化数据库
        DatabaseInit.UserDataInit(connectEvent);

        //初始化定时器线程
        if (userConfig.ModuleSwitch.BiliSubscription)
            SubscriptionTimer.TimerEventAdd(connectEvent);

        IsInit = true;

        return ValueTask.CompletedTask;
    }
}