using Terminal.Gui;
using TheXCompressor.UI;
class Program
{
    static void Main()
    {
        Application.Init();

        var view = new MainView();
        view.Setup();

        Application.Run();
    }
}