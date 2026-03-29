using Terminal.Gui;
using XCompressor.UI;

Application.Init();
var top = Application.Top;

// Main window
var mainWin = new MainWindow();
top.Add(mainWin);

Application.Run();