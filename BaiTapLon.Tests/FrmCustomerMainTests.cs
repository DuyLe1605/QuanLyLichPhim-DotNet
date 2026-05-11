using System.Reflection;
using BaiTapLon.Forms;

namespace BaiTapLon.Tests;

/// <summary>
/// Unit tests for FrmCustomerMain account menu structure and nav bar layout.
/// Requirements: 3.5, 4.1, 4.4
/// </summary>
public class FrmCustomerMainTests
{
    // -------------------------------------------------------------------------
    // Requirements 4.1, 4.4 — Nav bar uses FlowLayoutPanel with 3 nav buttons
    // -------------------------------------------------------------------------

    [Fact]
    [STAThread]
    public void NavBar_PnlNav_ContainsFlowLayoutPanel()
    {
        // Arrange
        var form = new FrmCustomerMain();

        // Act — retrieve pnlNav via reflection
        var pnlNavField = typeof(FrmCustomerMain)
            .GetField("pnlNav", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(pnlNavField);

        var pnlNav = pnlNavField.GetValue(form) as Panel;
        Assert.NotNull(pnlNav);

        // Assert — pnlNav contains a FlowLayoutPanel (flpNav)
        var flowPanel = pnlNav.Controls.OfType<FlowLayoutPanel>().FirstOrDefault();
        Assert.NotNull(flowPanel);

        form.Dispose();
    }

    [Fact]
    [STAThread]
    public void NavBar_FlpNav_ContainsExactlyThreeNavButtons()
    {
        // Arrange
        var form = new FrmCustomerMain();

        // Act — retrieve flpNav via reflection
        var flpNavField = typeof(FrmCustomerMain)
            .GetField("flpNav", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(flpNavField);

        var flpNav = flpNavField.GetValue(form) as FlowLayoutPanel;
        Assert.NotNull(flpNav);

        var buttons = flpNav.Controls.OfType<Button>().ToList();

        // Assert — exactly 3 nav buttons
        Assert.Equal(3, buttons.Count);

        form.Dispose();
    }

    [Fact]
    [STAThread]
    public void NavBar_FlpNav_IsTheFlowLayoutPanelInsidePnlNav()
    {
        // Arrange
        var form = new FrmCustomerMain();

        // Act — retrieve both fields via reflection
        var pnlNavField = typeof(FrmCustomerMain)
            .GetField("pnlNav", BindingFlags.NonPublic | BindingFlags.Instance);
        var flpNavField = typeof(FrmCustomerMain)
            .GetField("flpNav", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(pnlNavField);
        Assert.NotNull(flpNavField);

        var pnlNav = pnlNavField.GetValue(form) as Panel;
        var flpNav = flpNavField.GetValue(form) as FlowLayoutPanel;

        Assert.NotNull(pnlNav);
        Assert.NotNull(flpNav);

        // Assert — flpNav is a child of pnlNav
        Assert.Contains(flpNav, pnlNav.Controls.OfType<FlowLayoutPanel>());

        form.Dispose();
    }


    // -------------------------------------------------------------------------
    // Requirement 3.5 — Account menu retains only "Hồ sơ" and "Đăng xuất"
    // -------------------------------------------------------------------------

    [Fact]
    [STAThread]
    public void AccountMenu_ContainsExactlyTwoButtons()
    {
        // Arrange
        var form = new FrmCustomerMain();

        // Act — retrieve the private pnlAccountMenu field via reflection
        var field = typeof(FrmCustomerMain)
            .GetField("pnlAccountMenu", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(field);

        var pnlAccountMenu = field.GetValue(form) as Panel;
        Assert.NotNull(pnlAccountMenu);

        var buttons = pnlAccountMenu.Controls.OfType<Button>().ToList();

        // Assert — exactly 2 buttons
        Assert.Equal(2, buttons.Count);

        form.Dispose();
    }

    [Fact]
    [STAThread]
    public void AccountMenu_ContainsProfileButton()
    {
        // Arrange
        var form = new FrmCustomerMain();

        // Act
        var field = typeof(FrmCustomerMain)
            .GetField("pnlAccountMenu", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(field);

        var pnlAccountMenu = field.GetValue(form) as Panel;
        Assert.NotNull(pnlAccountMenu);

        var buttons = pnlAccountMenu.Controls.OfType<Button>().ToList();

        // Assert — "Hồ sơ" button exists
        Assert.Contains(buttons, b => b.Text == "Hồ sơ");

        form.Dispose();
    }

    [Fact]
    [STAThread]
    public void AccountMenu_ContainsLogoutButton()
    {
        // Arrange
        var form = new FrmCustomerMain();

        // Act
        var field = typeof(FrmCustomerMain)
            .GetField("pnlAccountMenu", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(field);

        var pnlAccountMenu = field.GetValue(form) as Panel;
        Assert.NotNull(pnlAccountMenu);

        var buttons = pnlAccountMenu.Controls.OfType<Button>().ToList();

        // Assert — "Đăng xuất" button exists
        Assert.Contains(buttons, b => b.Text == "Đăng xuất");

        form.Dispose();
    }

    [Fact]
    [STAThread]
    public void AccountMenu_DoesNotContainPointsButton()
    {
        // Arrange
        var form = new FrmCustomerMain();

        // Act
        var field = typeof(FrmCustomerMain)
            .GetField("pnlAccountMenu", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(field);

        var pnlAccountMenu = field.GetValue(form) as Panel;
        Assert.NotNull(pnlAccountMenu);

        var buttons = pnlAccountMenu.Controls.OfType<Button>().ToList();

        // Assert — "Điểm thưởng" button has been removed
        Assert.DoesNotContain(buttons, b => b.Text == "Điểm thưởng");

        form.Dispose();
    }
}
