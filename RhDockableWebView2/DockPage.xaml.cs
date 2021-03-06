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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RhDockableWebView2
{
  /// <summary>
  /// Interaction logic for DockPage.xaml
  /// </summary>
  public partial class DockPage : UserControl
  {
    WV2DockBar _parent;
    public DockPage(WV2DockBar Parent)
    {
      InitializeComponent();
      _parent = Parent;
    }

    private void MessageReceivedInternal(object sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
    {
      _parent.ReceiveFromBrowser(sender, e);
    }
  }
}
