using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ef_unlockfps
{
    /// <summary>
    /// ManualWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ManualWindow : Window
    {
        public ManualWindow()
        {
            InitializeComponent();
        }

        // 添加修改标签内容的方法
        public void UpdateTipMessage(string message)
        {
            // 更新标签内容并设置为绿色
            tiplaunch.Content = message;
            tiplaunch.Foreground = Brushes.Green;
        }
    }
}
