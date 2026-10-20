namespace Tm.Ant.Shell;

/// <summary>
/// 使用core标准Shell，将相对配置和数据目录固定在部署目录。
/// </summary>
public partial class App : Tm.Ant.CommonViewWpf.TmApplication
{
    public App()
    {
        System.IO.Directory.SetCurrentDirectory(AppContext.BaseDirectory);
    }
}
